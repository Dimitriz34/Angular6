namespace TPEmail.BusinessModels.Enums
{
    public enum UserDetailsType
    {
        USER_NOT_FOUND,
        USER_ALREADY_EXISTS,
        UNAUTHORIZED,
        INVALID_CREDENTIALS,
        AZURE_AD_USER  // User must authenticate via Azure AD (no password)
    }

    public enum RoleType
    {
        UNKNOWN = 0,
        MODERATOR = 1,
        USER = 2,
        ADMIN = 3,
        TESTER = 4,
    }

    public class UserUtils
    {
    }
}
