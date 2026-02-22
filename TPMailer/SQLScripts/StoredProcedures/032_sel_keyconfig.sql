IF OBJECT_ID(N'dbo.sel_keyconfig', N'P') IS NOT NULL
    DROP PROCEDURE [dbo].[sel_keyconfig];
GO

-- ============================================================================
-- sel_keyconfig: Retrieves active encryption key configuration
-- ============================================================================
CREATE PROCEDURE dbo.sel_keyconfig
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
    WHERE active = 1
END

GO
