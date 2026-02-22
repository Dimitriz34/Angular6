IF OBJECT_ID(N'dbo.commit_secretlog', N'P') IS NOT NULL
    DROP PROCEDURE [dbo].[commit_secretlog];
GO

CREATE PROCEDURE dbo.commit_secretlog
    @userid         UNIQUEIDENTIFIER,
    @noofupdate     INT = 0,
    @createdby      NVARCHAR(100) = 'SYSTEM',
    @modifiedby     NVARCHAR(100) = 'SYSTEM'
AS
BEGIN
    SET NOCOUNT ON

    INSERT INTO dbo.tpm_secretupdate (
        userid, updatetype, reason, requestedby, status, createddatetime
    )
    VALUES (
        @userid, 
        CASE WHEN @noofupdate = 0 THEN 'INITIAL_CREATION' ELSE 'SECRET_ROTATION' END,
        CASE WHEN @noofupdate = 0 THEN 'Initial credential creation' ELSE 'Password update' END,
        @createdby,
        'Completed',
        GETUTCDATE()
    )

    SELECT SCOPE_IDENTITY() AS id
END

GO
