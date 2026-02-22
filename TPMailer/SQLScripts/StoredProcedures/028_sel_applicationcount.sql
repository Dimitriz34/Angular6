IF OBJECT_ID(N'dbo.sel_applicationcount', N'P') IS NOT NULL
    DROP PROCEDURE [dbo].[sel_applicationcount];
GO

-- ============================================================================
-- sel_applicationcount: Returns filtered count of applications
-- Supports the same filters as sel_application for consistent pagination
--
-- Note: Search is only performed on unencrypted fields (appname) since
-- appowner and owneremail are now stored encrypted.
-- ============================================================================
CREATE PROCEDURE dbo.sel_applicationcount
    @appcode    INT = NULL,
    @userid     UNIQUEIDENTIFIER = NULL,
    @searchterm NVARCHAR(255) = NULL,
    @active     BIT = NULL
AS
BEGIN
    SET NOCOUNT ON

    IF @userid IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.tpm_user WITH (NOLOCK) WHERE userid = @userid AND active = 1)
    BEGIN
        SELECT 0 AS TotalCount, 0 AS ActiveCount, 0 AS InactiveCount
        RETURN
    END

    SELECT
        COUNT(*) AS TotalCount,
        SUM(CASE WHEN active = 1 THEN 1 ELSE 0 END) AS ActiveCount,
        SUM(CASE WHEN active = 0 THEN 1 ELSE 0 END) AS InactiveCount
    FROM dbo.tpm_application WITH (NOLOCK)
    WHERE (@active IS NULL OR active = @active)
        AND (@appcode IS NULL OR appcode = @appcode)
        AND (@userid IS NULL OR userid = @userid)
        AND (@searchterm IS NULL OR appname LIKE '%' + @searchterm + '%')
END

GO
