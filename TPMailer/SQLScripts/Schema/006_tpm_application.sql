IF OBJECT_ID(N'dbo.tpm_application', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tpm_application] (
    [appcode] INT IDENTITY(1,1) NOT NULL,
    [appname] NVARCHAR(255) NOT NULL,
    [appdesc] NVARCHAR(1000) NULL,
    [appclient] UNIQUEIDENTIFIER NULL,
    [appclientdefault] NVARCHAR(255) NULL,
    [tenantid] NVARCHAR(500) NULL,
    [active] BIT DEFAULT ((1)) NOT NULL,
    [createddatetime] DATETIME NOT NULL,
    [createdby] NVARCHAR(100) DEFAULT ('SYSTEM') NULL,
    [modifieddatetime] DATETIME NULL,
    [modifiedby] NVARCHAR(100) NULL,
    [fromemailaddress] NVARCHAR(255) NULL,
    [fromdisplayname] NVARCHAR(255) NULL,
    [emailserver] NVARCHAR(500) NULL,
    [port] NVARCHAR(100) NULL,
    [userid] NVARCHAR(100) NULL,
    [owneremail] NVARCHAR(500) NULL,
    [appowner] NVARCHAR(500) NULL,
    [emailserviceid] INT NULL,
    [appsecret] NVARCHAR(500) NULL,
    [isinternalapp] BIT DEFAULT ((0)) NOT NULL,
    [isencrypted] BIT DEFAULT ((0)) NOT NULL,
    [coowner] NVARCHAR(500) NULL,
    [coowneremail] NVARCHAR(500) NULL,
    [encryptedfields] NVARCHAR(500) NULL,
    [keyversion] INT NULL,
    [usetpassist] BIT DEFAULT ((0)) NOT NULL,
        CONSTRAINT [PK_tpm_application] PRIMARY KEY ([appcode])
    );
END
GO
