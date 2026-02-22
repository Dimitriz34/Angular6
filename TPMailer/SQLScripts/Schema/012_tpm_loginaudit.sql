IF OBJECT_ID(N'dbo.tpm_loginaudit', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tpm_loginaudit] (
    [id] BIGINT IDENTITY(1,1) NOT NULL,
    [userid] UNIQUEIDENTIFIER NULL,
    [email] NVARCHAR(500) NULL,
    [upn] NVARCHAR(500) NULL,
    [logintype] NVARCHAR(50) NULL,
    [success] BIT DEFAULT ((0)) NOT NULL,
    [failurereason] NVARCHAR(500) NULL,
    [ipaddress] NVARCHAR(50) NULL,
    [useragent] NVARCHAR(500) NULL,
    [deviceinfo] NVARCHAR(500) NULL,
    [location] NVARCHAR(255) NULL,
    [sessionid] NVARCHAR(255) NULL,
    [tokenexpiry] DATETIME NULL,
    [active] BIT DEFAULT ((1)) NOT NULL,
    [createddatetime] DATETIME NOT NULL,
        CONSTRAINT [PK_tpm_loginaudit] PRIMARY KEY ([id])
    );
END
GO
