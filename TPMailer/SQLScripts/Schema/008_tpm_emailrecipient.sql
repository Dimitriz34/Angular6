IF OBJECT_ID(N'dbo.tpm_emailrecipient', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tpm_emailrecipient] (
    [id] UNIQUEIDENTIFIER DEFAULT (newid()) NOT NULL,
    [emailid] UNIQUEIDENTIFIER NOT NULL,
    [recipienttype] NVARCHAR(10) NOT NULL,
    [recipientemail] NVARCHAR(500) NOT NULL,
    [recipientname] NVARCHAR(255) NULL,
    [deliverystatus] NVARCHAR(50) NULL,
    [deliverydatetime] DATETIME NULL,
    [active] BIT DEFAULT ((1)) NOT NULL,
    [createddatetime] DATETIME NOT NULL,
    [createdby] NVARCHAR(100) DEFAULT ('SYSTEM') NULL,
        CONSTRAINT [PK_tpm_emailrecipient] PRIMARY KEY ([id])
    );
END
GO
