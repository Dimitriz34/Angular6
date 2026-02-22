IF OBJECT_ID(N'dbo.commit_email', N'P') IS NOT NULL
    DROP PROCEDURE [dbo].[commit_email];
GO

CREATE PROCEDURE dbo.commit_email
    @emailid            UNIQUEIDENTIFIER = NULL OUTPUT,
    @userid             UNIQUEIDENTIFIER,
    @appcode            INT,
    @senderfrom         NVARCHAR(500) = NULL,
    @replyto            NVARCHAR(500) = NULL,
    @recipients         NVARCHAR(MAX) = NULL,
    @ccrecipients       NVARCHAR(MAX) = NULL,
    @bccrecipients      NVARCHAR(MAX) = NULL,
    @subject            NVARCHAR(1000) = NULL,
    @body               NVARCHAR(MAX) = NULL,
    @ishtmlbody         BIT = 0,
    @priority           NVARCHAR(50) = 'Normal',
    @status             NVARCHAR(50) = 'Pending',
    @scheduleddatetime  DATETIME = NULL,
    @trackingid         NVARCHAR(255) = NULL,
    @keyversion         INT = NULL,
    @modifiedby         NVARCHAR(100) = 'SYSTEM'
AS
BEGIN
    SET NOCOUNT ON

    -- Default keyversion to current active key if not provided
    IF @keyversion IS NULL
        SELECT TOP 1 @keyversion = id FROM dbo.tpm_keyconfig WHERE active = 1 ORDER BY id DESC

    IF @emailid IS NULL
    BEGIN
        SET @emailid = NEWID()

        INSERT INTO dbo.tpm_email (
            emailid, userid, appcode, senderfrom, replyto, recipients,
            ccrecipients, bccrecipients, subject, body, ishtmlbody,
            priority, status, scheduleddatetime, trackingid, keyversion, createddatetime, createdby
        )
        VALUES (
            @emailid, @userid, @appcode, @senderfrom, @replyto, @recipients,
            @ccrecipients, @bccrecipients, @subject, @body, @ishtmlbody,
            @priority, @status, @scheduleddatetime, @trackingid, @keyversion, GETUTCDATE(), @modifiedby
        )
    END
    ELSE
    BEGIN
        UPDATE dbo.tpm_email
        SET
            senderfrom = @senderfrom,
            replyto = @replyto,
            recipients = @recipients,
            ccrecipients = @ccrecipients,
            bccrecipients = @bccrecipients,
            subject = @subject,
            body = @body,
            ishtmlbody = @ishtmlbody,
            priority = @priority,
            status = @status,
            scheduleddatetime = @scheduleddatetime,
            trackingid = @trackingid,
            keyversion = ISNULL(@keyversion, keyversion),
            modifieddatetime = GETUTCDATE(),
            modifiedby = @modifiedby
        WHERE emailid = @emailid
    END

    SELECT @emailid AS emailid
END

GO
