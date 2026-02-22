IF OBJECT_ID(N'dbo.commit_emailrecipient', N'P') IS NOT NULL
    DROP PROCEDURE [dbo].[commit_emailrecipient];
GO

CREATE PROCEDURE dbo.commit_emailrecipient
    @emailid        UNIQUEIDENTIFIER,
    @recipienttype  NVARCHAR(10),
    @recipientemail NVARCHAR(500),
    @recipientname  NVARCHAR(255) = NULL,
    @createdby      NVARCHAR(100) = 'SYSTEM'
AS
BEGIN
    SET NOCOUNT ON

    INSERT INTO dbo.tpm_emailrecipient (
        emailid, recipienttype, recipientemail, recipientname, createddatetime, createdby
    )
    VALUES (
        @emailid, @recipienttype, @recipientemail, @recipientname, GETUTCDATE(), @createdby
    )

    SELECT SCOPE_IDENTITY() AS id
END

GO
