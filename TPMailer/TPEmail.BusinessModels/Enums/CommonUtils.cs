using System.ComponentModel.DataAnnotations;

namespace TPEmail.BusinessModels.Enums
{
    public static class CommonUtils
    {
        public enum Default
        {
            [Display(Name = "Inactive")]
            INACTIVE,

            [Display(Name = "Active")]
            ACTIVE
        }

        public enum Services
        {
            TPInternalRelay = 0,
            O365 = 1,
            Mailkit = 2,
            ExchangeServer = 3,
            SendGrid = 4,
        }

        public enum RecipientType
        {
            To = 1,
            Cc = 2,
        }

        public enum EmailServiceType
        {
            [Display(Name = "Free")]
            FREE,

            [Display(Name = "Premium")]
            PREMIUM
        }
    }
}
