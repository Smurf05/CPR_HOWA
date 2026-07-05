using System.Collections.Generic;
using System.Threading.Tasks;
using HOWA.Domain.Models;
using HOWA.Domain.DTOs;

namespace HOWA.Domain.Interfaces
{
    public interface IAttendanceRepository
    {
        Task<(int LogId, string Status)> LogAttendanceAsync(string scanValue, int eventId, string method);
        Task<DashboardStatsDto> GetDashboardStatsAsync(int eventId);
        Task<IEnumerable<AttendanceReportDto>> GetAttendanceReportAsync(int? eventId, string attendeeStatus);
        Task<IEnumerable<AttendanceLog>> GetAttendeeHistoryAsync(int attendeeId);
    }
}
