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

        public async Task<(int LogId, string Status)> LogAttendanceAsync(string scanValue, int eventId, string method)
        {
            using (var db = CreateConnection())
            {
                var parameters = new DynamicParameters();
                parameters.Add("@ScanValue", scanValue);
                parameters.Add("@EventId",   eventId);
                parameters.Add("@Method",    method);
                parameters.Add("@LogId",  dbType: DbType.Int32,   direction: ParameterDirection.Output);
                parameters.Add("@Status", dbType: DbType.String, size: 20, direction: ParameterDirection.Output);

                await db.ExecuteAsync(DbConstants.SpLogAttendance, parameters, commandType: CommandType.StoredProcedure);

                int    logId  = parameters.Get<int>("@LogId");
                string status = parameters.Get<string>("@Status");

                return (logId, status);
            }
        }

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
