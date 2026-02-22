IF OBJECT_ID(N'dbo.sel_userrole', N'P') IS NOT NULL
    DROP PROCEDURE [dbo].[sel_userrole];
GO

CREATE PROCEDURE dbo.sel_userrole
    @userid     UNIQUEIDENTIFIER = NULL,
    @roleid     INT = NULL
AS
BEGIN
    SET NOCOUNT ON

    SELECT
        ur.id, ur.userid, ur.roleid, r.name AS rolename,
        r.description AS roledescription, ur.createddatetime,
        u.active AS isapproved
    FROM dbo.tpm_userrole ur WITH (NOLOCK)
    INNER JOIN dbo.tpm_role r WITH (NOLOCK) ON ur.roleid = r.id AND r.active = 1
    INNER JOIN dbo.tpm_user u WITH (NOLOCK) ON ur.userid = u.userid
    WHERE ur.active = 1
        AND (@userid IS NULL OR ur.userid = @userid)
        AND (@roleid IS NULL OR ur.roleid = @roleid)
END

GO
