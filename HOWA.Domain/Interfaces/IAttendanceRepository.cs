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

        /// <summary>
        /// Issues an OTP after a successful RFID/QR scan.
        /// Returns the 6-digit code and its OtpId.
        /// </summary>
        Task<(int OtpId, string Code)> IssueOtpAsync(string scanValue, int eventId, string method);

        /// <summary>
        /// Verifies the OTP entered by the attendee and logs attendance if valid.
        /// </summary>
        Task<OtpVerifyResult> VerifyOtpAndLogAsync(int otpId, string code);

        /// <summary>
        /// Checks if there is an active (unused, not expired) OTP for the given attendee.
        /// Used by the mobile app to auto-detect a scan and pop up the OTP screen.
        /// Returns null if no pending OTP exists.
        /// </summary>
        Task<(int OtpId, string Code)?> GetPendingOtpAsync(int attendeeId);
    }
}
