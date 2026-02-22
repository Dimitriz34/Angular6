namespace TPEmail.BusinessModels.ResponseModels
{
    public class ADUser
    {
        public string? Id { get; set; }
        public string? DisplayName { get; set; }
        public string? GivenName { get; set; }
        public string? Surname { get; set; }
        public string? UserPrincipalName { get; set; }
        public string? Mail { get; set; }
        public string? JobTitle { get; set; }
        public string? Department { get; set; }
        public string? OfficeLocation { get; set; }
        public string? MobilePhone { get; set; }
        public string? CompanyName { get; set; }
    }

    public class ADUserSearchResponse
    {
        public List<ADUser> Users { get; set; } = new();
        public int Count { get; set; }
    }
}
