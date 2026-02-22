IF OBJECT_ID(N'dbo.sel_user', N'P') IS NOT NULL
    DROP PROCEDURE [dbo].[sel_user];
GO

CREATE PROCEDURE dbo.sel_user
    @userid             UNIQUEIDENTIFIER = NULL,
    @emailblindindex    NVARCHAR(128) = NULL,
    @upn                NVARCHAR(1000) = NULL,
    @appcode            INT = NULL,
    @active             BIT = NULL,
    @searchterm         NVARCHAR(255) = NULL,
    @roleid             INT = NULL,
    @sortby             NVARCHAR(50) = NULL,
    @pageindex          INT = 1,
    @pagesize           INT = 10
AS
BEGIN
    SET NOCOUNT ON

    ;WITH FilteredUsers AS (
        SELECT DISTINCT u.userid, u.email, u.emailblindindex, u.emailencversion,
               u.upn, u.upnblindindex, u.upnencversion, u.username,
               u.appsecret, u.salt, u.encryptionkey, u.appcode,
               u.active, u.createddatetime, u.createdby, u.modifieddatetime, u.modifiedby,
               CASE
                   WHEN u.appsecret IS NULL OR u.appsecret = '' OR u.salt IS NULL OR u.salt = ''
                       THEN 'AZURE_AD'
                   ELSE 'PASSWORD'
               END AS authtype
        FROM dbo.tpm_user u WITH (NOLOCK)
        LEFT JOIN dbo.tpm_userrole ur WITH (NOLOCK) ON u.userid = ur.userid
        WHERE (@active IS NULL OR u.active = @active)
            AND (@userid IS NULL OR u.userid = @userid)
            AND (@emailblindindex IS NULL OR u.emailblindindex = @emailblindindex)
            AND (@upn IS NULL OR LOWER(u.upn) = LOWER(@upn))
            AND (@appcode IS NULL OR u.appcode = @appcode)
            AND (@roleid IS NULL OR ur.roleid = @roleid)
            AND (@searchterm IS NULL OR
                 u.upn LIKE '%' + @searchterm + '%' OR
                 u.username LIKE '%' + @searchterm + '%' OR
                 u.email LIKE '%' + @searchterm + '%')
    )
    SELECT *
    FROM FilteredUsers
    ORDER BY
        CASE WHEN @sortby = 'date_asc' THEN createddatetime END ASC,
        CASE WHEN @sortby = 'name_asc' THEN username END ASC,
        CASE WHEN @sortby = 'name_desc' THEN username END DESC,
        CASE WHEN @sortby IS NULL OR @sortby = 'date_desc' OR @sortby = '' THEN createddatetime END DESC
    OFFSET (@pageindex - 1) * @pagesize ROWS
    FETCH NEXT @pagesize ROWS ONLY
END

GO
