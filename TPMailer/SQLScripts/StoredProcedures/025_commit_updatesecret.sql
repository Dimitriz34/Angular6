IF OBJECT_ID(N'dbo.commit_updatesecret', N'P') IS NOT NULL
    DROP PROCEDURE [dbo].[commit_updatesecret];
GO

CREATE PROCEDURE dbo.commit_updatesecret
    @userid         UNIQUEIDENTIFIER,
    @appsecret      NVARCHAR(MAX),
    @salt           NVARCHAR(MAX) = NULL,
    @encryptionkey  NVARCHAR(MAX) = NULL,
    @modifiedby     NVARCHAR(100) = 'SYSTEM'
AS
BEGIN
    SET NOCOUNT ON

    DECLARE @oldkeyhash NVARCHAR(256)
    SELECT @oldkeyhash = CONVERT(NVARCHAR(256), HASHBYTES('SHA2_256', appsecret), 2) FROM dbo.tpm_user WHERE userid = @userid

    UPDATE dbo.tpm_user
    SET
        appsecret = @appsecret,
        salt = ISNULL(@salt, salt),
        encryptionkey = ISNULL(@encryptionkey, encryptionkey),
        modifieddatetime = GETUTCDATE(),
        modifiedby = @modifiedby
    WHERE userid = @userid AND active = 1

    INSERT INTO dbo.tpm_secretupdate (
        userid, updatetype, oldkeyhash, newkeyhash, reason, requestedby, status
    )
    VALUES (
        @userid, 'SECRET_ROTATION', @oldkeyhash,
        CONVERT(NVARCHAR(256), HASHBYTES('SHA2_256', @appsecret), 2), 'Secret rotation', @modifiedby, 'Completed'
    )

    SELECT 1 AS success
END

GO
