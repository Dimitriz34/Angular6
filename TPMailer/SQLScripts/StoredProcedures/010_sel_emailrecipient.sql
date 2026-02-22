IF OBJECT_ID(N'dbo.sel_emailrecipient', N'P') IS NOT NULL
    DROP PROCEDURE [dbo].[sel_emailrecipient];
GO

CREATE PROCEDURE dbo.sel_emailrecipient
    @emailid    UNIQUEIDENTIFIER = NULL
AS
BEGIN
    SET NOCOUNT ON

    SELECT
        id AS Id,
        emailid AS EmailId,
        recipienttype AS RecipientType,
        recipientemail AS Recipient,
        recipientname AS ToDisplayName,
        deliverystatus AS DeliveryStatus,
        deliverydatetime AS DeliveryDateTime,
        createddatetime AS CreateDateTime
    FROM dbo.tpm_emailrecipient WITH (NOLOCK)
    WHERE (@emailid IS NULL OR emailid = @emailid) AND active = 1
    ORDER BY recipienttype, recipientemail
END

GO
