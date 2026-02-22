IF OBJECT_ID(N'dbo.sel_application', N'P') IS NOT NULL
    DROP PROCEDURE [dbo].[sel_application];
GO

-- ============================================================================
-- sel_application: Retrieves applications with server-side pagination and filtering
--
-- Filter Parameters:
--   @appcode    - Filter by specific application ID
--   @appclient  - Filter by AppClientId (GUID) - used for token authentication
--   @appname    - Filter by application name (partial match)
--   @userid     - Filter by owner user ID
--   @searchterm - Unified search across appname
--   @active     - Filter by active status (NULL = all, 1 = active, 0 = inactive)
--
-- Pagination Parameters:
--   @pageindex  - Page number (1-based, default: 1)
--   @pagesize   - Records per page (default: 10)
--
-- Note: Sensitive fields (appowner, owneremail, emailserver, port) are stored encrypted.
--       Decryption is handled at the service layer.
-- ============================================================================
CREATE PROCEDURE dbo.sel_application
    @appcode    INT = NULL,
    @appclient  UNIQUEIDENTIFIER = NULL,
    @appname    NVARCHAR(255) = NULL,
    @userid     UNIQUEIDENTIFIER = NULL,
    @searchterm NVARCHAR(255) = NULL,
    @active     BIT = NULL,
    @pageindex  INT = 1,
    @pagesize   INT = 10
AS
BEGIN
    SET NOCOUNT ON

    IF @userid IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.tpm_user WITH (NOLOCK) WHERE userid = @userid AND active = 1)
    BEGIN
        RETURN
    END

    SELECT
        appcode AS Id,
        appname AS AppName,
        appdesc AS Description,
        appclient AS AppClient,
        appclientdefault AS AppClientDefault,
        appsecret AS AppSecret,
        ISNULL(isencrypted, 0) AS IsEncrypted,
        tenantid AS TenantId,
        userid AS UserId,
        appowner AS AppOwner,
        owneremail AS OwnerEmail,
        fromemailaddress AS FromEmailAddress,
        fromdisplayname AS FromEmailDisplayName,
        emailserver AS EmailServer,
        port AS EncryptedPort,
        emailserviceid AS EmailServiceId,
        coowner AS CoOwner,
        coowneremail AS CoOwnerEmail,
        EmailServiceName = CASE ISNULL(emailserviceid, 0)
            WHEN 0 THEN 'TP Internal'
            WHEN 1 THEN 'O365'
            WHEN 2 THEN 'Mailkit'
            WHEN 3 THEN 'Exchange Server'
            WHEN 4 THEN 'SendGrid'
            ELSE 'TP Internal'
        END,
        ISNULL(isinternalapp, 0) AS IsInternalApp,
        ISNULL(usetpassist, 0) AS UseTPAssist,
        encryptedfields AS EncryptedFields,
        keyversion AS KeyVersion,
        active AS Active,
        createddatetime AS CreatedDateTime,
        createdby AS CreatedBy,
        modifieddatetime AS ModifiedDateTime,
        modifiedby AS ModifiedBy
    FROM dbo.tpm_application WITH (NOLOCK)
    WHERE (@active IS NULL OR active = @active)
        AND (@appcode IS NULL OR appcode = @appcode)
        AND (@appclient IS NULL OR appclient = @appclient)
        AND (@userid IS NULL OR userid = @userid)
        AND (@appname IS NULL OR appname LIKE '%' + @appname + '%')
        AND (@searchterm IS NULL OR appname LIKE '%' + @searchterm + '%')
    ORDER BY appname
    OFFSET (@pageindex - 1) * @pagesize ROWS
    FETCH NEXT @pagesize ROWS ONLY
END

GO
