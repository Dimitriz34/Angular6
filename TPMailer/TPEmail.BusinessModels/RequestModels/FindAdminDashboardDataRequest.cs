namespace TPEmail.BusinessModels.RequestModels
{
    /// <summary>
    /// Request model for dashboard data retrieval with optional user filter
    /// </summary>
    public class FindAdminDashboardDataRequest
    {
        public string? UserId { get; set; }
    }
}
