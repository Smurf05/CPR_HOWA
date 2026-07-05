namespace HOWA.Shared.Constants
{
    public static class DbConstants
    {
        // Default connection string — used as fallback if appsettings.json is missing.
        // Both Admin and Mobile point to SQLEXPRESS so they share the same database.
        public const string DefaultConnectionString =
            "Server=SMURFIIES\\SQLEXPRESS,1433;Database=HOWA_Db;User Id=sa;Password=Howa@2024;TrustServerCertificate=True;MultipleActiveResultSets=true;";

        // ----------------------------------------------------------------
        // Stored Procedure Names
        // ----------------------------------------------------------------
        public const string SpRegisterUser      = "sp_RegisterUser";
        public const string SpApproveAttendee   = "sp_ApproveAttendee";
        public const string SpLogAttendance     = "sp_LogAttendance";
        public const string SpGetDashboardStats = "sp_GetDashboardStats";

        // ----------------------------------------------------------------
        // View Names
        // ----------------------------------------------------------------
        public const string ViewAttendeeDetails   = "vw_AttendeeDetails";
        public const string ViewAttendanceReport  = "vw_AttendanceReport";

        // ----------------------------------------------------------------
        // Table Names
        // ----------------------------------------------------------------
        public const string TableUsers          = "Users";
        public const string TableAttendees      = "Attendees";
        public const string TableEvents         = "Events";
        public const string TableAttendanceLogs = "AttendanceLogs";
    }
}
