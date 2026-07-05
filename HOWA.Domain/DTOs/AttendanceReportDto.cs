using System;

namespace HOWA.Domain.DTOs
{
    public class AttendanceReportDto
    {
        public int LogId { get; set; }
        public int AttendeeId { get; set; }
        public int UserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string UserRole { get; set; }
        public int EventId { get; set; }
        public string EventName { get; set; }
        public DateTime EventDate { get; set; }
        public DateTime ScannedAt { get; set; }
        public string AttendanceMethod { get; set; }
        public string AttendanceStatus { get; set; }

        public string FullName => $"{FirstName} {LastName}";
    }
}
