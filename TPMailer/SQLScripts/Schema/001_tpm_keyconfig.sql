IF OBJECT_ID(N'dbo.tpm_keyconfig', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tpm_keyconfig] (
    [id] INT IDENTITY(1,1) NOT NULL,
    [keyversion] INT DEFAULT ((1)) NOT NULL,
    [keypurpose] NVARCHAR(50) NOT NULL,
    [encryptionkey] NVARCHAR(MAX) NULL,
    [decryptionenabled] BIT DEFAULT ((1)) NOT NULL,
    [expirationdatetime] DATETIME NULL,
    [vaborultkeyid] NVARCHAR(500) NULL,
    [active] BIT DEFAULT ((1)) NOT NULL,
    [createddatetime] DATETIME NOT NULL,
    [createdby] NVARCHAR(100) DEFAULT ('system') NULL,
    [modifieddatetime] DATETIME NULL,
    [modifiedby] NVARCHAR(100) NULL,
    [saltbytes] VARBINARY(MAX) NULL,
        CONSTRAINT [PK_tpm_keyconfig] PRIMARY KEY ([id])
    );
END
GO
