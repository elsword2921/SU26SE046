/*
For databases already initialized by the ReThreads bootstrap.
This resets ONLY the 5 known Manager/Staff bootstrap accounts to ReThread@2026.
No backslash; ReThread is singular. Other accounts and business data are unchanged.
Set @DatabaseName to the database used by your backend; run the entire file.
*/
USE [master];
SET NOCOUNT ON;
SET XACT_ABORT ON;
DECLARE @DatabaseName sysname=N'ReThreadsDb';
IF DB_ID(@DatabaseName) IS NULL THROW 51001, 'Target database does not exist.', 1;
DECLARE @Sql nvarchar(max)=N'USE '+QUOTENAME(@DatabaseName)+N';
BEGIN TRY
    BEGIN TRANSACTION;
    DECLARE @Expected TABLE (UserName nvarchar(100), RoleName nvarchar(100));
    INSERT INTO @Expected VALUES
        (''manager.demo'',''Manager''),
        (''receiving.staff'',''ReceivingStaff''),
        (''receiving.warehouse'',''ReceivingStaff''),
        (''classification.staff'',''ClassificationStaff''),
        (''warehouse.staff'',''WarehouseStaff'');
    IF (SELECT COUNT(*) FROM dbo.Users u WITH (UPDLOCK,HOLDLOCK)
        JOIN dbo.Roles r ON r.Id=u.RoleId
        JOIN @Expected e ON e.UserName=u.UserName AND e.RoleName=r.RoleName)<>5
        THROW 51002, ''Expected exactly 5 bootstrap accounts with matching roles. No passwords were changed.'', 1;
    UPDATE u SET PasswordHash=@NewHash,UpdateAt=SYSUTCDATETIME()
    FROM dbo.Users u JOIN dbo.Roles r ON r.Id=u.RoleId
    JOIN @Expected e ON e.UserName=u.UserName AND e.RoleName=r.RoleName;
    COMMIT;
    PRINT N''Updated 5 bootstrap account passwords.'';
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK;
    THROW;
END CATCH;';
EXEC sys.sp_executesql @Sql,N'@NewHash nvarchar(100)',@NewHash=N'$2b$11$mMtLrhj8wlwjQnhljS2XIOGxCtmq4OfFlJU2ddq7xQATVcWWZOlq6';
