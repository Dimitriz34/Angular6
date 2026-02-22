IF OBJECT_ID(N'dbo.tpm_activitylog', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tpm_activitylog] (
    [id] BIGINT IDENTITY(1,1) NOT NULL,
    [userid] UNIQUEIDENTIFIER NULL,
    [appcode] INT NULL,
    [logtypeid] INT NULL,
    [action] NVARCHAR(255) NOT NULL,
    [description] NVARCHAR(MAX) NULL,
    [entitytype] NVARCHAR(100) NULL,
    [entityid] NVARCHAR(255) NULL,
    [ipaddress] NVARCHAR(50) NULL,
    [useragent] NVARCHAR(500) NULL,
    [requestpath] NVARCHAR(1000) NULL,
    [requestmethod] NVARCHAR(10) NULL,
    [responsecode] INT NULL,
    [durationms] INT NULL,
    [active] BIT DEFAULT ((1)) NOT NULL,
    [createddatetime] DATETIME NOT NULL,
    [createdby] NVARCHAR(100) DEFAULT ('SYSTEM') NULL,
        CONSTRAINT [PK_tpm_activitylog] PRIMARY KEY ([id])
    );
END
GO
