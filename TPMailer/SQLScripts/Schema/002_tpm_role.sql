IF OBJECT_ID(N'dbo.tpm_role', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tpm_role] (
    [id] INT IDENTITY(1,1) NOT NULL,
    [name] NVARCHAR(100) NOT NULL,
    [description] NVARCHAR(500) NULL,
    [active] BIT DEFAULT ((1)) NOT NULL,
    [createddatetime] DATETIME NOT NULL,
    [createdby] NVARCHAR(100) DEFAULT ('system') NULL,
    [modifieddatetime] DATETIME NULL,
    [modifiedby] NVARCHAR(100) NULL,
        CONSTRAINT [PK_tpm_role] PRIMARY KEY ([id])
    );
END
GO
