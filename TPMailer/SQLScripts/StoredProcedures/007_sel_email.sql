IF OBJECT_ID(N'dbo.sel_email', N'P') IS NOT NULL
    DROP PROCEDURE [dbo].[sel_email];
GO

-- ============================================================================
-- sel_email: Retrieves emails with pagination, filtering, and service name
--
-- Features:
--   - ServiceName column derived from tpm_application.emailserviceid
--   - Service names: O365 (1), Mailkit (2), Exchange Server (3), SendGrid (4)
--   - User details (upn, username) from tpm_user join
--   - @countonly parameter: when 1, returns COUNT(*) only (for pagination)
-- ============================================================================
CREATE PROCEDURE dbo.sel_email
    @emailid        UNIQUEIDENTIFIER = NULL,
    @userid         UNIQUEIDENTIFIER = NULL,
    @appcode        INT = NULL,
    @appname        NVARCHAR(255) = NULL,
    @status         NVARCHAR(50) = NULL,
    @trackingid     NVARCHAR(255) = NULL,
    @searchterm     NVARCHAR(255) = NULL,
    @datefrom       DATETIME = NULL,
    @dateto         DATETIME = NULL,
    @active         BIT = 1,
    @pageindex      INT = 1,
    @pagesize       INT = 10,
    @countonly       BIT = 0
AS
BEGIN
    SET NOCOUNT ON

    -- Fast path: single email lookup by PK
    IF @emailid IS NOT NULL
    BEGIN
        IF @countonly = 1
        BEGIN
            SELECT COUNT(*) FROM dbo.tpm_email WHERE emailid = @emailid AND (@active IS NULL OR active = @active)
            RETURN
        END

        SELECT
            e.emailid AS EmailId, e.userid AS UserId, e.appcode AS AppId,
            e.senderfrom AS Sender, e.senderfrom AS FromEmailAddress,
            e.subject AS Subject, e.body AS Body, e.ishtmlbody AS IsHtml,
            e.status AS Status, e.errorcode AS ErrorCode, e.errormessage AS ErrorMessage,
            e.active AS Active, e.createdby AS CreatedBy,
            e.createddatetime AS CreationDateTime,
            ISNULL(e.modifieddatetime, e.createddatetime) AS ModificationDateTime,
            a.appname AS AppName,
            ServiceName = CASE ISNULL(a.emailserviceid, 0)
                WHEN 0 THEN 'TP Internal' WHEN 1 THEN 'O365' WHEN 2 THEN 'Mailkit'
                WHEN 3 THEN 'Exchange Server' WHEN 4 THEN 'SendGrid' ELSE 'TP Internal' END,
            u.upn AS Upn, u.username AS Username, e.keyversion AS KeyVersion
        FROM dbo.tpm_email e WITH (NOLOCK)
        LEFT JOIN dbo.tpm_application a WITH (NOLOCK) ON e.appcode = a.appcode
        LEFT JOIN dbo.tpm_user u WITH (NOLOCK) ON e.userid = u.userid
        WHERE e.emailid = @emailid AND (@active IS NULL OR e.active = @active)
        RETURN
    END

    -- Fast path: tracking ID lookup
    IF @trackingid IS NOT NULL
    BEGIN
        IF @countonly = 1
        BEGIN
            SELECT COUNT(*) FROM dbo.tpm_email WHERE trackingid = @trackingid AND (@active IS NULL OR active = @active)
            RETURN
        END

        SELECT
            e.emailid AS EmailId, e.userid AS UserId, e.appcode AS AppId,
            e.senderfrom AS Sender, e.senderfrom AS FromEmailAddress,
            e.subject AS Subject, e.body AS Body, e.ishtmlbody AS IsHtml,
            e.status AS Status, e.errorcode AS ErrorCode, e.errormessage AS ErrorMessage,
            e.active AS Active, e.createdby AS CreatedBy,
            e.createddatetime AS CreationDateTime,
            ISNULL(e.modifieddatetime, e.createddatetime) AS ModificationDateTime,
            a.appname AS AppName,
            ServiceName = CASE ISNULL(a.emailserviceid, 0)
                WHEN 0 THEN 'TP Internal' WHEN 1 THEN 'O365' WHEN 2 THEN 'Mailkit'
                WHEN 3 THEN 'Exchange Server' WHEN 4 THEN 'SendGrid' ELSE 'TP Internal' END,
            u.upn AS Upn, u.username AS Username, e.keyversion AS KeyVersion
        FROM dbo.tpm_email e WITH (NOLOCK)
        LEFT JOIN dbo.tpm_application a WITH (NOLOCK) ON e.appcode = a.appcode
        LEFT JOIN dbo.tpm_user u WITH (NOLOCK) ON e.userid = u.userid
        WHERE e.trackingid = @trackingid AND (@active IS NULL OR e.active = @active)
        ORDER BY e.createddatetime DESC
        OFFSET (@pageindex - 1) * @pagesize ROWS FETCH NEXT @pagesize ROWS ONLY
        RETURN
    END

    -- Validate user exists
    IF @userid IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.tpm_user WITH (NOLOCK) WHERE userid = @userid AND active = 1)
    BEGIN
        IF @countonly = 1
            SELECT 0
        RETURN
    END

    -- General query path with all filters
    IF @countonly = 1
    BEGIN
        SELECT COUNT(*)
        FROM dbo.tpm_email e WITH (NOLOCK)
        LEFT JOIN dbo.tpm_application a WITH (NOLOCK) ON e.appcode = a.appcode
        LEFT JOIN dbo.tpm_user u WITH (NOLOCK) ON e.userid = u.userid
        WHERE (@active IS NULL OR e.active = @active)
            AND (@userid IS NULL OR e.userid = @userid)
            AND (@appcode IS NULL OR e.appcode = @appcode)
            AND (@appname IS NULL OR a.appname LIKE '%' + @appname + '%')
            AND (@status IS NULL OR e.status = @status)
            AND (@searchterm IS NULL OR (
                 a.appname LIKE '%' + @searchterm + '%' OR
                 u.upn LIKE '%' + @searchterm + '%' OR
                 u.username LIKE '%' + @searchterm + '%' OR
                 e.subject LIKE '%' + @searchterm + '%'))
            AND (@datefrom IS NULL OR e.createddatetime >= @datefrom)
            AND (@dateto IS NULL OR e.createddatetime <= @dateto)
        RETURN
    END

    SELECT
        e.emailid AS EmailId, e.userid AS UserId, e.appcode AS AppId,
        e.senderfrom AS Sender, e.senderfrom AS FromEmailAddress,
        e.subject AS Subject, e.body AS Body, e.ishtmlbody AS IsHtml,
        e.status AS Status, e.errorcode AS ErrorCode, e.errormessage AS ErrorMessage,
        e.active AS Active, e.createdby AS CreatedBy,
        e.createddatetime AS CreationDateTime,
        ISNULL(e.modifieddatetime, e.createddatetime) AS ModificationDateTime,
        a.appname AS AppName,
        ServiceName = CASE ISNULL(a.emailserviceid, 0)
            WHEN 0 THEN 'TP Internal' WHEN 1 THEN 'O365' WHEN 2 THEN 'Mailkit'
            WHEN 3 THEN 'Exchange Server' WHEN 4 THEN 'SendGrid' ELSE 'TP Internal' END,
        u.upn AS Upn, u.username AS Username, e.keyversion AS KeyVersion
    FROM dbo.tpm_email e WITH (NOLOCK)
    LEFT JOIN dbo.tpm_application a WITH (NOLOCK) ON e.appcode = a.appcode
    LEFT JOIN dbo.tpm_user u WITH (NOLOCK) ON e.userid = u.userid
    WHERE (@active IS NULL OR e.active = @active)
        AND (@userid IS NULL OR e.userid = @userid)
        AND (@appcode IS NULL OR e.appcode = @appcode)
        AND (@appname IS NULL OR a.appname LIKE '%' + @appname + '%')
        AND (@status IS NULL OR e.status = @status)
        AND (@searchterm IS NULL OR (
             a.appname LIKE '%' + @searchterm + '%' OR
             u.upn LIKE '%' + @searchterm + '%' OR
             u.username LIKE '%' + @searchterm + '%' OR
             e.subject LIKE '%' + @searchterm + '%'))
        AND (@datefrom IS NULL OR e.createddatetime >= @datefrom)
        AND (@dateto IS NULL OR e.createddatetime <= @dateto)
    ORDER BY e.createddatetime DESC
    OFFSET (@pageindex - 1) * @pagesize ROWS
    FETCH NEXT @pagesize ROWS ONLY
END

GO
