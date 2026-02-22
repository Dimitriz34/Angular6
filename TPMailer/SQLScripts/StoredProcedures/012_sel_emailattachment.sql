IF OBJECT_ID(N'dbo.sel_emailattachment', N'P') IS NOT NULL
    DROP PROCEDURE [dbo].[sel_emailattachment];
GO

CREATE PROCEDURE dbo.sel_emailattachment
    @emailid    UNIQUEIDENTIFIER = NULL
AS
BEGIN
    SET NOCOUNT ON

    SELECT
        id, emailid, filename, fileextension, contenttype, filesize,
        filepath, bloburi, contentbase64, checksum, createddatetime
    FROM dbo.tpm_emailattachment WITH (NOLOCK)
    WHERE (@emailid IS NULL OR emailid = @emailid) AND active = 1
    ORDER BY filename
END

GO
