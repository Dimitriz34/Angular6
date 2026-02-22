namespace TPEmail.BusinessModels.ResponseModels
{
    /// <summary>
    /// DTO for user count results with active/inactive breakdown.
    /// Used for database-level count queries with filtering.
    /// </summary>
    public class UserCountDto
    {
        public int TotalCount { get; set; }
        public int ActiveCount { get; set; }
        public int InactiveCount { get; set; }
    }

    /// <summary>
    /// DTO for application count results with active/inactive breakdown.
    /// Used for database-level count queries with filtering.
    /// </summary>
    public class ApplicationCountDto
    {
        public int TotalCount { get; set; }
        public int ActiveCount { get; set; }
        public int InactiveCount { get; set; }
    }
}
