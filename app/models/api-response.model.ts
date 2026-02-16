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
 */
export class ResultCodes {
  static readonly SUCCESS = 1;                          // Operation completed successfully
  static readonly VALIDATION_FAILURE = 0;               // Request validation failed
  static readonly SYSTEM_ERROR = -1;                    // Unexpected system/technical error

  // Authentication & Authorization Result Codes
  static readonly USER_NOT_FOUND = 100;                 // User does not exist
  static readonly INVALID_CREDENTIALS = 101;            // Email/password mismatch
  static readonly USER_NOT_APPROVED = 102;              // User registered but not approved yet
  static readonly USER_INACTIVE = 103;                  // User account is inactive/disabled
  static readonly UNAUTHORIZED = 104;                   // User lacks required permissions
  static readonly TOKEN_EXPIRED = 105;                  // JWT token has expired
  static readonly TOKEN_INVALID = 106;                  // JWT token is invalid/malformed
  static readonly AZURE_AD_LOGIN_REQUIRED = 107;        // User must use Azure AD login (no password)

  // User Management Result Codes
  static readonly USER_ALREADY_EXISTS = 200;            // User email already registered
  static readonly REGISTRATION_SUCCESS = 201;           // User registration successful
  static readonly EMAIL_SEND_FAILED = 202;              // Email sending failed
  static readonly REGISTRATION_FAILURE = 203;           // User registration failed
  static readonly SERVER_ERROR = 204;                   // Server-side error during operation

  // Application Management Result Codes
  static readonly APPLICATION_NOT_FOUND = 300;          // Application does not exist
  static readonly APPLICATION_ALREADY_EXISTS = 301;     // Application name already exists
  static readonly APPLICATION_CREATED = 302;            // Application created successfully

  // Email Management Result Codes
  static readonly EMAIL_NOT_FOUND = 400;                // Email not found
  static readonly EMAIL_SEND_ERROR = 401;               // Error sending email
  static readonly RECIPIENT_NOT_FOUND = 402;            // Email recipient not found

  // Data Codes
  static readonly DATA_NOT_FOUND = 500;                 // Requested data not found
  static readonly DATA_CONFLICT = 501;                  // Data conflict/duplicate
  static readonly PERMISSION_DENIED = 502;              // User does not have permission for this action
}
