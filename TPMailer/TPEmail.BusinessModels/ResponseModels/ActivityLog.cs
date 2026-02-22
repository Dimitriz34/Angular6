using System.ComponentModel.DataAnnotations;

namespace TPEmail.BusinessModels.ResponseModels
{

    public class ActivityLog {
        public int LogId { get; set; }
        public int LogTypeLookupId { get; set; }
        public Operation LogTypeLookup { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string LoggedBy { get; set; } = string.Empty;
        public DateTime LoggedDateTime { get; set; }
    }

    public enum Operation
    {
        UNKNOWN,
        INSERT,
        UPDATE,
        DELETE,
        RETRIEVE,
        SIGNIN,
        SIGNOUT,
        REGISTRATION,
    }
}
