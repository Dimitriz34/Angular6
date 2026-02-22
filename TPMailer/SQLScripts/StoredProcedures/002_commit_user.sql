IF OBJECT_ID(N'dbo.commit_user', N'P') IS NOT NULL
    DROP PROCEDURE [dbo].[commit_user];
GO

CREATE PROCEDURE dbo.commit_user
    @userid             UNIQUEIDENTIFIER = NULL OUTPUT,
    @email              NVARCHAR(MAX),
    @emailblindindex    NVARCHAR(128) = NULL,
    @emailencversion    INT = 1,
    @upn                NVARCHAR(1000) = NULL,
    @upnblindindex      NVARCHAR(128) = NULL,
    @upnencversion      INT = NULL,
    @username           NVARCHAR(1000) = NULL,
    @appsecret          NVARCHAR(MAX) = NULL,
    @salt               NVARCHAR(MAX) = NULL,
    @encryptionkey      NVARCHAR(MAX) = NULL,
    @appcode            INT = NULL,
    @active             BIT = 0,
    @modifiedby         NVARCHAR(100) = 'SYSTEM'
AS
BEGIN
    SET NOCOUNT ON

    IF @userid IS NULL
    BEGIN
        SET @userid = NEWID()
        
        INSERT INTO dbo.tpm_user (
            userid, email, emailblindindex, emailencversion, upn, upnblindindex,
            upnencversion, username, appsecret, salt, encryptionkey, appcode,
            active, createddatetime, createdby
        )
        VALUES (
            @userid, @email, @emailblindindex, @emailencversion, @upn, @upnblindindex,
            @upnencversion, @username, @appsecret, @salt, @encryptionkey, @appcode,
            @active, GETUTCDATE(), @modifiedby
        )
    END
    ELSE
    BEGIN
        UPDATE dbo.tpm_user
        SET
            email = @email,
            emailblindindex = @emailblindindex,
            emailencversion = @emailencversion,
            upn = @upn,
            upnblindindex = @upnblindindex,
            upnencversion = @upnencversion,
            username = @username,
            appsecret = ISNULL(@appsecret, appsecret),
            salt = ISNULL(@salt, salt),
            encryptionkey = ISNULL(@encryptionkey, encryptionkey),
            appcode = @appcode,
            active = @active,
            modifieddatetime = GETUTCDATE(),
            modifiedby = @modifiedby
        WHERE userid = @userid
    END

    SELECT @userid AS userid
END

GO
