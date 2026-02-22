IF OBJECT_ID(N'dbo.commit_emailstatus', N'P') IS NOT NULL
    DROP PROCEDURE [dbo].[commit_emailstatus];
GO

CREATE PROCEDURE dbo.commit_emailstatus
    @emailid        UNIQUEIDENTIFIER,
    @status         NVARCHAR(50),
    @errorcode      NVARCHAR(50) = NULL,
    @errormessage   NVARCHAR(MAX) = NULL,
    @graphmessageid NVARCHAR(500) = NULL,
    @modifiedby     NVARCHAR(100) = 'SYSTEM'
AS
BEGIN
    SET NOCOUNT ON

    UPDATE dbo.tpm_email
    SET
        status = @status,
        errorcode = @errorcode,
        errormessage = @errormessage,
        graphmessageid = @graphmessageid,
        sentdatetime = CASE WHEN @status = 'Sent' THEN GETUTCDATE() ELSE sentdatetime END,
        retrycount = CASE WHEN @status = 'Failed' THEN retrycount + 1 ELSE retrycount END,
        modifieddatetime = GETUTCDATE(),
        modifiedby = @modifiedby
    WHERE emailid = @emailid

    SELECT 1 AS success
END

GO
