namespace TPEmail.BusinessModels.ResponseModels
{
    /// <summary>
    /// Request model for list/data-fetching endpoints.
    /// Pagination logic (defaults, limits) is handled by stored procedures.
    /// </summary>
    public class ListDataRequest
    {
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public string? SearchTerm { get; set; }
        public int? RoleId { get; set; }
        public int? Active { get; set; }
        public string? SortBy { get; set; }
    }
}
