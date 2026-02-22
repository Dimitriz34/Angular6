IF OBJECT_ID(N'dbo.tpm_notification', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tpm_notification] (
    [id] BIGINT IDENTITY(1,1) NOT NULL,
    [userid] UNIQUEIDENTIFIER NULL,
    [notificationtype] NVARCHAR(50) NOT NULL,
    [title] NVARCHAR(255) NOT NULL,
    [message] NVARCHAR(MAX) NULL,
    [priority] NVARCHAR(20) DEFAULT ('Normal') NULL,
    [isread] BIT DEFAULT ((0)) NOT NULL,
    [readdatetime] DATETIME NULL,
    [expirationdatetime] DATETIME NULL,
    [actionurl] NVARCHAR(1000) NULL,
    [active] BIT DEFAULT ((1)) NOT NULL,
    [createddatetime] DATETIME NOT NULL,
    [createdby] NVARCHAR(100) DEFAULT ('SYSTEM') NULL,
        CONSTRAINT [PK_tpm_notification] PRIMARY KEY ([id])
    );
END
GO
