IF OBJECT_ID(N'dbo.sel_keyconfigall', N'P') IS NOT NULL
    DROP PROCEDURE [dbo].[sel_keyconfigall];
GO

-- ============================================================================
-- sel_keyconfigall: Retrieves ALL encryption key configurations
--
-- Purpose: Supports multi-key decryption. When data was encrypted with
--          a previous key version, the service layer can try all available
--          keys to decrypt it. Active keys are returned first.
--
-- Used by: EncryptionHelper.TryDecryptWithAllKeys()
-- ============================================================================
CREATE PROCEDURE dbo.sel_keyconfigall
AS
BEGIN
    SET NOCOUNT ON

    SELECT
        id,
        keyversion,
        encryptionkey,
        saltbytes,
        active
    FROM dbo.tpm_keyconfig WITH (NOLOCK)
    ORDER BY active DESC, id DESC
END

GO
