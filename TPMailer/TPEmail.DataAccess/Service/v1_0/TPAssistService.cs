using System.Text;
using System.Text.Json;
using TPEmail.BusinessModels.Constants;

namespace TPEmail.DataAccess.Service.v1_0
{
    public static class TPAssistService
    {
       
    }

    public class TPAssistResult
    {
        public bool Success { get; set; }
        public string Body { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
        public bool IsHtml { get; set; }
    }
}
