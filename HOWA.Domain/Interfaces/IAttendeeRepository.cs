using System.Collections.Generic;
using System.Threading.Tasks;
using HOWA.Domain.Models;

namespace HOWA.Domain.Interfaces
{
    public interface IAttendeeRepository
    {
        Task<Attendee> GetByIdAsync(int id);
        Task<Attendee> GetByUserIdAsync(int userId);
        Task<Attendee> GetByRfidUidAsync(string rfidUid);
        Task<Attendee> GetByQrCodeDataAsync(string qrCodeData);
        Task<IEnumerable<Attendee>> GetPendingApprovalsAsync();
        Task<IEnumerable<Attendee>> GetAllApprovedAsync();
        Task<bool> RegisterAsync(User user, Attendee attendee);
        Task<bool> ApproveAsync(int attendeeId, string rfidUid, int approvedByUserId);
        Task<bool> RejectAsync(int attendeeId);
    }
}
