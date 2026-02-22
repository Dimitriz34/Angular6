IF OBJECT_ID(N'dbo.tpm_user', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tpm_user] (
    [userid] UNIQUEIDENTIFIER DEFAULT (newid()) NOT NULL,
    [email] NVARCHAR(MAX) NOT NULL,
    [emailblindindex] NVARCHAR(128) NULL,
    [emailencversion] INT DEFAULT ((1)) NULL,
    [upn] NVARCHAR(1000) NULL,
    [upnblindindex] NVARCHAR(128) NULL,
    [upnencversion] INT NULL,
    [username] NVARCHAR(1000) NULL,
    [appsecret] NVARCHAR(MAX) NULL,
    [salt] NVARCHAR(MAX) NULL,
    [encryptionkey] NVARCHAR(MAX) NULL,
    [appcode] INT NULL,
    [active] BIT DEFAULT ((0)) NOT NULL,
    [createddatetime] DATETIME NOT NULL,
    [createdby] NVARCHAR(100) DEFAULT ('SYSTEM') NULL,
    [modifieddatetime] DATETIME NULL,
    [modifiedby] NVARCHAR(100) NULL,
        CONSTRAINT [PK_tpm_user] PRIMARY KEY ([userid])
    );
END
GO
