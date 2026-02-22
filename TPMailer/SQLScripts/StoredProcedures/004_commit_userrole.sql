IF OBJECT_ID(N'dbo.commit_userrole', N'P') IS NOT NULL
    DROP PROCEDURE [dbo].[commit_userrole];
GO

CREATE PROCEDURE dbo.commit_userrole
    @userid     UNIQUEIDENTIFIER,
    @roleid     INT,
    @createdby  NVARCHAR(100) = 'SYSTEM'
AS
BEGIN
    SET NOCOUNT ON

    IF NOT EXISTS (
        SELECT 1 FROM dbo.tpm_userrole 
        WHERE userid = @userid AND roleid = @roleid AND active = 1
    )
    BEGIN
        INSERT INTO dbo.tpm_userrole (userid, roleid, createddatetime, createdby)
        VALUES (@userid, @roleid, GETUTCDATE(), @createdby)
    END

    SELECT 1 AS success
END

GO
