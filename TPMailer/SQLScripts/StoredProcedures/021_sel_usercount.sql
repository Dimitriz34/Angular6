IF OBJECT_ID(N'dbo.sel_usercount', N'P') IS NOT NULL
    DROP PROCEDURE [dbo].[sel_usercount];
GO

-- ============================================================================
-- sel_usercount: Returns user count with optional filtering
--
-- Filter Parameters:
--   @appcode    - Filter by application code
--   @searchterm - Unified search across: username, upn, email (partial match)
--   @roleid     - Filter by role ID
--   @active     - Filter by active status (NULL = all, 1 = active, 0 = inactive)
-- ============================================================================
CREATE PROCEDURE dbo.sel_usercount
    @appcode    INT = NULL,
    @searchterm NVARCHAR(255) = NULL,
    @roleid     INT = NULL,
    @active     BIT = NULL
AS
BEGIN
    SET NOCOUNT ON

    SELECT
        COUNT(DISTINCT u.userid) AS totalcount,
        SUM(CASE WHEN u.active = 1 THEN 1 ELSE 0 END) AS activecount,
        SUM(CASE WHEN u.active = 0 THEN 1 ELSE 0 END) AS inactivecount
    FROM dbo.tpm_user u WITH (NOLOCK)
    LEFT JOIN dbo.tpm_userrole ur WITH (NOLOCK) ON u.userid = ur.userid
    WHERE (@appcode IS NULL OR u.appcode = @appcode)
        AND (@active IS NULL OR u.active = @active)
        AND (@roleid IS NULL OR ur.roleid = @roleid)
        AND (@searchterm IS NULL OR
             u.upn LIKE '%' + @searchterm + '%' OR
             u.username LIKE '%' + @searchterm + '%' OR
             u.email LIKE '%' + @searchterm + '%')
END

GO
