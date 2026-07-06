using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using HOWA.Domain.DTOs;
using HOWA.Domain.Interfaces;
using HOWA.Domain.Models;
using HOWA.Shared.Constants;

namespace HOWA.Repository.Repositories
{
    public class AttendanceRepository : BaseRepository, IAttendanceRepository
    {
        public AttendanceRepository(string connectionString) : base(connectionString)
        {
        }

        // ----------------------------------------------------------------
        // Original direct log (kept for backward-compat / non-OTP paths)
        // ----------------------------------------------------------------
        public async Task<(int LogId, string Status)> LogAttendanceAsync(string scanValue, int eventId, string method)
        {
            using (var db = CreateConnection())
            {
                var parameters = new DynamicParameters();
                parameters.Add("@ScanValue", scanValue);
                parameters.Add("@EventId",   eventId);
                parameters.Add("@Method",    method);
                parameters.Add("@LogId",  dbType: DbType.Int32,  direction: ParameterDirection.Output);
                parameters.Add("@Status", dbType: DbType.String, size: 20, direction: ParameterDirection.Output);

                await db.ExecuteAsync(DbConstants.SpLogAttendance, parameters, commandType: CommandType.StoredProcedure);

                int    logId  = parameters.Get<int>("@LogId");
                string status = parameters.Get<string>("@Status");

                return (logId, status);
            }
        }

        // ----------------------------------------------------------------
        // OTP — Step 1: issue a code after scan
        // ----------------------------------------------------------------
        public async Task<(int OtpId, string Code)> IssueOtpAsync(string scanValue, int eventId, string method)
        {
            using (var db = CreateConnection())
            {
                var parameters = new DynamicParameters();
                parameters.Add("@ScanValue", scanValue);
                parameters.Add("@EventId",   eventId);
                parameters.Add("@Method",    method);
                parameters.Add("@OtpId",  dbType: DbType.Int32,  direction: ParameterDirection.Output);
                parameters.Add("@Code",   dbType: DbType.String, size: 6,  direction: ParameterDirection.Output);

                await db.ExecuteAsync(DbConstants.SpIssueOtp, parameters, commandType: CommandType.StoredProcedure);

                int    otpId = parameters.Get<int>("@OtpId");
                string code  = parameters.Get<string>("@Code");

                return (otpId, code);
            }
        }

        // ----------------------------------------------------------------
        // OTP — Step 2: verify code and log attendance
        // ----------------------------------------------------------------
        public async Task<OtpVerifyResult> VerifyOtpAndLogAsync(int otpId, string code)
        {
            using (var db = CreateConnection())
            {
                var parameters = new DynamicParameters();
                parameters.Add("@OtpId",   otpId);
                parameters.Add("@Code",    code);
                parameters.Add("@Success", dbType: DbType.Boolean, direction: ParameterDirection.Output);
                parameters.Add("@LogId",   dbType: DbType.Int32,   direction: ParameterDirection.Output);
                parameters.Add("@Status",  dbType: DbType.String,  size: 20, direction: ParameterDirection.Output);
                parameters.Add("@Message", dbType: DbType.String,  size: 255, direction: ParameterDirection.Output);

                await db.ExecuteAsync(DbConstants.SpVerifyOtp, parameters, commandType: CommandType.StoredProcedure);

                return new OtpVerifyResult
                {
                    Success = parameters.Get<bool>("@Success"),
                    LogId   = parameters.Get<int>("@LogId"),
                    Status  = parameters.Get<string>("@Status") ?? string.Empty,
                    Message = parameters.Get<string>("@Message") ?? string.Empty
                };
            }
        }

        // ----------------------------------------------------------------
        // OTP — Step 0: poll for a pending OTP (mobile auto-detect)
        // ----------------------------------------------------------------
        public async Task<(int OtpId, string Code)?> GetPendingOtpAsync(int attendeeId)
        {
            const string sql = @"
                SELECT TOP 1 [OtpId], [Code]
                FROM   [dbo].[OtpTokens]
                WHERE  [AttendeeId] = @AttendeeId
                  AND  [IsUsed]     = 0
                  AND  [ExpiresAt]  > GETDATE()
                ORDER BY [IssuedAt] DESC";

            using (var db = CreateConnection())
            {
                var row = await db.QueryFirstOrDefaultAsync(sql, new { AttendeeId = attendeeId });
                if (row == null) return null;
                return ((int)row.OtpId, (string)row.Code);
            }
        }

        // ----------------------------------------------------------------
        // Dashboard stats
        // ----------------------------------------------------------------
        public async Task<DashboardStatsDto> GetDashboardStatsAsync(int eventId)
        {
            using (var db = CreateConnection())
            {
                var parameters = new DynamicParameters();
                parameters.Add("@EventId", eventId);

                return await db.QueryFirstOrDefaultAsync<DashboardStatsDto>(
                    DbConstants.SpGetDashboardStats,
                    parameters,
                    commandType: CommandType.StoredProcedure
                );
            }
        }

        // ----------------------------------------------------------------
        // Report query
        // ----------------------------------------------------------------
        public async Task<IEnumerable<AttendanceReportDto>> GetAttendanceReportAsync(int? eventId, string attendeeStatus)
        {
            var sb = new StringBuilder($"SELECT * FROM {DbConstants.ViewAttendanceReport} WHERE 1=1");
            var parameters = new DynamicParameters();

            if (eventId.HasValue)
            {
                sb.Append(" AND EventId = @EventId");
                parameters.Add("@EventId", eventId.Value);
            }

            if (!string.IsNullOrEmpty(attendeeStatus))
            {
                sb.Append(" AND AttendanceStatus = @Status");
                parameters.Add("@Status", attendeeStatus);
            }

            sb.Append(" ORDER BY ScannedAt DESC");

            using (var db = CreateConnection())
            {
                return await db.QueryAsync<AttendanceReportDto>(sb.ToString(), parameters);
            }
        }

        // ----------------------------------------------------------------
        // Attendee history
        // ----------------------------------------------------------------
        public async Task<IEnumerable<AttendanceLog>> GetAttendeeHistoryAsync(int attendeeId)
        {
            const string sql = $"SELECT * FROM {DbConstants.TableAttendanceLogs} WHERE AttendeeId = @AttendeeId ORDER BY Timestamp DESC";
            using (var db = CreateConnection())
            {
                return await db.QueryAsync<AttendanceLog>(sql, new { AttendeeId = attendeeId });
            }
        }
    }
}
