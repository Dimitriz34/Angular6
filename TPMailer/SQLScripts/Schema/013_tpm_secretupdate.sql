IF OBJECT_ID(N'dbo.tpm_secretupdate', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tpm_secretupdate] (
    [id] BIGINT IDENTITY(1,1) NOT NULL,
    [userid] UNIQUEIDENTIFIER NOT NULL,
    [updatetype] NVARCHAR(50) NOT NULL,
    [oldkeyhash] NVARCHAR(256) NULL,
    [newkeyhash] NVARCHAR(256) NULL,
    [reason] NVARCHAR(500) NULL,
    [requestedby] NVARCHAR(255) NULL,
    [approvedby] NVARCHAR(255) NULL,
    [approveddatetime] DATETIME NULL,
    [status] NVARCHAR(50) DEFAULT ('Pending') NOT NULL,
    [ipaddress] NVARCHAR(50) NULL,
    [active] BIT DEFAULT ((1)) NOT NULL,
    [createddatetime] DATETIME NOT NULL,
    [createdby] NVARCHAR(100) DEFAULT ('SYSTEM') NULL,
    [modifieddatetime] DATETIME NULL,
    [modifiedby] NVARCHAR(100) NULL,
        CONSTRAINT [PK_tpm_secretupdate] PRIMARY KEY ([id])
    );
END
GO
