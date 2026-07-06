using System;

namespace HOWA.Domain.Models
{
    /// <summary>
    /// Represents a one-time password issued during an RFID or QR scan.
    /// Valid for <see cref="ExpiresAt"/>; consumed once by the attendee.
    /// </summary>
    public class OtpToken
    {
        public int    OtpId      { get; set; }
        public int    AttendeeId { get; set; }
        public int    EventId    { get; set; }
        public string Code       { get; set; } = string.Empty; // 6-digit numeric
        public string Method     { get; set; } = string.Empty; // 'RFID' or 'QR'
        public DateTime IssuedAt  { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool   IsUsed     { get; set; }
    }
}
