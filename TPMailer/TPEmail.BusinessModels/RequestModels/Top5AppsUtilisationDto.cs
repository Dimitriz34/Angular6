namespace TPEmail.BusinessModels.RequestModels
{
    public class Top5AppsUtilisationDto
    {
        public int Ranking { get; set; }
        public int AppId { get; set; }
        public string AppName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string AppOwner { get; set; } = string.Empty;
        public string FromEmailAddress { get; set; } = string.Empty;
        public int? EmailServiceId { get; set; }
        public string? UserId { get; set; }
        public DateTime CreatedDateTime { get; set; }
        public bool Active { get; set; }
        public int Last10DaysEmail { get; set; }
    }
}
