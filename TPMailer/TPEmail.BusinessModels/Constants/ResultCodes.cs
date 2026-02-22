namespace TPEmail.BusinessModels.Constants
{
    /// <summary>
    /// Standard Result Codes for API Responses
    /// All endpoints must use these codes in ApiResponse.resultCode
    /// 1 = Success, 0 = Validation/Business Failure, -1 = System Error
    /// </summary>
    public static class ResultCodes
    {
        public const int Success = 1;                          // Operation completed successfully
        public const int ValidationFailure = 0;                // Request validation or business logic failure
        public const int SystemError = -1;                     // Unexpected system/technical error
    }
}
