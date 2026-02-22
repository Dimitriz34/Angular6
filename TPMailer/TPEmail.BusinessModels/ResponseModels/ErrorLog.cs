namespace TPEmail.BusinessModels.ResponseModels
{

    public class ErrorLog
    {
        public int Id { get; set; }
        public string Error { get; set; } = string.Empty;
        public string ErrorSource { get; set; } = string.Empty;
        public string LoggedBy { get; set; } = string.Empty;
        public DateTime LoggedDateTime { get; set; }
    }
}
