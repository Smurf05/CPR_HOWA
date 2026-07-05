namespace HOWA.Shared.Constants
{
    public static class AppConstants
    {
        public const string AppName = "House of Worship Attendance System (HOWA)";
        public const string AdminRole = "Admin";
        public const string AttendeeRole = "Attendee";
        
        public static class Status
        {
            public const string Pending = "Pending";
            public const string Approved = "Approved";
            public const string Rejected = "Rejected";
        }

        public static class AttendanceStatus
        {
            public const string Present = "Present";
            public const string Late = "Late";
            public const string Absent = "Absent";
        }

        public static class ScanMethod
        {
            public const string RFID = "RFID";
            public const string QR = "QR";
            public const string Manual = "Manual";
        }
    }
}
