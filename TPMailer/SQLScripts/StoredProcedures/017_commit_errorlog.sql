IF OBJECT_ID(N'dbo.commit_errorlog', N'P') IS NOT NULL
    DROP PROCEDURE [dbo].[commit_errorlog];
GO

CREATE PROCEDURE dbo.commit_errorlog
    @userid         UNIQUEIDENTIFIER = NULL,
    @appcode        INT = NULL,
    @errorcode      NVARCHAR(50) = NULL,
    @errormessage   NVARCHAR(MAX) = NULL,
    @stacktrace     NVARCHAR(MAX) = NULL,
    @innerexception NVARCHAR(MAX) = NULL,
    @source         NVARCHAR(500) = NULL,
    @targetsite     NVARCHAR(500) = NULL,
    @severity       NVARCHAR(20) = 'Error',
    @requestpath    NVARCHAR(1000) = NULL,
    @requestmethod  NVARCHAR(10) = NULL,
    @requestbody    NVARCHAR(MAX) = NULL,
    @ipaddress      NVARCHAR(50) = NULL,
    @useragent      NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON

    INSERT INTO dbo.tpm_errorlog (
        userid, appcode, errorcode, errormessage, stacktrace, innerexception,
        source, targetsite, severity, requestpath, requestmethod, requestbody,
        ipaddress, useragent, createddatetime
    )
    VALUES (
        @userid, @appcode, @errorcode, @errormessage, @stacktrace, @innerexception,
        @source, @targetsite, @severity, @requestpath, @requestmethod, @requestbody,
        @ipaddress, @useragent, GETUTCDATE()
    )

    SELECT SCOPE_IDENTITY() AS id
END

GO
