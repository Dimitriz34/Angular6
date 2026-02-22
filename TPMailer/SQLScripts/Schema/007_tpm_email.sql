IF OBJECT_ID(N'dbo.tpm_email', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tpm_email] (
    [emailid] UNIQUEIDENTIFIER DEFAULT (newid()) NOT NULL,
    [userid] UNIQUEIDENTIFIER NOT NULL,
    [appcode] INT NOT NULL,
    [senderfrom] NVARCHAR(500) NULL,
    [replyto] NVARCHAR(500) NULL,
    [recipients] NVARCHAR(MAX) NULL,
    [ccrecipients] NVARCHAR(MAX) NULL,
    [bccrecipients] NVARCHAR(MAX) NULL,
    [subject] NVARCHAR(1000) NULL,
    [body] NVARCHAR(MAX) NULL,
    [ishtmlbody] BIT DEFAULT ((0)) NOT NULL,
    [priority] NVARCHAR(50) DEFAULT ('Normal') NULL,
    [status] NVARCHAR(50) DEFAULT ('Pending') NOT NULL,
    [errorcode] NVARCHAR(50) NULL,
    [errormessage] NVARCHAR(MAX) NULL,
    [retrycount] INT DEFAULT ((0)) NOT NULL,
    [maxretry] INT DEFAULT ((3)) NOT NULL,
    [scheduleddatetime] DATETIME NULL,
    [sentdatetime] DATETIME NULL,
    [trackingid] NVARCHAR(255) NULL,
    [graphmessageid] NVARCHAR(500) NULL,
    [active] BIT DEFAULT ((1)) NOT NULL,
    [createddatetime] DATETIME NOT NULL,
    [createdby] NVARCHAR(100) DEFAULT ('SYSTEM') NULL,
    [modifieddatetime] DATETIME NULL,
    [modifiedby] NVARCHAR(100) NULL,
    [keyversion] INT NULL,
        CONSTRAINT [PK_tpm_email] PRIMARY KEY ([emailid])
    );
END
GO
