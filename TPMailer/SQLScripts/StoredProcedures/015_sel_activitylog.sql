IF OBJECT_ID(N'dbo.sel_activitylog', N'P') IS NOT NULL
    DROP PROCEDURE [dbo].[sel_activitylog];
GO

CREATE PROCEDURE dbo.sel_activitylog
    @userid         UNIQUEIDENTIFIER = NULL,
    @appcode        INT = NULL,
    @action         NVARCHAR(255) = NULL,
    @datefrom       DATETIME = NULL,
    @dateto         DATETIME = NULL,
    @pageindex      INT = 1,
    @pagesize       INT = 100,
    @countonly       BIT = 0
AS
BEGIN
    SET NOCOUNT ON

    IF @countonly = 1
    BEGIN
        SELECT COUNT(*) FROM dbo.tpm_activitylog a WITH (NOLOCK)
        WHERE a.active = 1
            AND (@userid IS NULL OR a.userid = @userid)
            AND (@appcode IS NULL OR a.appcode = @appcode)
            AND (@action IS NULL OR a.action LIKE '%' + @action + '%')
            AND (@datefrom IS NULL OR a.createddatetime >= @datefrom)
            AND (@dateto IS NULL OR a.createddatetime <= @dateto)
        RETURN
    END

    SELECT
        a.id            AS LogId,
        a.logtypeid     AS LogTypeLookupId,
        lt.typename     AS LogTypeLookup,
        a.description   AS Description,
        a.requestpath   AS Url,
        a.createdby     AS LoggedBy,
        a.createddatetime AS LoggedDateTime
    FROM dbo.tpm_activitylog a WITH (NOLOCK)
    LEFT JOIN dbo.tpm_logtype lt WITH (NOLOCK) ON a.logtypeid = lt.id
    WHERE a.active = 1
        AND (@userid IS NULL OR a.userid = @userid)
        AND (@appcode IS NULL OR a.appcode = @appcode)
        AND (@action IS NULL OR a.action LIKE '%' + @action + '%')
        AND (@datefrom IS NULL OR a.createddatetime >= @datefrom)
        AND (@dateto IS NULL OR a.createddatetime <= @dateto)
    ORDER BY a.createddatetime DESC
    OFFSET (@pageindex - 1) * @pagesize ROWS
    FETCH NEXT @pagesize ROWS ONLY
END

GO
