namespace TPEmail.BusinessModels.RequestModels
{
    /// <summary>
    /// Request model for approving/updating an app user
    /// </summary>
    public class UpdateAppUserApprovalRequest
    {
        public Guid UserId { get; set; }
        public int Active { get; set; } = 1;
    }
}
