namespace HOWA.Domain.DTOs
{
    public class DashboardStatsDto
    {
        public int TotalRegistered { get; set; }
        public int TotalPresent { get; set; }
        public int PendingApprovals { get; set; }
        public decimal AttendanceRate { get; set; }
    }
}
