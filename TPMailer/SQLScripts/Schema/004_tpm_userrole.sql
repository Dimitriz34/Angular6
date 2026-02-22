IF OBJECT_ID(N'dbo.tpm_userrole', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tpm_userrole] (
    [id] UNIQUEIDENTIFIER DEFAULT (newid()) NOT NULL,
    [userid] UNIQUEIDENTIFIER NOT NULL,
    [roleid] INT NOT NULL,
    [active] BIT DEFAULT ((1)) NOT NULL,
    [createddatetime] DATETIME NOT NULL,
    [createdby] NVARCHAR(100) DEFAULT ('SYSTEM') NULL,
    [modifieddatetime] DATETIME NULL,
    [modifiedby] NVARCHAR(100) NULL,
        CONSTRAINT [PK_tpm_userrole] PRIMARY KEY ([id])
    );
END
GO
