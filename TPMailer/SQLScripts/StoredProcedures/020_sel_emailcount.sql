IF OBJECT_ID(N'dbo.sel_emailcount', N'P') IS NOT NULL
    DROP PROCEDURE [dbo].[sel_emailcount];
GO

CREATE PROCEDURE dbo.sel_emailcount
    @UserId UNIQUEIDENTIFIER = NULL
AS
BEGIN
    SET NOCOUNT ON

    ;WITH AppStats AS (
        SELECT
            a.appcode AS AppId,
            a.appname AS AppName,
            COUNT(e.emailid) AS TotalSentEmail,
            SUM(CASE WHEN e.createddatetime >= DATEADD(DAY, -30, GETUTCDATE()) THEN 1 ELSE 0 END) AS LastThirtyDaysEmail,
            SUM(CASE WHEN e.createddatetime >= DATEADD(DAY, -7, GETUTCDATE()) THEN 1 ELSE 0 END) AS LastSevenDaysEmail,
            SUM(CASE WHEN CAST(e.createddatetime AS DATE) = CAST(GETUTCDATE() AS DATE) THEN 1 ELSE 0 END) AS TodayEmail,
            SUM(CASE WHEN e.createddatetime >= DATEFROMPARTS(YEAR(GETUTCDATE()), MONTH(GETUTCDATE()), 1) THEN 1 ELSE 0 END) AS MonthlyEmail,
            SUM(CASE WHEN e.createddatetime >= DATEFROMPARTS(YEAR(GETUTCDATE()), 1, 1) THEN 1 ELSE 0 END) AS YearlyEmail
        FROM dbo.tpm_application a WITH (NOLOCK)
        LEFT JOIN dbo.tpm_email e WITH (NOLOCK) ON a.appcode = e.appcode
            AND e.active = 1
            AND e.status = 'Sent'
            AND (@UserId IS NULL OR e.userid = @UserId)
        WHERE a.active = 1
            AND (@UserId IS NULL OR a.userid = @UserId)
        GROUP BY a.appcode, a.appname
    )
    SELECT
        0 AS AppId,
        'ALL' AS AppName,
        ISNULL(SUM(TotalSentEmail), 0) AS TotalSentEmail,
        ISNULL(SUM(LastThirtyDaysEmail), 0) AS LastThirtyDaysEmail,
        ISNULL(SUM(LastSevenDaysEmail), 0) AS LastSevenDaysEmail,
        ISNULL(SUM(TodayEmail), 0) AS TodayEmail,
        ISNULL(SUM(MonthlyEmail), 0) AS MonthlyEmail,
        ISNULL(SUM(YearlyEmail), 0) AS YearlyEmail
    FROM AppStats

    UNION ALL

    SELECT
        AppId,
        AppName,
        TotalSentEmail,
        LastThirtyDaysEmail,
        LastSevenDaysEmail,
        TodayEmail,
        MonthlyEmail,
        YearlyEmail
    FROM AppStats
    ORDER BY AppId
END

GO
