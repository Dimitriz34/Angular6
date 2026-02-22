IF OBJECT_ID(N'dbo.nlog_addentryp', N'P') IS NOT NULL
    DROP PROCEDURE [dbo].[nlog_addentryp];
GO

CREATE PROCEDURE dbo.nlog_addentryp
    @machineName    NVARCHAR(200) = NULL,
    @logged         DATETIME = NULL,
    @level          NVARCHAR(50) = NULL,
    @message        NVARCHAR(MAX) = NULL,
    @logger         NVARCHAR(400) = NULL,
    @properties     NVARCHAR(MAX) = NULL,
    @callsite       NVARCHAR(400) = NULL,
    @exception      NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON

    INSERT INTO dbo.tpm_nlog (
        machinename,
        logged,
        level,
        message,
        logger,
        properties,
        callsite,
        exception
    )
    VALUES (
        @machineName,
        COALESCE(@logged, GETUTCDATE()),
        COALESCE(@level, 'Info'),
        @message,
        @logger,
        @properties,
        @callsite,
        @exception
    )
END

GO
