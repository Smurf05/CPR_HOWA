using System;

namespace HOWA.Domain.Models
{
    public class AttendanceLog
    {
        public int LogId { get; set; }
        public int AttendeeId { get; set; }
        public int EventId { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string Method { get; set; } // 'RFID', 'QR', 'Manual'
        public string Status { get; set; } // 'Present', 'Late', 'Absent'
    }
}
