IF OBJECT_ID(N'dbo.commit_application', N'P') IS NOT NULL
    DROP PROCEDURE [dbo].[commit_application];
GO

CREATE PROCEDURE dbo.commit_application
    @appcode            INT = NULL OUTPUT,
    @appname            NVARCHAR(255),
    @appdesc            NVARCHAR(1000) = NULL,
    @appclient          UNIQUEIDENTIFIER = NULL,
    @appclientdefault   NVARCHAR(255) = NULL,
    @appsecret          NVARCHAR(500) = NULL,
    @isencrypted        BIT = 0,
    @tenantid           NVARCHAR(500) = NULL,
    @userid             NVARCHAR(100) = NULL,
    @appowner           NVARCHAR(500) = NULL,
    @owneremail         NVARCHAR(500) = NULL,
    @fromemailaddress   NVARCHAR(255) = NULL,
    @fromdisplayname    NVARCHAR(255) = NULL,
    @emailserver        NVARCHAR(500) = NULL,
    @port               NVARCHAR(100) = NULL,
    @emailserviceid     INT = NULL,
    @isinternalapp      BIT = 0,
    @usetpassist        BIT = 0,
    @coowner            NVARCHAR(500) = NULL,
    @coowneremail       NVARCHAR(500) = NULL,
    @encryptedfields    NVARCHAR(500) = NULL,
    @keyversion         INT = NULL,
    @active             BIT = 1,
    @modifiedby         NVARCHAR(100) = 'SYSTEM'
AS
BEGIN
    SET NOCOUNT ON

    -- Default keyversion to current active key if not provided
    IF @keyversion IS NULL
        SELECT TOP 1 @keyversion = id FROM dbo.tpm_keyconfig WHERE active = 1 ORDER BY id DESC

    IF @appcode IS NULL
    BEGIN
        INSERT INTO dbo.tpm_application (
            appname, appdesc, appclient, appclientdefault, appsecret, isencrypted, tenantid,
            userid, appowner, owneremail, fromemailaddress, fromdisplayname,
            emailserver, port, emailserviceid, isinternalapp, usetpassist, coowner, coowneremail, encryptedfields, keyversion, active, createddatetime, createdby
        )
        VALUES (
            @appname, @appdesc, @appclient, @appclientdefault, @appsecret, @isencrypted, @tenantid,
            @userid, @appowner, @owneremail, @fromemailaddress, @fromdisplayname,
            @emailserver, @port, @emailserviceid, @isinternalapp, @usetpassist, @coowner, @coowneremail, @encryptedfields, @keyversion, @active, GETUTCDATE(), @modifiedby
        )

        SET @appcode = SCOPE_IDENTITY()
    END
    ELSE
    BEGIN
        UPDATE dbo.tpm_application
        SET
            appname = @appname,
            appdesc = @appdesc,
            appclient = @appclient,
            appclientdefault = @appclientdefault,
            appsecret = COALESCE(@appsecret, appsecret),
            isencrypted = COALESCE(@isencrypted, isencrypted),
            tenantid = @tenantid,
            userid = @userid,
            appowner = @appowner,
            owneremail = @owneremail,
            fromemailaddress = @fromemailaddress,
            fromdisplayname = @fromdisplayname,
            emailserver = @emailserver,
            port = @port,
            emailserviceid = @emailserviceid,
            isinternalapp = @isinternalapp,
            usetpassist = @usetpassist,
            coowner = @coowner,
            coowneremail = @coowneremail,
            encryptedfields = @encryptedfields,
            keyversion = ISNULL(@keyversion, keyversion),
            active = @active,
            modifieddatetime = GETUTCDATE(),
            modifiedby = @modifiedby
        WHERE appcode = @appcode
    END
END

GO
