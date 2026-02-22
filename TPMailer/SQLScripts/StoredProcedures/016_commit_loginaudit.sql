IF OBJECT_ID(N'dbo.commit_loginaudit', N'P') IS NOT NULL
    DROP PROCEDURE [dbo].[commit_loginaudit];
GO

CREATE PROCEDURE dbo.commit_loginaudit
    @userid         UNIQUEIDENTIFIER = NULL,
    @email          NVARCHAR(500) = NULL,
    @upn            NVARCHAR(500) = NULL,
    @logintype      NVARCHAR(50) = NULL,
    @success        BIT = 0,
    @failurereason  NVARCHAR(500) = NULL,
    @ipaddress      NVARCHAR(50) = NULL,
    @useragent      NVARCHAR(500) = NULL,
    @deviceinfo     NVARCHAR(500) = NULL,
    @location       NVARCHAR(255) = NULL,
    @sessionid      NVARCHAR(255) = NULL,
    @tokenexpiry    DATETIME = NULL
AS
BEGIN
    SET NOCOUNT ON

    INSERT INTO dbo.tpm_loginaudit (
        userid, email, upn, logintype, success, failurereason,
        ipaddress, useragent, deviceinfo, location, sessionid, tokenexpiry, createddatetime
    )
    VALUES (
        @userid, @email, @upn, @logintype, @success, @failurereason,
        @ipaddress, @useragent, @deviceinfo, @location, @sessionid, @tokenexpiry, GETUTCDATE()
    )

    SELECT SCOPE_IDENTITY() AS id
END

GO
