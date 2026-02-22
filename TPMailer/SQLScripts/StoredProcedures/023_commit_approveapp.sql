IF OBJECT_ID(N'dbo.commit_approveapp', N'P') IS NOT NULL
    DROP PROCEDURE [dbo].[commit_approveapp];
GO

CREATE PROCEDURE dbo.commit_approveapp
    @appcode    INT,
    @modifiedby NVARCHAR(100) = 'SYSTEM'
AS
BEGIN
    SET NOCOUNT ON

    UPDATE dbo.tpm_application
    SET
        active = 1,
        modifieddatetime = GETUTCDATE(),
        modifiedby = @modifiedby
    WHERE appcode = @appcode

    SELECT 1 AS success
END

GO
