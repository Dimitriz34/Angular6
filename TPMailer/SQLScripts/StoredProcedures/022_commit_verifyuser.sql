IF OBJECT_ID(N'dbo.commit_verifyuser', N'P') IS NOT NULL
    DROP PROCEDURE [dbo].[commit_verifyuser];
GO

CREATE PROCEDURE dbo.commit_verifyuser
    @userid     UNIQUEIDENTIFIER,
    @active     BIT = 1,
    @modifiedby NVARCHAR(100) = 'SYSTEM'
AS
BEGIN
    SET NOCOUNT ON

    UPDATE dbo.tpm_user
    SET
        active = @active,
        modifieddatetime = GETUTCDATE(),
        modifiedby = @modifiedby
    WHERE userid = @userid

    SELECT 1 AS success
END

GO
