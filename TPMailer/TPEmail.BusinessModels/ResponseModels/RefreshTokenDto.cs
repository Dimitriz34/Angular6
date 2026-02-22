namespace TPEmail.BusinessModels.ResponseModels
{
    public class RefreshTokenDto
    {
        public Guid TokenId { get; set; }
        public Guid UserId { get; set; }
        public DateTime IssuedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public string CreatedByIp { get; set; } = string.Empty;
        public bool Revoked { get; set; }
        public DateTime? RevokedAt { get; set; }
        public string? RevokedByIp { get; set; }
        public Guid? ReplacedByToken { get; set; }
        public string? ReasonRevoked { get; set; }
    }
}
