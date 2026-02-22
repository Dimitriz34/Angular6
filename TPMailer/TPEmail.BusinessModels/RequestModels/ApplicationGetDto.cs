namespace TPEmail.BusinessModels.RequestModels
{

    public class ApplicationGetDto
    {
        public int? Id { get; set; }
        public string AppName { get; set; } = string.Empty;
        public string AppDescription { get; set; } = string.Empty;
        public string AppOwner { get; set; } = string.Empty;
        public string OwnerEmail { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string AppClient { get; set; } = string.Empty;
        public int EmailServiceId { get; set; }
        public int Active { get; set; }
        public bool IsVerified { get; set; }
    }
}
