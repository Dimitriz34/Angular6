IF OBJECT_ID(N'dbo.sel_emaildetail', N'P') IS NOT NULL
    DROP PROCEDURE [dbo].[sel_emaildetail];
GO

-- ============================================================================
-- sel_emaildetail: Retrieves complete email details by emailid
--
-- Purpose: Get full email information including body, recipients, attachments
--          for viewing email details in the UI
-- ============================================================================
CREATE PROCEDURE dbo.sel_emaildetail
    @emailid UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON

    SELECT
        e.emailid,
        e.userid,
        e.appcode,
        e.senderfrom,
        e.replyto,
        e.recipients,
        e.ccrecipients,
        e.bccrecipients,
        e.subject,
        e.body,
        e.ishtmlbody,
        e.priority,
        e.status,
        e.errorcode,
        e.errormessage,
        e.retrycount,
        e.maxretry,
        e.scheduleddatetime,
        e.sentdatetime,
        e.trackingid,
        e.graphmessageid,
        e.active,
        e.createddatetime,
        e.createdby,
        e.modifieddatetime,
        e.modifiedby,
        a.appname,
        a.emailserviceid,
        ServiceName = CASE ISNULL(a.emailserviceid, 0)
            WHEN 0 THEN 'TP Internal'
            WHEN 1 THEN 'O365'
            WHEN 2 THEN 'Mailkit'
            WHEN 3 THEN 'Exchange Server'
            WHEN 4 THEN 'SendGrid'
            ELSE 'TP Internal'
        END,
        u.upn AS upn,
        u.username AS username,
        e.keyversion
    FROM dbo.tpm_email e WITH (NOLOCK)
    LEFT JOIN dbo.tpm_application a WITH (NOLOCK) ON e.appcode = a.appcode
    LEFT JOIN dbo.tpm_user u WITH (NOLOCK) ON e.userid = u.userid
    WHERE e.emailid = @emailid
END

GO
