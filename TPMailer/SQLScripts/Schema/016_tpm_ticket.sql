IF OBJECT_ID(N'dbo.tpm_ticket', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tpm_ticket] (
    [id] BIGINT IDENTITY(1,1) NOT NULL,
    [ticketnumber] NVARCHAR(50) NOT NULL,
    [userid] UNIQUEIDENTIFIER NULL,
    [appcode] INT NULL,
    [category] NVARCHAR(100) NULL,
    [priority] NVARCHAR(20) DEFAULT ('Medium') NULL,
    [subject] NVARCHAR(500) NOT NULL,
    [description] NVARCHAR(MAX) NULL,
    [status] NVARCHAR(50) DEFAULT ('Open') NOT NULL,
    [assignedto] NVARCHAR(255) NULL,
    [assigneddatetime] DATETIME NULL,
    [resolveddatetime] DATETIME NULL,
    [resolvedby] NVARCHAR(100) NULL,
    [resolution] NVARCHAR(MAX) NULL,
    [slahours] INT NULL,
    [active] BIT DEFAULT ((1)) NOT NULL,
    [createddatetime] DATETIME NOT NULL,
    [createdby] NVARCHAR(100) DEFAULT ('SYSTEM') NULL,
    [modifieddatetime] DATETIME NULL,
    [modifiedby] NVARCHAR(100) NULL,
        CONSTRAINT [PK_tpm_ticket] PRIMARY KEY ([id])
    );
END
GO
