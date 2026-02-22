namespace TPEmail.BusinessModels.RequestModels
{

    public class DashboardEmailDto
    {
        public int AppId { get; set; }
        public string AppName { get; set; } = string.Empty;
        public int TotalSentEmail { get; set; }
        public int LastThirtyDaysEmail { get; set; }
        public int LastSevenDaysEmail { get; set; }
        public int TodayEmail { get; set; }
        public int MonthlyEmail { get; set; }
        public int YearlyEmail { get; set; }
    }
}
