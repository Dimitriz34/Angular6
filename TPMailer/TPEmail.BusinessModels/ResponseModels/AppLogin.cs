namespace TPEmail.BusinessModels.ResponseModels
{

    public class AppLogin
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string IPAddress { get; set; } = string.Empty;
        public DateTime CreatedDateTime { get; set; }

        public DateTime LoginDateTime
        {
            get { return CreatedDateTime.ToLocalTime(); }
        }

        public int Success { get; set; }
    }
}
