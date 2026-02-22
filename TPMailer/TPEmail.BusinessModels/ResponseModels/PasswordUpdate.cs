namespace TPEmail.BusinessModels.ResponseModels
{
    public class PasswordUpdate
    {
        public Guid UserId { get; set; }
        public int NoOfUpdate { get; set; }
        public DateTime CreationDateTime { get; set; }
        public DateTime ModificationDateTime { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string ModifiedBy { get; set; } = string.Empty;
    }
}
