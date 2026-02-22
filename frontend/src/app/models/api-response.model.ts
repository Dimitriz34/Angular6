/**
 * Unified API Response Model - All API endpoints return this format
 * resultCode: Status of operation (1=Success, 0=Validation Failure, -1=System Error, specific codes for business logic)
 * resultData: Array of result objects/data
 * resultMessages: Array of user-friendly messages
 */
export interface ApiResponse<T> {
  resultCode: number;
  resultData: T[];
  resultMessages: string[];
}

/**
 * Result codes used across all API endpoints
 * 1 = Success, 0 = Validation/Business Failure, -1 = System Error
 */
export class ResultCodes {
  static readonly SUCCESS = 1;                          // Operation completed successfully
  static readonly VALIDATION_FAILURE = 0;               // Request validation or business logic failure
  static readonly SYSTEM_ERROR = -1;                    // Unexpected system/technical error
}
