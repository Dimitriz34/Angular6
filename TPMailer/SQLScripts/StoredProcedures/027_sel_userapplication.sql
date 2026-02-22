IF OBJECT_ID(N'dbo.sel_userapplication', N'P') IS NOT NULL
    DROP PROCEDURE [dbo].[sel_userapplication];
GO

-- =====================================================================
-- sel_userapplication: Retrieves applications owned by a specific user
-- =====================================================================
CREATE PROCEDURE dbo.sel_userapplication
    @userid     UNIQUEIDENTIFIER = NULL,
    @searchterm NVARCHAR(255) = NULL,
    @pageindex  INT = 1,
    @pagesize   INT = 50
AS
BEGIN
    SET NOCOUNT ON

    IF @userid IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.tpm_user WITH (NOLOCK) WHERE userid = @userid AND active = 1)
    BEGIN
        RETURN
    END

    SELECT
        a.appcode,
        a.appname,
        a.appdesc,
        a.appclient,
        a.appclientdefault,
        a.tenantid,
        a.active,
        a.createddatetime,
        a.createdby,
        a.modifieddatetime,
        a.modifiedby
    FROM dbo.tpm_application a WITH (NOLOCK)
    WHERE (@userid IS NULL OR a.userid = @userid)
        AND (@searchterm IS NULL OR a.appname LIKE '%' + @searchterm + '%')
    ORDER BY a.appname
    OFFSET (@pageindex - 1) * @pagesize ROWS
    FETCH NEXT @pagesize ROWS ONLY
END

GO
