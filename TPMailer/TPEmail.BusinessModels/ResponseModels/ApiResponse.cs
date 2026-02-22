using TPEmail.BusinessModels.Constants;

namespace TPEmail.BusinessModels.ResponseModels
{
    /// <summary>
    /// Standardized API Response Model - All endpoints must return this format
    /// resultCode: Status of operation (1=Success, 0=Validation Failure, -1=System Error, specific codes for business logic)
    /// resultData: Array of result objects/data
    /// resultMessages: Array of user-friendly messages
    /// </summary>
    public class ApiResponse<T>
    {
        public int resultCode { get; set; }
        public T[] resultData { get; set; } = Array.Empty<T>();
        public string[] resultMessages { get; set; } = Array.Empty<string>();

        public ApiResponse() { }

        public ApiResponse(int code, T[]? data = null, string[]? messages = null)
        {
            resultCode = code;
            resultData = data ?? Array.Empty<T>();
            resultMessages = messages ?? Array.Empty<string>();
        }

        public static ApiResponse<T> Success(T[] data, string[] messages = null)
        {
            return new ApiResponse<T>(1, data, messages ?? new[] { MessageConstants.OperationSuccessful });
        }

        public static ApiResponse<T> ValidationFailure(string[] messages)
        {
            return new ApiResponse<T>(0, Array.Empty<T>(), messages);
        }

        public static ApiResponse<T> SystemError(string[] messages)
        {
            return new ApiResponse<T>(-1, Array.Empty<T>(), messages ?? new[] { MessageConstants.UnexpectedErrorOccurred });
        }
    }

}
