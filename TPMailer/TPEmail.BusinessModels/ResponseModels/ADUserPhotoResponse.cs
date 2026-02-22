namespace TPEmail.BusinessModels.ResponseModels
{
    public class ADUserPhotoResponse
    {
        public bool Success { get; set; }
        public string? Data { get; set; }
        public int? StatusCode { get; set; }
        public string? Error { get; set; }
    }
}
