IF OBJECT_ID(N'dbo.commit_deleteuser', N'P') IS NOT NULL
    DROP PROCEDURE [dbo].[commit_deleteuser];
GO

CREATE PROCEDURE dbo.commit_deleteuser
    @userid     UNIQUEIDENTIFIER,
    @modifiedby NVARCHAR(100) = 'SYSTEM'
AS
BEGIN
    SET NOCOUNT ON

    UPDATE dbo.tpm_user
    SET
        active = 0,
        modifieddatetime = GETUTCDATE(),
        modifiedby = @modifiedby
    WHERE userid = @userid

    UPDATE dbo.tpm_userrole
    SET
        active = 0,
        modifieddatetime = GETUTCDATE(),
        modifiedby = @modifiedby
    WHERE userid = @userid

    SELECT 1 AS success
END

GO
