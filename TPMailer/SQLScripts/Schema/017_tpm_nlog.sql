IF OBJECT_ID(N'dbo.tpm_nlog', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tpm_nlog] (
    [id] BIGINT IDENTITY(1,1) NOT NULL,
    [machinename] NVARCHAR(200) NULL,
    [logged] DATETIME NOT NULL,
    [level] NVARCHAR(50) NOT NULL,
    [message] NVARCHAR(MAX) NULL,
    [logger] NVARCHAR(400) NULL,
    [properties] NVARCHAR(MAX) NULL,
    [callsite] NVARCHAR(400) NULL,
    [exception] NVARCHAR(MAX) NULL,
        CONSTRAINT [PK_tpm_nlog] PRIMARY KEY ([id])
    );
END
GO
