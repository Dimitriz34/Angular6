-- Seed Data
-- Reference / lookup tables: tpm_logtype, tpm_role, tpm_keyconfig

-- ============================================================================
-- tpm_logtype: Master lookup table for operation/activity types
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM dbo.tpm_logtype)
BEGIN
    SET IDENTITY_INSERT dbo.tpm_logtype ON;

    INSERT INTO dbo.tpm_logtype (id, typename, description, active, createddatetime)
        VALUES (1, N'unknown', 'Unknown operation type', 1, '2026-02-20 18:38:42');
    INSERT INTO dbo.tpm_logtype (id, typename, description, active, createddatetime)
        VALUES (2, N'insert', 'Data creation', 1, '2026-02-20 18:38:42');
    INSERT INTO dbo.tpm_logtype (id, typename, description, active, createddatetime)
        VALUES (3, N'update', 'Data modification', 1, '2026-02-20 18:38:42');
    INSERT INTO dbo.tpm_logtype (id, typename, description, active, createddatetime)
        VALUES (4, N'delete', 'Data removal', 1, '2026-02-20 18:38:42');
    INSERT INTO dbo.tpm_logtype (id, typename, description, active, createddatetime)
        VALUES (5, N'retrieve', 'Data retrieval', 1, '2026-02-20 18:38:42');
    INSERT INTO dbo.tpm_logtype (id, typename, description, active, createddatetime)
        VALUES (6, N'signin', 'User authentication', 1, '2026-02-20 18:38:42');
    INSERT INTO dbo.tpm_logtype (id, typename, description, active, createddatetime)
        VALUES (7, N'signout', 'User session end', 1, '2026-02-20 18:38:42');
    INSERT INTO dbo.tpm_logtype (id, typename, description, active, createddatetime)
        VALUES (8, N'registration', 'New user registration', 1, '2026-02-20 18:38:42');

    SET IDENTITY_INSERT dbo.tpm_logtype OFF;
END
GO

-- ============================================================================
-- tpm_role: Master lookup table for RBAC roles
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM dbo.tpm_role)
BEGIN
    SET IDENTITY_INSERT dbo.tpm_role ON;

    INSERT INTO dbo.tpm_role (id, name, description, active, createddatetime, createdby)
        VALUES (1, N'moderator', 'Content moderation permissions', 0, '2026-02-20 18:38:42', 'system');
    INSERT INTO dbo.tpm_role (id, name, description, active, createddatetime, createdby)
        VALUES (2, N'user', 'Standard user permissions', 1, '2026-02-20 18:38:42', 'system');
    INSERT INTO dbo.tpm_role (id, name, description, active, createddatetime, createdby)
        VALUES (3, N'admin', 'Full administrative access', 1, '2026-02-20 18:38:42', 'system');
    INSERT INTO dbo.tpm_role (id, name, description, active, createddatetime, createdby)
        VALUES (4, N'tester', 'QA and testing access', 0, '2026-02-20 18:38:42', 'system');

    SET IDENTITY_INSERT dbo.tpm_role OFF;
END
GO

-- ============================================================================
-- tpm_keyconfig: Encryption key configuration (AES-256-GCM)
-- The encryptionkey and saltbytes are required for field-level encryption
-- used across tpm_application and tpm_user sensitive columns.
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM dbo.tpm_keyconfig)
BEGIN
    SET IDENTITY_INSERT dbo.tpm_keyconfig ON;

    INSERT INTO dbo.tpm_keyconfig 
        (id, keyversion, keypurpose, encryptionkey, saltbytes, decryptionenabled, 
         active, createddatetime, createdby, expirationdatetime, vaborultkeyid)
    VALUES 
        (1, 1, N'primary', 
         N'45770F0C-C719-4F37-914B-3D3390F27EA1-970113A1-469B-4FC5-9509-30F0C7A10FBD', 
         0x8B23ACCF39A15D31406F1126E69BD623, 
         1, 1, '2026-02-20 18:38:42', 'system', NULL, NULL);

    SET IDENTITY_INSERT dbo.tpm_keyconfig OFF;
END
GO
