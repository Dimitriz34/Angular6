using static TPEmail.BusinessModels.Enums.CommonUtils;

namespace TPEmail.BusinessModels.ResponseModels
{
    public class EmailServiceLookup
    {
        public int? Id { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public EmailServiceType Type { get; set; }
        public bool Active { get; set; }
        public DateTime CreationDateTime { get; set; }
        public DateTime ModificationDateTime { get; set; }
        public string? CreatedBy { get; set; }
        public string? ModifiedBy { get; set; }
    }
}
