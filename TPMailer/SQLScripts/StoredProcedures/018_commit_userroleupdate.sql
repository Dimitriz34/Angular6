IF OBJECT_ID(N'dbo.commit_userroleupdate', N'P') IS NOT NULL
    DROP PROCEDURE [dbo].[commit_userroleupdate];
GO

-- Updates a user's role by deactivating old roles and adding the new one
CREATE PROCEDURE dbo.commit_userroleupdate
    @userid     UNIQUEIDENTIFIER,
    @newroleid  INT,
    @modifiedby NVARCHAR(100) = 'SYSTEM'
AS
BEGIN
    SET NOCOUNT ON

    BEGIN TRY
        BEGIN TRANSACTION

        -- Deactivate all current roles for the user
        UPDATE dbo.tpm_userrole 
        SET active = 0, 
            modifieddatetime = GETUTCDATE(), 
            modifiedby = @modifiedby
        WHERE userid = @userid AND active = 1

        -- Check if user already had this role (reactivate it)
        IF EXISTS (
            SELECT 1 FROM dbo.tpm_userrole 
            WHERE userid = @userid AND roleid = @newroleid AND active = 0
        )
        BEGIN
            UPDATE dbo.tpm_userrole
            SET active = 1,
                modifieddatetime = GETUTCDATE(),
                modifiedby = @modifiedby
            WHERE userid = @userid AND roleid = @newroleid
        END
        ELSE
        BEGIN
            -- Insert new role
            INSERT INTO dbo.tpm_userrole (userid, roleid, createddatetime, createdby)
            VALUES (@userid, @newroleid, GETUTCDATE(), @modifiedby)
        END

        COMMIT TRANSACTION
        SELECT 1 AS success
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION
        SELECT 0 AS success
    END CATCH
END

GO
