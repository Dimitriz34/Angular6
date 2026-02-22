IF OBJECT_ID(N'dbo.sel_topfiveappsutilisation', N'P') IS NOT NULL
    DROP PROCEDURE [dbo].[sel_topfiveappsutilisation];
GO

CREATE PROCEDURE dbo.sel_topfiveappsutilisation
AS
BEGIN
    SET NOCOUNT ON

    SELECT TOP 5
        ROW_NUMBER() OVER (ORDER BY COUNT(e.emailid) DESC, a.appname ASC) AS Ranking,
        a.appcode       AS AppId,
        a.appname       AS AppName,
        ISNULL(a.appdesc, '')           AS Description,
        ISNULL(a.appowner, '')          AS AppOwner,
        ISNULL(a.fromemailaddress, '')  AS FromEmailAddress,
        a.emailserviceid                AS EmailServiceId,
        a.userid                        AS UserId,
        a.createddatetime               AS CreatedDateTime,
        a.active                        AS Active,
        COUNT(e.emailid)                AS Last10DaysEmail
    FROM dbo.tpm_application a WITH (NOLOCK)
    LEFT JOIN dbo.tpm_email e WITH (NOLOCK)
        ON a.appcode = e.appcode
        AND e.active = 1
        AND e.status = 'Sent'
        AND e.createddatetime >= DATEADD(DAY, -10, GETUTCDATE())
    WHERE a.active = 1
    GROUP BY a.appcode, a.appname, a.appdesc, a.appowner, a.fromemailaddress,
             a.emailserviceid, a.userid, a.createddatetime, a.active
    HAVING COUNT(e.emailid) > 0
    ORDER BY COUNT(e.emailid) DESC, a.appname ASC
END

GO
