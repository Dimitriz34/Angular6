namespace TPEmail.BusinessModels.Constants
{
    public static class MessageConstants
    {
        // Error Messages
        public const string NoValidEmailId = "No valid email id is provided";
        public const string AuthenticationFailedAtEmailServer = "Authentication failed at email server";
        public const string EmailSendingFailed = "Email sending failed. Please check your SMTP configuration and credentials.";
        public const string EncryptionKeyNotLoaded = "Encryption key configuration is not loaded. Database connection may have failed at startup.";
        public const string InternalServerError = "Internal Server Error. Please try again later.";
        public const string Unauthorized = "Unauthorized";
        public const string UnexpectedErrorOccurred = "An unexpected error occurred.";
        public const string InvalidFileExtension = "Invalid file extension";
        public const string AttachmentSizeExceedsLimitFormat = "Total attachment size exceeds the maximum allowed limit of {0} MB.";
        public const string JwtKeyNotConfigured = "JWT key is not configured.";
        public const string InvalidCredentials = "Invalid credentials";
        public const string ConfigurationNotInitialized = "Configuration has not been initialized.";
        
        // Application Messages
        public const string ApplicationNotFoundInDatabase = "Application not found in database";
        public const string ApplicationNotFound = "Application not found";
        public const string OwnerEmailRequired = "Owner email is required to send guidance email";
        public const string AppSecretRequired = "App Secret is required to send guidance email";
        public const string SmtpAppPasswordRequired = "SMTP App Password is required to send guidance email";
        public const string GuidanceEmailSentSuccessfully = "Guidance email sent successfully";
        public const string InvalidAppClientConfiguration = "Invalid app client configuration. Ensure you authenticated using /api/AppUser/app-authenticate with valid AppClientId and AppSecret.";
        public const string ServerNameRequiredForServiceFormat = "Server name is required for {0}";
        public const string AlreadyExistsFormat = "{0} already exists!";
        public const string EmailSentSuccessfully = "Email sent successfully.";
        public const string EmailSentSuccessfullyManualTest = "Email sent successfully (Manual Test)";
        public const string EmailListRetrievedSuccessfully = "Email list retrieved successfully.";
        public const string EmailRetrievedSuccessfully = "Email retrieved successfully.";
        public const string NoEmailFoundWithIdFormat = "No email found with ID: {0}";
        
        // Validation Error Messages
        public const string EmailSubjectRequired = "Email subject is required";
        public const string EmailRecipientRequired = "Email recipient(s) is required";
        public const string FromEmailRequired = "From email address is required";
        public const string EmailServiceNameRequired = "Email service name is required";
        public const string ApplicationNameRequired = "Application name is required";
        public const string DescriptionRequired = "Description is required";
        public const string AppOwnerRequired = "App Owner is required";
        public const string OwnerEmailAddressRequired = "Owner Email is required";
        public const string InvalidEmailAddress = "Invalid Email Address";
        public const string EmailServiceRequired = "Email Service is required";
        public const string UserIdRequired = "User ID is required";
        public const string FromEmailAddressRequired = "From Email Address is required";
        public const string OperationSuccessful = "Operation successful.";
        public const string ValidEmailRecipientRequired = "Valid email recipient(s) is required";
        public const string EmailIsRequired = "Email is required.";
        public const string PasswordIsRequired = "Password is required.";
        public const string PasswordCannotBeNullOrEmpty = "Password cannot be null or empty.";
        public const string EmailServiceListRetrievedSuccessfully = "Email service list retrieved successfully.";
        public const string EmailServiceNotFound = "Email service not found.";
        public const string EmailServiceRetrievedSuccessfully = "Email service retrieved successfully.";
        public const string EmailServiceAddedSuccessfully = "Email service added successfully.";
        public const string EmailServiceUpdatedSuccessfully = "Email service updated successfully.";
        public const string NoEmailFoundWithFormat = "No email found with: {0}";
        public const string EmailServiceLookupNotFoundForIdFormat = "EmailServiceLookup not found for id {0}";
        public const string UserAlreadyExists = "This email is already registered. Please use a different email or try logging in.";
        public const string RegistrationSuccessfulPendingApproval = "Registration successful. Your account is pending approval. Please wait for administrator confirmation.";
        public const string AuthenticationSuccessful = "Authentication successful.";
        public const string NoRegisteredApplicationFoundWithClientId = "No registered application found with the provided client ID.";
        public const string InvalidApplicationCredentials = "Invalid application credentials.";
        public const string ApplicationNotActiveFormat = "Application '{0}' is not active. Please contact the administrator.";
        public const string NoUserFoundForApplication = "No user found for this application.";
        public const string ApplicationNotFoundInUserList = "Application not found in user's application list.";
        public const string UserListRetrievedSuccessfully = "User list retrieved successfully.";
        public const string UserNotFound = "User not found.";
        public const string UserRetrievedSuccessfully = "User retrieved successfully.";
        public const string UserApprovalUpdatedSuccessfully = "User approval updated successfully.";
        public const string UserCredentialsUpdatedSuccessfully = "User credentials updated successfully.";
        public const string UserInfoRetrievedSuccessfully = "User info retrieved successfully.";
        public const string RoleListRetrievedSuccessfully = "Role list retrieved successfully.";
        public const string UserRoleUpdatedSuccessfully = "User role updated successfully.";
        public const string InvalidEmailOrPassword = "Invalid email or password.";
        public const string UserPendingApprovalContactAdmin = "Your account has been registered but is pending approval. Please contact the administrator.";
        public const string UserWithEmailNotFoundFormat = "User with email '{0}' not found.";
        public const string NoUserFoundWithEmailFormat = "No user found with email: {0}";
        public const string ApplicationAlreadyExistsFormat = "Application '{0}' already exists.";
        public const string ApplicationCreatedSuccessfully = "Application created successfully.";
        public const string ApplicationUpdatedSuccessfully = "Application updated successfully.";
        public const string ApplicationApprovalUpdatedSuccessfully = "Application approval updated successfully.";
        public const string UserApplicationListRetrievedSuccessfully = "User application list retrieved successfully.";
        public const string ApplicationListRetrievedSuccessfully = "Application list retrieved successfully.";
        public const string ApplicationNotFoundMessage = "Application not found.";
        public const string ApplicationRetrievedSuccessfully = "Application retrieved successfully.";
        public const string ApplicationIsActive = "Application is ACTIVE";
        public const string ApplicationIsInactiveNeedsApproval = "Application is INACTIVE - needs admin approval";
        public const string ApplicationDiagnosticDataRetrievedSuccessfully = "Application diagnostic data retrieved successfully.";
        public const string ActivityLogListRetrievedSuccessfully = "Activity log list retrieved successfully.";
        public const string DashboardDataRetrievedSuccessfully = "Dashboard data retrieved successfully.";
        public const string RowCountRetrievedSuccessfully = "Row count retrieved successfully.";
        public const string EmailSentSuccessfullyManualTestEndpoint = "Email sent successfully (Manual Test)";
        public const string SuccessValue = "success";
        
        // Validation Messages for Data Models
        public const string EmailAddressValidationRequired = "Email is required";
        public const string ValidEmailAddressRequired = "Please enter a valid email address";
        public const string UsernameRequired = "Username is required";
        public const string UpnRequired = "UPN is required";
        public const string ConfirmPasswordMismatch = "Confirm password does not match!";
        public const string PasswordRequired = "Password is required";
        public const string PasswordLengthValidation = "Password length should be minimum: {0} and maximum: {1} character";
        public const string InvalidFromEmailAddress = "Invalid From Email Address";
        public const string FromEmailDisplayNameRequired = "From Email Display Name is required";
        public const string EmailServerRequired = "Email Server is required";
        public const string PortRequired = "Port is required";
        public const string InvalidPortNumber = "Invalid Port number";
        public const string ToEmailAddressRequired = "To email address is required";
        public const string AppClientIdRequired = "App client id is required";
        public const string AppSecretValidationRequired = "App Secret is required";
        
        // TPAssist Messages
        public const string TPAssistDefaultMessage = "I can help with TP Mailer features like applications, emails, users, and more.";
        public const string KnowledgeBaseReloadedFormat = "Knowledge base reloaded. {0} entries indexed.";
        public const string AnswerNotFound = "Answer not found.";
        public const string AdministratorAccessRequired = "This requires administrator access.";
        public const string EmailBodyRequiredForTPAssist = "Email body is required for TPAssist enhancement";
        public const string EmailBodyEnhancedSuccessfully = "Email body enhanced successfully by TPAssist";
        public const string TPAssistEnhancementFailedFormat = "TPAssist enhancement failed";
        
        // Email Test Messages
        public const string ManualSmtpTestEmailBody = "This is a test email sent from the manual SMTP test endpoint (SendMailTest2).";
        public const string SmtpTestEmailSubject = "SMTP Test Email (Manual)";
        
        // Search Messages
        public const string SearchTermRequired = "Search term is required";
        public const string NoMatchesFound = "No matches found. Try different keywords:";
        
        // Data Access Layer Messages
        public const string EmailServiceTypesStatic = "Email service types are static and cannot be modified. See CommonUtils.Services enum.";
        public const string PasswordRequiredForCredentialUpdate = "Password is required for credential update";
        public const string AzureADAuthenticationSuccessful = "Azure AD authentication successful. Welcome!";
        public const string SendInternalSmtpRelayEmailFailed = "SendInternalSmtpRelayEmail failed - check diagnostic email for details";
        public const string TPAssistConfigurationMissing = "TPAssist configuration missing. Check environment variables.";
        public const string AzureADAuthenticationRequired = "This account uses Azure AD authentication. Please use the 'Login with Azure AD' option.";
        public const string PasswordAuthenticationRequired = "This account uses password authentication. Please use the standard login form.";
        public const string UPNRequiredForAzureAD = "UPN is required for Azure AD authentication";
        public const string FailedToCreateAzureADUserAccount = "Failed to create Azure AD user account";
        public const string AzureADAuthenticationError = "An error occurred during Azure AD authentication";
        
        // Auth Messages
        public const string RefreshTokenRequired = "Refresh token is required";
        public const string LoggedOutSuccessfully = "Logged out successfully";
        public const string LogoutErrorOccurred = "An error occurred during logout";
        public const string LogoutReason = "Logout";
        public const string InvalidUserIdFormat = "Invalid user ID format";
        public const string SystemUser = "SYSTEM";
        
        // Common Layer Messages
        public const string EncryptionFailedFormat = "Encryption failed in EncryptionHelper: {0}";
        public const string DecryptionFailedFormat = "Decryption failed in EncryptionHelper: {0}";
        public const string AesEncryptionFailedFormat = "Encryption failed: {0}";
        public const string AesDecryptionFailedFormat = "Decryption failed: {0}";
    }
}
