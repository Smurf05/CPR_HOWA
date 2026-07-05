using System;

namespace HOWA.Domain.Models
{
    public class Attendee
    {
        public int AttendeeId { get; set; }
        public int UserId { get; set; }
        public string RfidUid { get; set; }
        public string QrCodeData { get; set; }
        public string Status { get; set; } // 'Pending', 'Approved', 'Rejected'
        public int? ApprovedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }

        // Navigation property placeholders (if ORM loaded)
        public User UserDetails { get; set; }
    }
}
