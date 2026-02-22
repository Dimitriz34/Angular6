namespace TPEmail.BusinessModels.RequestModels
{
    public class ADUserRequest
    {
        public string Upn { get; set; } = string.Empty;
    }

    public class ADUserSearchRequest
    {
        public string Term { get; set; } = string.Empty;
        /// <summary>
        /// Search type: 'upn', 'displayName', 'firstName', 'lastName', or 'all' (default: 'all')
        /// 'all' searches across UPN, displayName, firstName, and lastName
        /// </summary>
        public string? SearchType { get; set; } = "all";
    }
}
