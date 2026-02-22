IF OBJECT_ID(N'dbo.tpm_errorlog', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tpm_errorlog] (
    [id] BIGINT IDENTITY(1,1) NOT NULL,
    [userid] UNIQUEIDENTIFIER NULL,
    [appcode] INT NULL,
    [errorcode] NVARCHAR(50) NULL,
    [errormessage] NVARCHAR(MAX) NULL,
    [stacktrace] NVARCHAR(MAX) NULL,
    [innerexception] NVARCHAR(MAX) NULL,
    [source] NVARCHAR(500) NULL,
    [targetsite] NVARCHAR(500) NULL,
    [severity] NVARCHAR(20) DEFAULT ('Error') NULL,
    [requestpath] NVARCHAR(1000) NULL,
    [requestmethod] NVARCHAR(10) NULL,
    [requestbody] NVARCHAR(MAX) NULL,
    [ipaddress] NVARCHAR(50) NULL,
    [useragent] NVARCHAR(500) NULL,
    [resolved] BIT DEFAULT ((0)) NOT NULL,
    [resolveddatetime] DATETIME NULL,
    [resolvedby] NVARCHAR(100) NULL,
    [resolution] NVARCHAR(MAX) NULL,
    [active] BIT DEFAULT ((1)) NOT NULL,
    [createddatetime] DATETIME NOT NULL,
        CONSTRAINT [PK_tpm_errorlog] PRIMARY KEY ([id])
    );
END
GO
