IF OBJECT_ID(N'dbo.tpm_logtype', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tpm_logtype] (
    [id] INT IDENTITY(1,1) NOT NULL,
    [typename] NVARCHAR(100) NOT NULL,
    [description] NVARCHAR(500) NULL,
    [active] BIT DEFAULT ((1)) NOT NULL,
    [createddatetime] DATETIME NOT NULL,
        CONSTRAINT [PK_tpm_logtype] PRIMARY KEY ([id])
    );
END
GO
