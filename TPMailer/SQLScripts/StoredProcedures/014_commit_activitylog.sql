IF OBJECT_ID(N'dbo.commit_activitylog', N'P') IS NOT NULL
    DROP PROCEDURE [dbo].[commit_activitylog];
GO

CREATE PROCEDURE dbo.commit_activitylog
    @userid         UNIQUEIDENTIFIER = NULL,
    @appcode        INT = NULL,
    @logtypeid      INT = NULL,
    @action         NVARCHAR(255),
    @description    NVARCHAR(MAX) = NULL,
    @entitytype     NVARCHAR(100) = NULL,
    @entityid       NVARCHAR(255) = NULL,
    @ipaddress      NVARCHAR(50) = NULL,
    @useragent      NVARCHAR(500) = NULL,
    @requestpath    NVARCHAR(1000) = NULL,
    @requestmethod  NVARCHAR(10) = NULL,
    @responsecode   INT = NULL,
    @durationms     INT = NULL,
    @createdby      NVARCHAR(100) = 'SYSTEM'
AS
BEGIN
    SET NOCOUNT ON

    INSERT INTO dbo.tpm_activitylog (
        userid, appcode, logtypeid, action, description, entitytype, entityid,
        ipaddress, useragent, requestpath, requestmethod, responsecode, durationms, createddatetime, createdby
    )
    VALUES (
        @userid, @appcode, @logtypeid, @action, @description, @entitytype, @entityid,
        @ipaddress, @useragent, @requestpath, @requestmethod, @responsecode, @durationms, GETUTCDATE(), @createdby
    )

    SELECT SCOPE_IDENTITY() AS id
END

GO
