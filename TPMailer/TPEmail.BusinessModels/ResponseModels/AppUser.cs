namespace TPEmail.BusinessModels.ResponseModels
{
    public class AppUser
    {
        public Guid? UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? EmailBlindIndex { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? Upn { get; set; }
        public string? AppSecret { get; set; }  // Null for Azure AD users
        public string? Salt { get; set; }  // Null for Azure AD users
        public string? EncryptionKey { get; set; }  // Null for Azure AD users
        public int AppCode { get; set; }
        public int Active { get; set; }
        public DateTime CreationDateTime { get; set; }
        public DateTime ModificationDateTime { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string ModifiedBy { get; set; } = string.Empty;

        public int? RoleId { get; set; }
    }

    public class AppUserRole
    {
        public Guid UserId { get; set; }
        public int RoleId { get; set; }
    }

}
