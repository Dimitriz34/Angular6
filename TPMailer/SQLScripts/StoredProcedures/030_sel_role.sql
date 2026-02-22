IF OBJECT_ID(N'dbo.sel_role', N'P') IS NOT NULL
    DROP PROCEDURE [dbo].[sel_role];
GO

CREATE PROCEDURE dbo.sel_role
    @roleid     INT = NULL
AS
BEGIN
    SET NOCOUNT ON

    SELECT
        r.id,
        r.name,
        r.description,
        r.active
    FROM dbo.tpm_role r WITH (NOLOCK)
    WHERE r.active = 1
        AND (@roleid IS NULL OR r.id = @roleid)
END

GO
