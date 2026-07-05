using System;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace HOWA.Framework.Database
{
    /// <summary>
    /// Ensures the HOWA_Db database and all required schema objects exist.
    /// Safe to call on every app start — all statements are idempotent.
    /// </summary>
    public class DbInitializer
    {
        private readonly string _connectionString;

        // Master connection (points to 'master' so we can CREATE DATABASE)
        private readonly string _masterConnectionString;

        public DbInitializer(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));

            // Build a master-DB connection string from the app one
            var builder = new SqlConnectionStringBuilder(connectionString)
            {
                InitialCatalog = "master"
            };
            _masterConnectionString = builder.ConnectionString;
        }

        /// <summary>
        /// Runs the full schema creation pipeline in dependency order.
        /// </summary>
        public async Task InitializeAsync()
        {
            await EnsureDatabaseExistsAsync();
            await EnsureTablesExistAsync();
            await EnsureViewsExistAsync();
            await EnsureStoredProceduresExistAsync();
        }

        // ----------------------------------------------------------------
        // 1. Database
        // ----------------------------------------------------------------
        private async Task EnsureDatabaseExistsAsync()
        {
            const string sql = @"
                IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'HOWA_Db')
                BEGIN
                    CREATE DATABASE HOWA_Db;
                END";

            await using var conn = new SqlConnection(_masterConnectionString);
            await conn.OpenAsync();
            await using var cmd = new SqlCommand(sql, conn);
            await cmd.ExecuteNonQueryAsync();
        }

        // ----------------------------------------------------------------
        // 2. Tables  (dependency order: Users → Attendees → Events → AttendanceLogs)
        // ----------------------------------------------------------------
        private async Task EnsureTablesExistAsync()
        {
            var batches = new[]
            {
                // Users — also migrate PasswordHash → Password if the old column exists
                @"IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users' AND schema_id = SCHEMA_ID('dbo'))
                BEGIN
                    CREATE TABLE [dbo].[Users] (
                        [UserId]       INT            IDENTITY(1,1) NOT NULL,
                        [Username]     NVARCHAR(50)   NOT NULL,
                        [Password]     NVARCHAR(255)  NOT NULL,
                        [Role]         NVARCHAR(20)   NOT NULL,
                        [FirstName]    NVARCHAR(50)   NOT NULL,
                        [LastName]     NVARCHAR(50)   NOT NULL,
                        [Email]        NVARCHAR(100)  NULL,
                        [ContactNo]    NVARCHAR(20)   NULL,
                        [CreatedAt]    DATETIME       NOT NULL CONSTRAINT [DF_Users_CreatedAt] DEFAULT GETDATE(),
                        CONSTRAINT [PK_Users]          PRIMARY KEY CLUSTERED ([UserId] ASC),
                        CONSTRAINT [UQ_Users_Username] UNIQUE ([Username])
                    );
                    CREATE NONCLUSTERED INDEX [IX_Users_Role] ON [dbo].[Users]([Role] ASC);
                END
                ELSE
                BEGIN
                    -- Migrate: rename PasswordHash → Password if the old column still exists
                    IF EXISTS (
                        SELECT 1 FROM sys.columns
                        WHERE object_id = OBJECT_ID('dbo.Users')
                          AND name = 'PasswordHash'
                    )
                    BEGIN
                        EXEC sp_rename 'dbo.Users.PasswordHash', 'Password', 'COLUMN';
                    END
                END",

                // Attendees
                @"IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Attendees' AND schema_id = SCHEMA_ID('dbo'))
                BEGIN
                    CREATE TABLE [dbo].[Attendees] (
                        [AttendeeId]  INT            IDENTITY(1,1) NOT NULL,
                        [UserId]      INT            NOT NULL,
                        [RfidUid]     NVARCHAR(50)   NULL,
                        [QrCodeData]  NVARCHAR(255)  NOT NULL,
                        [Status]      NVARCHAR(20)   NOT NULL CONSTRAINT [DF_Attendees_Status] DEFAULT 'Pending',
                        [ApprovedBy]  INT            NULL,
                        [ApprovedAt]  DATETIME       NULL,
                        CONSTRAINT [PK_Attendees]                  PRIMARY KEY CLUSTERED ([AttendeeId] ASC),
                        CONSTRAINT [FK_Attendees_Users_UserId]     FOREIGN KEY ([UserId])     REFERENCES [dbo].[Users]([UserId]) ON DELETE CASCADE,
                        CONSTRAINT [FK_Attendees_Users_ApprovedBy] FOREIGN KEY ([ApprovedBy]) REFERENCES [dbo].[Users]([UserId]),
                        CONSTRAINT [UQ_Attendees_RfidUid]          UNIQUE ([RfidUid]),
                        CONSTRAINT [UQ_Attendees_QrCodeData]       UNIQUE ([QrCodeData])
                    );
                    CREATE NONCLUSTERED INDEX [IX_Attendees_Status] ON [dbo].[Attendees]([Status] ASC);
                END",

                // Events
                @"IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Events' AND schema_id = SCHEMA_ID('dbo'))
                BEGIN
                    CREATE TABLE [dbo].[Events] (
                        [EventId]     INT            IDENTITY(1,1) NOT NULL,
                        [EventName]   NVARCHAR(150)  NOT NULL,
                        [EventDate]   DATETIME       NOT NULL,
                        [Description] NVARCHAR(500)  NULL,
                        [CreatedAt]   DATETIME       NOT NULL CONSTRAINT [DF_Events_CreatedAt] DEFAULT GETDATE(),
                        CONSTRAINT [PK_Events] PRIMARY KEY CLUSTERED ([EventId] ASC)
                    );
                END",

                // AttendanceLogs
                @"IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AttendanceLogs' AND schema_id = SCHEMA_ID('dbo'))
                BEGIN
                    CREATE TABLE [dbo].[AttendanceLogs] (
                        [LogId]       INT           IDENTITY(1,1) NOT NULL,
                        [AttendeeId]  INT           NOT NULL,
                        [EventId]     INT           NOT NULL,
                        [Timestamp]   DATETIME      NOT NULL CONSTRAINT [DF_AttendanceLogs_Timestamp] DEFAULT GETDATE(),
                        [Method]      NVARCHAR(20)  NOT NULL,
                        [Status]      NVARCHAR(20)  NOT NULL,
                        CONSTRAINT [PK_AttendanceLogs]            PRIMARY KEY CLUSTERED ([LogId] ASC),
                        CONSTRAINT [FK_AttendanceLogs_Attendees]  FOREIGN KEY ([AttendeeId]) REFERENCES [dbo].[Attendees]([AttendeeId]) ON DELETE CASCADE,
                        CONSTRAINT [FK_AttendanceLogs_Events]     FOREIGN KEY ([EventId])    REFERENCES [dbo].[Events]([EventId])    ON DELETE CASCADE,
                        CONSTRAINT [UQ_Attendance_Attendee_Event] UNIQUE ([AttendeeId], [EventId])
                    );
                    CREATE NONCLUSTERED INDEX [IX_AttendanceLogs_Timestamp] ON [dbo].[AttendanceLogs]([Timestamp] ASC);
                END"
            };

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            foreach (var batch in batches)
            {
                await using var cmd = new SqlCommand(batch, conn);
                await cmd.ExecuteNonQueryAsync();
            }
        }

        // ----------------------------------------------------------------
        // 3. Views
        // ----------------------------------------------------------------
        private async Task EnsureViewsExistAsync()
        {
            // DROP + CREATE because CREATE OR ALTER VIEW needs a single batch.
            // We use IF EXISTS / DROP so repeated runs are safe.
            var batches = new[]
            {
                // vw_AttendeeDetails
                @"IF OBJECT_ID('dbo.vw_AttendeeDetails', 'V') IS NOT NULL
                    DROP VIEW [dbo].[vw_AttendeeDetails];",

                @"CREATE VIEW [dbo].[vw_AttendeeDetails] AS
                SELECT
                    a.[AttendeeId],
                    u.[UserId],
                    u.[Username],
                    u.[FirstName],
                    u.[LastName],
                    u.[Email],
                    u.[ContactNo],
                    a.[RfidUid],
                    a.[QrCodeData],
                    a.[Status]       AS [AttendeeStatus],
                    a.[ApprovedAt],
                    u_app.[Username] AS [ApprovedByUsername]
                FROM       [dbo].[Attendees] a
                INNER JOIN [dbo].[Users]     u     ON a.[UserId]     = u.[UserId]
                LEFT  JOIN [dbo].[Users]     u_app ON a.[ApprovedBy] = u_app.[UserId];",

                // vw_AttendanceReport
                @"IF OBJECT_ID('dbo.vw_AttendanceReport', 'V') IS NOT NULL
                    DROP VIEW [dbo].[vw_AttendanceReport];",

                @"CREATE VIEW [dbo].[vw_AttendanceReport] AS
                SELECT
                    l.[LogId],
                    a.[AttendeeId],
                    u.[UserId],
                    u.[FirstName],
                    u.[LastName],
                    u.[Role]      AS [UserRole],
                    e.[EventId],
                    e.[EventName],
                    e.[EventDate],
                    l.[Timestamp] AS [ScannedAt],
                    l.[Method]    AS [AttendanceMethod],
                    l.[Status]    AS [AttendanceStatus]
                FROM       [dbo].[AttendanceLogs] l
                INNER JOIN [dbo].[Attendees]      a ON l.[AttendeeId] = a.[AttendeeId]
                INNER JOIN [dbo].[Users]          u ON a.[UserId]     = u.[UserId]
                INNER JOIN [dbo].[Events]         e ON l.[EventId]    = e.[EventId];"
            };

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            foreach (var batch in batches)
            {
                await using var cmd = new SqlCommand(batch, conn);
                await cmd.ExecuteNonQueryAsync();
            }
        }

        // ----------------------------------------------------------------
        // 4. Stored Procedures
        // ----------------------------------------------------------------
        private async Task EnsureStoredProceduresExistAsync()
        {
            var batches = new[]
            {
                // sp_RegisterUser ----------------------------------------
                @"IF OBJECT_ID('dbo.sp_RegisterUser', 'P') IS NOT NULL
                    DROP PROCEDURE [dbo].[sp_RegisterUser];",

                @"CREATE PROCEDURE [dbo].[sp_RegisterUser]
                    @Username       NVARCHAR(50),
                    @Password       NVARCHAR(255),
                    @FirstName      NVARCHAR(50),
                    @LastName       NVARCHAR(50),
                    @Email          NVARCHAR(100),
                    @ContactNo      NVARCHAR(20),
                    @QrCodeData     NVARCHAR(255),
                    @NewUserId      INT OUTPUT,
                    @NewAttendeeId  INT OUTPUT
                AS
                BEGIN
                    SET NOCOUNT ON;
                    BEGIN TRANSACTION;
                    BEGIN TRY
                        INSERT INTO [dbo].[Users]
                            ([Username],[Password],[Role],[FirstName],[LastName],[Email],[ContactNo])
                        VALUES
                            (@Username,@Password,'Attendee',@FirstName,@LastName,@Email,@ContactNo);

                        SET @NewUserId = SCOPE_IDENTITY();

                        INSERT INTO [dbo].[Attendees] ([UserId],[QrCodeData],[Status])
                        VALUES (@NewUserId, @QrCodeData, 'Pending');

                        SET @NewAttendeeId = SCOPE_IDENTITY();
                        COMMIT TRANSACTION;
                    END TRY
                    BEGIN CATCH
                        ROLLBACK TRANSACTION;
                        THROW;
                    END CATCH
                END;",

                // sp_ApproveAttendee -------------------------------------
                @"IF OBJECT_ID('dbo.sp_ApproveAttendee', 'P') IS NOT NULL
                    DROP PROCEDURE [dbo].[sp_ApproveAttendee];",

                @"CREATE PROCEDURE [dbo].[sp_ApproveAttendee]
                    @AttendeeId INT,
                    @RfidUid    NVARCHAR(50),
                    @ApprovedBy INT
                AS
                BEGIN
                    SET NOCOUNT ON;
                    UPDATE [dbo].[Attendees]
                    SET
                        [Status]     = 'Approved',
                        [RfidUid]    = NULLIF(TRIM(@RfidUid), ''),
                        [ApprovedBy] = @ApprovedBy,
                        [ApprovedAt] = GETDATE()
                    WHERE [AttendeeId] = @AttendeeId;
                END;",

                // sp_LogAttendance ---------------------------------------
                @"IF OBJECT_ID('dbo.sp_LogAttendance', 'P') IS NOT NULL
                    DROP PROCEDURE [dbo].[sp_LogAttendance];",

                @"CREATE PROCEDURE [dbo].[sp_LogAttendance]
                    @ScanValue  NVARCHAR(255),
                    @EventId    INT,
                    @Method     NVARCHAR(20),
                    @LogId      INT          OUTPUT,
                    @Status     NVARCHAR(20) OUTPUT
                AS
                BEGIN
                    SET NOCOUNT ON;

                    DECLARE @AttendeeId     INT          = NULL;
                    DECLARE @AttendeeStatus NVARCHAR(20) = NULL;

                    IF @Method = 'RFID'
                        SELECT @AttendeeId = [AttendeeId], @AttendeeStatus = [Status]
                        FROM   [dbo].[Attendees] WHERE [RfidUid]    = @ScanValue;
                    ELSE IF @Method = 'QR'
                        SELECT @AttendeeId = [AttendeeId], @AttendeeStatus = [Status]
                        FROM   [dbo].[Attendees] WHERE [QrCodeData] = @ScanValue;

                    IF @AttendeeId IS NULL
                    BEGIN
                        RAISERROR('Attendee not found for the provided credentials.',16,1);
                        RETURN;
                    END

                    IF @AttendeeStatus <> 'Approved'
                    BEGIN
                        RAISERROR('Attendee registration is not approved. Current status: %s.',16,1,@AttendeeStatus);
                        RETURN;
                    END

                    DECLARE @EventDate DATETIME;
                    SELECT @EventDate = [EventDate] FROM [dbo].[Events] WHERE [EventId] = @EventId;

                    IF @EventDate IS NULL
                    BEGIN
                        RAISERROR('Target event does not exist.',16,1);
                        RETURN;
                    END

                    DECLARE @CurrentTime DATETIME = GETDATE();
                    IF @CurrentTime <= DATEADD(minute, 15, @EventDate)
                        SET @Status = 'Present';
                    ELSE
                        SET @Status = 'Late';

                    IF EXISTS (
                        SELECT 1 FROM [dbo].[AttendanceLogs]
                        WHERE [AttendeeId] = @AttendeeId AND [EventId] = @EventId
                    )
                    BEGIN
                        SELECT @LogId = [LogId], @Status = [Status]
                        FROM   [dbo].[AttendanceLogs]
                        WHERE  [AttendeeId] = @AttendeeId AND [EventId] = @EventId;
                        RETURN;
                    END

                    INSERT INTO [dbo].[AttendanceLogs]
                        ([AttendeeId],[EventId],[Timestamp],[Method],[Status])
                    VALUES
                        (@AttendeeId,@EventId,@CurrentTime,@Method,@Status);

                    SET @LogId = SCOPE_IDENTITY();
                END;",

                // sp_GetDashboardStats -----------------------------------
                @"IF OBJECT_ID('dbo.sp_GetDashboardStats', 'P') IS NOT NULL
                    DROP PROCEDURE [dbo].[sp_GetDashboardStats];",

                @"CREATE PROCEDURE [dbo].[sp_GetDashboardStats]
                    @EventId INT
                AS
                BEGIN
                    SET NOCOUNT ON;

                    DECLARE @TotalApproved    INT;
                    DECLARE @TotalPresent     INT;
                    DECLARE @PendingApprovals INT;
                    DECLARE @AttendanceRate   DECIMAL(5,2) = 0.00;

                    SELECT @TotalApproved    = COUNT(*) FROM [dbo].[Attendees]      WHERE [Status]  = 'Approved';
                    SELECT @TotalPresent     = COUNT(*) FROM [dbo].[AttendanceLogs] WHERE [EventId] = @EventId;
                    SELECT @PendingApprovals = COUNT(*) FROM [dbo].[Attendees]      WHERE [Status]  = 'Pending';

                    IF @TotalApproved > 0
                        SET @AttendanceRate = CAST((@TotalPresent * 100.0) / @TotalApproved AS DECIMAL(5,2));

                    SELECT
                        ISNULL(@TotalApproved,    0)    AS [TotalRegistered],
                        ISNULL(@TotalPresent,     0)    AS [TotalPresent],
                        ISNULL(@PendingApprovals, 0)    AS [PendingApprovals],
                        ISNULL(@AttendanceRate,   0.00) AS [AttendanceRate];
                END;"
            };

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            foreach (var batch in batches)
            {
                await using var cmd = new SqlCommand(batch, conn);
                await cmd.ExecuteNonQueryAsync();
            }
        }
    }
}
