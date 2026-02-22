IF OBJECT_ID(N'dbo.commit_emailattachment', N'P') IS NOT NULL
    DROP PROCEDURE [dbo].[commit_emailattachment];
GO

CREATE PROCEDURE dbo.commit_emailattachment
    @emailid        UNIQUEIDENTIFIER,
    @filename       NVARCHAR(500),
    @fileextension  NVARCHAR(50) = NULL,
    @contenttype    NVARCHAR(255) = NULL,
    @filesize       BIGINT = NULL,
    @filepath       NVARCHAR(1000) = NULL,
    @bloburi        NVARCHAR(2000) = NULL,
    @contentbase64  NVARCHAR(MAX) = NULL,
    @checksum       NVARCHAR(128) = NULL,
    @createdby      NVARCHAR(100) = 'SYSTEM'
AS
BEGIN
    SET NOCOUNT ON

    INSERT INTO dbo.tpm_emailattachment (
        emailid, filename, fileextension, contenttype, filesize,
        filepath, bloburi, contentbase64, checksum, createddatetime, createdby
    )
    VALUES (
        @emailid, @filename, @fileextension, @contenttype, @filesize,
        @filepath, @bloburi, @contentbase64, @checksum, GETUTCDATE(), @createdby
    )

    SELECT SCOPE_IDENTITY() AS id
END

GO
