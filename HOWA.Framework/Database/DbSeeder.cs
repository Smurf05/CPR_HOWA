using System;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace HOWA.Framework.Database
{
    /// <summary>
    /// Seeds the database with default admin accounts and sample data.
    /// Only runs when the Users table is completely empty (first launch).
    /// </summary>
    public class DbSeeder
    {
        private readonly string _connectionString;

        // Default password for all seed accounts
        private const string DefaultPassword = "Password123";

        public DbSeeder(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        /// <summary>
        /// Seeds admin users and sample data only if Users table is empty
        /// OR if existing passwords look like SHA-256 hashes (64-char hex)
        /// from the old hashing scheme — in that case, wipes and re-seeds
        /// with plain-text passwords.
        /// </summary>
        public async Task SeedAsync()
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            // Check how many rows exist and whether they're still hashed
            await using var checkCmd = new SqlCommand(
                "SELECT TOP 1 [Password] FROM [dbo].[Users] ORDER BY [UserId]", conn);

            bool needsReseed = false;
            try
            {
                var firstPassword = await checkCmd.ExecuteScalarAsync() as string;

                if (firstPassword == null)
                {
                    // Table is empty
                    needsReseed = true;
                }
                else if (firstPassword.Length == 64 &&
                         System.Text.RegularExpressions.Regex.IsMatch(firstPassword, "^[0-9a-f]{64}$"))
                {
                    // Looks like a SHA-256 hash — wipe and re-seed with plain text
                    needsReseed = true;
                    await WipeAllDataAsync(conn);
                }
            }
            catch
            {
                // Column doesn't exist yet or table empty — let initializer handle it
                return;
            }

            if (!needsReseed)
                return;

            await SeedUsersAsync(conn);
            await SeedAttendeesAsync(conn);
            await SeedEventsAsync(conn);
            await SeedAttendanceLogsAsync(conn);
        }

        private async Task WipeAllDataAsync(SqlConnection conn)
        {
            var statements = new[]
            {
                "DELETE FROM [dbo].[AttendanceLogs]",
                "DELETE FROM [dbo].[Attendees]",
                "DELETE FROM [dbo].[Events]",
                "DELETE FROM [dbo].[Users]",
                "DBCC CHECKIDENT ('[dbo].[AttendanceLogs]', RESEED, 0)",
                "DBCC CHECKIDENT ('[dbo].[Events]',         RESEED, 0)",
                "DBCC CHECKIDENT ('[dbo].[Attendees]',      RESEED, 0)",
                "DBCC CHECKIDENT ('[dbo].[Users]',          RESEED, 0)",
            };
            foreach (var sql in statements)
            {
                await using var cmd = new SqlCommand(sql, conn);
                await cmd.ExecuteNonQueryAsync();
            }
        }

        private async Task SeedUsersAsync(SqlConnection conn)
        {
            const string sql = @"
                INSERT INTO [dbo].[Users]
                    ([Username],[Password],[Role],[FirstName],[LastName],[Email],[ContactNo])
                VALUES
                    ('admin1',      @Hash, 'Admin',    'Aldrin Jone', 'Bisin',  'aldrin@scc.edu.ph',    '09123456789'),
                    ('admin2',      @Hash, 'Admin',    'Zoe',         'Majani', 'zoe@scc.edu.ph',       '09987654321'),
                    ('john_doe',    @Hash, 'Attendee', 'John',        'Doe',    'johndoe@scc.edu.ph',   '09151234567'),
                    ('jane_smith',  @Hash, 'Attendee', 'Jane',        'Smith',  'janesmith@scc.edu.ph', '09267654321'),
                    ('sam_wilson',  @Hash, 'Attendee', 'Sam',         'Wilson', 'samw@scc.edu.ph',      '09379876543'),
                    ('alice_jones', @Hash, 'Attendee', 'Alice',       'Jones',  'alicej@scc.edu.ph',    '09452345678'),
                    ('bob_brown',   @Hash, 'Attendee', 'Bob',         'Brown',  'bobb@scc.edu.ph',      '09568765432');";

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Hash", DefaultPassword);
            await cmd.ExecuteNonQueryAsync();
        }

        private async Task SeedAttendeesAsync(SqlConnection conn)
        {
            const string sql = @"
                INSERT INTO [dbo].[Attendees]
                    ([UserId],[RfidUid],[QrCodeData],[Status],[ApprovedBy],[ApprovedAt])
                VALUES
                    (3, 'RFID_987654321', 'HOWA_john_doe_QR_TOKEN',    'Approved', 1, GETDATE()),
                    (4, 'RFID_876543210', 'HOWA_jane_smith_QR_TOKEN',  'Approved', 1, GETDATE()),
                    (5, 'RFID_765432109', 'HOWA_sam_wilson_QR_TOKEN',  'Approved', 2, GETDATE()),
                    (6, NULL,             'HOWA_alice_jones_QR_TOKEN', 'Pending',  NULL, NULL),
                    (7, NULL,             'HOWA_bob_brown_QR_TOKEN',   'Pending',  NULL, NULL);";

            await using var cmd = new SqlCommand(sql, conn);
            await cmd.ExecuteNonQueryAsync();
        }

        private async Task SeedEventsAsync(SqlConnection conn)
        {
            const string sql = @"
                INSERT INTO [dbo].[Events] ([EventName],[EventDate],[Description])
                VALUES
                    ('Midweek Chapel Convocation',
                     DATEADD(day, -2, GETDATE()),
                     'SCC Midweek religious convocation for students and faculty.'),
                    ('Friday Praise and Worship',
                     DATEADD(day, -1, GETDATE()),
                     'Weekend spiritual gathering for worship expression.'),
                    ('Sunday Service Convocation',
                     DATEADD(hour, 2, GETDATE()),
                     'Main Sunday religious service and attendance check.');";

            await using var cmd = new SqlCommand(sql, conn);
            await cmd.ExecuteNonQueryAsync();
        }

        private async Task SeedAttendanceLogsAsync(SqlConnection conn)
        {
            const string sql = @"
                INSERT INTO [dbo].[AttendanceLogs]
                    ([AttendeeId],[EventId],[Timestamp],[Method],[Status])
                VALUES
                    (1, 1, DATEADD(minute,  5, DATEADD(day,-2,GETDATE())), 'RFID', 'Present'),
                    (2, 1, DATEADD(minute, 12, DATEADD(day,-2,GETDATE())), 'QR',   'Present'),
                    (3, 1, DATEADD(minute, 20, DATEADD(day,-2,GETDATE())), 'RFID', 'Late'),
                    (1, 2, DATEADD(minute,  3, DATEADD(day,-1,GETDATE())), 'RFID', 'Present'),
                    (2, 2, DATEADD(minute,  8, DATEADD(day,-1,GETDATE())), 'QR',   'Present');";

            await using var cmd = new SqlCommand(sql, conn);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
