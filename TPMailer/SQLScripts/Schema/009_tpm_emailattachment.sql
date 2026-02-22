IF OBJECT_ID(N'dbo.tpm_emailattachment', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tpm_emailattachment] (
    [id] UNIQUEIDENTIFIER DEFAULT (newid()) NOT NULL,
    [emailid] UNIQUEIDENTIFIER NOT NULL,
    [filename] NVARCHAR(500) NOT NULL,
    [fileextension] NVARCHAR(50) NULL,
    [contenttype] NVARCHAR(255) NULL,
    [filesize] BIGINT NULL,
    [filepath] NVARCHAR(1000) NULL,
    [bloburi] NVARCHAR(2000) NULL,
    [contentbase64] NVARCHAR(MAX) NULL,
    [checksum] NVARCHAR(128) NULL,
    [active] BIT DEFAULT ((1)) NOT NULL,
    [createddatetime] DATETIME NOT NULL,
    [createdby] NVARCHAR(100) DEFAULT ('SYSTEM') NULL,
        CONSTRAINT [PK_tpm_emailattachment] PRIMARY KEY ([id])
    );
END
GO
