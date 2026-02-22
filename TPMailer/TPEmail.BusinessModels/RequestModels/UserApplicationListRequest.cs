namespace TPEmail.BusinessModels.RequestModels
{
    public class UserApplicationListRequest
    {
        public string UserId { get; set; } = string.Empty;
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public string? SearchTerm { get; set; }
    }
}
