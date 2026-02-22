IF OBJECT_ID(N'dbo.commit_insertrefreshtoken', N'P') IS NOT NULL
    DROP PROCEDURE [dbo].[commit_insertrefreshtoken];
GO

CREATE   PROCEDURE dbo.commit_insertrefreshtoken @TokenId UNIQUEIDENTIFIER, @UserId UNIQUEIDENTIFIER, @IssuedAt DATETIME, @ExpiresAt DATETIME, @CreatedByIp VARCHAR(45) AS BEGIN SET NOCOUNT ON; INSERT INTO dbo.tpm_refreshtoken (tokenid, userid, issuedat, expiresat, createdbyip, revoked) VALUES (@TokenId, @UserId, @IssuedAt, @ExpiresAt, @CreatedByIp, 0); END;
GO
