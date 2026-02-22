-- Seed Data
-- Reference / lookup tables: tpm_logtype, tpm_role, tpm_keyconfig

-- tpm_logtype
IF NOT EXISTS (SELECT 1 FROM dbo.tpm_logtype)
BEGIN
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
END
GO

-- tpm_role
IF NOT EXISTS (SELECT 1 FROM dbo.tpm_role)
BEGIN
    INSERT INTO dbo.tpm_role (id, name, description, active, createddatetime, createdby)
        VALUES (1, N'moderator', 'Content moderation permissions', 0, '2026-02-20 18:38:42', 'system');
    INSERT INTO dbo.tpm_role (id, name, description, active, createddatetime, createdby)
        VALUES (2, N'user', 'Standard user permissions', 1, '2026-02-20 18:38:42', 'system');
    INSERT INTO dbo.tpm_role (id, name, description, active, createddatetime, createdby)
        VALUES (3, N'admin', 'Full administrative access', 1, '2026-02-20 18:38:42', 'system');
    INSERT INTO dbo.tpm_role (id, name, description, active, createddatetime, createdby)
        VALUES (4, N'tester', 'QA and testing access', 0, '2026-02-20 18:38:42', 'system');
END
GO

-- tpm_keyconfig placeholder (encryption keys must be set post-deploy)
-- INSERT INTO dbo.tpm_keyconfig is intentionally omitted;
-- Run key initialisation through the application after deployment.
GO
