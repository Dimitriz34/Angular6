IF OBJECT_ID(N'dbo.commit_revokerefreshtoken', N'P') IS NOT NULL
    DROP PROCEDURE [dbo].[commit_revokerefreshtoken];
GO

CREATE   PROCEDURE dbo.commit_revokerefreshtoken @TokenId UNIQUEIDENTIFIER, @RevokedAt DATETIME, @RevokedByIp VARCHAR(45), @ReasonRevoked VARCHAR(255) AS BEGIN SET NOCOUNT ON; UPDATE dbo.tpm_refreshtoken SET revoked = 1, revokedat = @RevokedAt, revokedbyip = @RevokedByIp, reasonrevoked = @ReasonRevoked WHERE tokenid = @TokenId; END;
GO
