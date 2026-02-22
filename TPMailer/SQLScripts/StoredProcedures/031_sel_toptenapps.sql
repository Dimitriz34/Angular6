IF OBJECT_ID(N'dbo.sel_toptenapps', N'P') IS NOT NULL
    DROP PROCEDURE [dbo].[sel_toptenapps];
GO

CREATE PROCEDURE dbo.sel_toptenapps
AS
BEGIN
    SET NOCOUNT ON

    DECLARE @TotalEmails INT = 0

    SELECT @TotalEmails = COUNT(*)
    FROM dbo.tpm_email WITH (NOLOCK)
    WHERE active = 1 AND status = 'Sent'

    ;WITH AppEmailCounts AS (
        SELECT
            a.appcode AS AppId,
            a.appname AS AppName,
            COUNT(e.emailid) AS TotalSentEmail,
            SUM(CASE WHEN e.createddatetime >= DATEADD(DAY, -30, GETUTCDATE()) THEN 1 ELSE 0 END) AS LastThirtyDaysEmail,
            SUM(CASE WHEN e.createddatetime >= DATEADD(DAY, -7, GETUTCDATE()) THEN 1 ELSE 0 END) AS LastSevenDaysEmail,
            SUM(CASE WHEN CAST(e.createddatetime AS DATE) = CAST(GETUTCDATE() AS DATE) THEN 1 ELSE 0 END) AS TodayEmail
        FROM dbo.tpm_application a WITH (NOLOCK)
        INNER JOIN dbo.tpm_email e WITH (NOLOCK) ON a.appcode = e.appcode AND e.active = 1 AND e.status = 'Sent'
        WHERE a.active = 1
        GROUP BY a.appcode, a.appname
        HAVING COUNT(e.emailid) > 0
    )
    SELECT TOP 10
        ROW_NUMBER() OVER (ORDER BY TotalSentEmail DESC, AppName ASC) AS Ranking,
        AppId,
        AppName,
        TotalSentEmail,
        LastThirtyDaysEmail,
        LastSevenDaysEmail,
        TodayEmail,
        CASE
            WHEN @TotalEmails > 0 THEN CAST(ROUND((CAST(TotalSentEmail AS FLOAT) / @TotalEmails) * 100, 2) AS DECIMAL(5,2))
            ELSE 0.00
        END AS PercentageOfTotal
    FROM AppEmailCounts
    ORDER BY TotalSentEmail DESC, AppName ASC
END

GO
