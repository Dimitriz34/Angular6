IF OBJECT_ID(N'dbo.commit_refreshtoken', N'P') IS NOT NULL
    DROP PROCEDURE [dbo].[commit_refreshtoken];
GO

CREATE PROCEDURE dbo.commit_refreshtoken
    @action VARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;

    IF @action = 'CLEANUP'
    BEGIN
        DECLARE @DeletedCount INT;

        DELETE FROM dbo.tpm_refreshtoken
        WHERE (expiresat < GETUTCDATE() AND revoked = 0)   -- expired but never revoked
           OR (revoked = 1 AND revokedat < DATEADD(DAY, -30, GETUTCDATE())); -- revoked > 30 days ago

        SET @DeletedCount = @@ROWCOUNT;
        SELECT @DeletedCount;
    END
END

GO
