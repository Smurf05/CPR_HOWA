using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using HOWA.Domain.Interfaces;
using HOWA.Domain.Models;
using HOWA.Shared.Constants;

namespace HOWA.Repository.Repositories
{
    public class AttendeeRepository : BaseRepository, IAttendeeRepository
    {
        public AttendeeRepository(string connectionString) : base(connectionString)
        {
        }

        public async Task<Attendee> GetByIdAsync(int id)
        {
            const string sql = $"SELECT * FROM {DbConstants.TableAttendees} WHERE AttendeeId = @Id";
            using (var db = CreateConnection())
            {
                return await db.QueryFirstOrDefaultAsync<Attendee>(sql, new { Id = id });
            }
        }

        public async Task<Attendee> GetByUserIdAsync(int userId)
        {
            const string sql = $"SELECT * FROM {DbConstants.TableAttendees} WHERE UserId = @UserId";
            using (var db = CreateConnection())
            {
                return await db.QueryFirstOrDefaultAsync<Attendee>(sql, new { UserId = userId });
            }
        }

        public async Task<Attendee> GetByRfidUidAsync(string rfidUid)
        {
            const string sql = $"SELECT * FROM {DbConstants.TableAttendees} WHERE RfidUid = @RfidUid";
            using (var db = CreateConnection())
            {
                return await db.QueryFirstOrDefaultAsync<Attendee>(sql, new { RfidUid = rfidUid });
            }
        }

        public async Task<Attendee> GetByQrCodeDataAsync(string qrCodeData)
        {
            const string sql = $"SELECT * FROM {DbConstants.TableAttendees} WHERE QrCodeData = @QrCodeData";
            using (var db = CreateConnection())
            {
                return await db.QueryFirstOrDefaultAsync<Attendee>(sql, new { QrCodeData = qrCodeData });
            }
        }

        public async Task<IEnumerable<Attendee>> GetPendingApprovalsAsync()
        {
            const string sql = @$"
                SELECT a.*, u.* 
                FROM {DbConstants.TableAttendees} a 
                INNER JOIN {DbConstants.TableUsers} u ON a.UserId = u.UserId 
                WHERE a.Status IN ('Pending', 'Rejected')";

            using (var db = CreateConnection())
            {
                return await db.QueryAsync<Attendee, User, Attendee>(
                    sql,
                    (attendee, user) =>
                    {
                        attendee.UserDetails = user;
                        return attendee;
                    },
                    splitOn: "UserId"
                );
            }
        }

        public async Task<IEnumerable<Attendee>> GetAllApprovedAsync()
        {
            const string sql = @$"
                SELECT a.*, u.* 
                FROM {DbConstants.TableAttendees} a 
                INNER JOIN {DbConstants.TableUsers} u ON a.UserId = u.UserId 
                WHERE a.Status = 'Approved'";

            using (var db = CreateConnection())
            {
                return await db.QueryAsync<Attendee, User, Attendee>(
                    sql,
                    (attendee, user) =>
                    {
                        attendee.UserDetails = user;
                        return attendee;
                    },
                    splitOn: "UserId"
                );
            }
        }

        public async Task<bool> RegisterAsync(User user, Attendee attendee)
        {
            using (var db = CreateConnection())
            {
                var parameters = new DynamicParameters();
                parameters.Add("@Username",      user.Username);
                parameters.Add("@Password",      user.Password);
                parameters.Add("@FirstName",     user.FirstName);
                parameters.Add("@LastName",      user.LastName);
                parameters.Add("@Email",         user.Email);
                parameters.Add("@ContactNo",     user.ContactNo);
                parameters.Add("@QrCodeData",    attendee.QrCodeData);
                parameters.Add("@NewUserId",     dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@NewAttendeeId", dbType: DbType.Int32, direction: ParameterDirection.Output);

                await db.ExecuteAsync(DbConstants.SpRegisterUser, parameters, commandType: CommandType.StoredProcedure);

                int userId     = parameters.Get<int>("@NewUserId");
                int attendeeId = parameters.Get<int>("@NewAttendeeId");

                if (userId > 0 && attendeeId > 0)
                {
                    user.UserId         = userId;
                    attendee.AttendeeId = attendeeId;
                    attendee.UserId     = userId;
                    return true;
                }
                return false;
            }
        }

        public async Task<bool> ApproveAsync(int attendeeId, string rfidUid, int approvedByUserId)
        {
            using (var db = CreateConnection())
            {
                var parameters = new DynamicParameters();
                parameters.Add("@AttendeeId", attendeeId);
                parameters.Add("@RfidUid",    rfidUid);
                parameters.Add("@ApprovedBy", approvedByUserId);

                var rows = await db.ExecuteAsync(DbConstants.SpApproveAttendee, parameters, commandType: CommandType.StoredProcedure);
                return rows >= 0;
            }
        }

        public async Task<bool> RejectAsync(int attendeeId)
        {
            const string sql = $"UPDATE {DbConstants.TableAttendees} SET Status = 'Rejected' WHERE AttendeeId = @AttendeeId";
            using (var db = CreateConnection())
            {
                var rows = await db.ExecuteAsync(sql, new { AttendeeId = attendeeId });
                return rows > 0;
            }
        }
    }
}
