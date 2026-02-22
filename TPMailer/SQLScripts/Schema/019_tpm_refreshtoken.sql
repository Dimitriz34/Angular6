IF OBJECT_ID(N'dbo.tpm_refreshtoken', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tpm_refreshtoken] (
    [tokenid] UNIQUEIDENTIFIER NOT NULL,
    [userid] UNIQUEIDENTIFIER NOT NULL,
    [issuedat] DATETIME2 NOT NULL,
    [expiresat] DATETIME2 NOT NULL,
    [createdbyip] VARCHAR(45) NULL,
    [revoked] BIT DEFAULT ((0)) NOT NULL,
    [revokedat] DATETIME2 NULL,
    [revokedbyip] VARCHAR(45) NULL,
    [replacedbytoken] UNIQUEIDENTIFIER NULL,
    [reasonrevoked] VARCHAR(255) NULL,
        CONSTRAINT [PK_tpm_refreshtoken] PRIMARY KEY ([tokenid])
    );
END
GO
