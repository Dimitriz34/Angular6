IF OBJECT_ID(N'dbo.sel_refreshtokenbyid', N'P') IS NOT NULL
    DROP PROCEDURE [dbo].[sel_refreshtokenbyid];
GO

CREATE   PROCEDURE dbo.sel_refreshtokenbyid @TokenId UNIQUEIDENTIFIER AS BEGIN SET NOCOUNT ON; SELECT tokenid, userid, issuedat, expiresat, createdbyip, revoked, revokedat, revokedbyip, replacedbytoken, reasonrevoked FROM dbo.tpm_refreshtoken WITH (NOLOCK) WHERE tokenid = @TokenId; END;
GO
