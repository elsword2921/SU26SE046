SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @SourceWarehouseId uniqueidentifier = 'B17468FF-CBE1-46A0-8375-890B50CD2F99';
DECLARE @WarehouseId uniqueidentifier = 'D1200000-0000-4000-8000-000000000001';
DECLARE @Now datetime2 = SYSUTCDATETIME();
DECLARE @Address nvarchar(500) = N'QTSC Building 1, Khu Công viên phần mềm Quang Trung, Phường Trung Mỹ Tây, TP. Hồ Chí Minh, Việt Nam';
DECLARE @PasswordHash nvarchar(max) = '$2b$12$Kl4LboLyLfKtgLTqn2XmrODUjdlP5QTq4/rfG.ONngEn.KgrPFI72';

BEGIN TRY
    BEGIN TRANSACTION;

    IF NOT EXISTS (SELECT 1 FROM Warehouses WHERE Id = @SourceWarehouseId AND IsActive = 1)
        THROW 51000, N'Không tìm thấy kho Thủ Đức để làm cấu trúc mẫu.', 1;

    IF NOT EXISTS (SELECT 1 FROM Warehouses WHERE Id = @WarehouseId)
    BEGIN
        INSERT INTO Warehouses
            (Id, WarehouseName, Address, PhoneNumber, Email, Description,
             CreateAt, IsActive, CurrentWeight, TotalCapacityKg, Latitude, Longitude, ServiceRadiusKm)
        SELECT @WarehouseId, N'Kho Quận 12 - QTSC', @Address, '02837158888',
               'q12.warehouse@rethreads.local',
               N'Kho tiếp nhận, phân loại, lưu trữ và phân phối tại khu vực Quận 12.',
               @Now, 1, 0, TotalCapacityKg, 10.853099, 106.625941, ServiceRadiusKm
        FROM Warehouses
        WHERE Id = @SourceWarehouseId;

        CREATE TABLE #AreaMap (SourceId uniqueidentifier PRIMARY KEY, TargetId uniqueidentifier NOT NULL);
        INSERT INTO #AreaMap (SourceId, TargetId)
        SELECT Id, NEWID()
        FROM WarehouseAreas
        WHERE WarehouseId = @SourceWarehouseId AND IsActive = 1;

        INSERT INTO WarehouseAreas
            (Id, WarehouseId, AreaName, Description, CapacityKg, CurrentKg,
             CreateAt, IsActive, AreaType)
        SELECT m.TargetId, @WarehouseId, a.AreaName, a.Description, a.CapacityKg, 0,
               @Now, 1, a.AreaType
        FROM WarehouseAreas a
        JOIN #AreaMap m ON m.SourceId = a.Id;

        CREATE TABLE #GroupMap (SourceId uniqueidentifier PRIMARY KEY, TargetId uniqueidentifier NOT NULL);
        INSERT INTO #GroupMap (SourceId, TargetId)
        SELECT g.Id, NEWID()
        FROM AreaGroups g
        JOIN #AreaMap a ON a.SourceId = g.AreaId
        WHERE g.IsActive = 1;

        INSERT INTO AreaGroups
            (Id, AreaId, GroupName, Description, CapacityKg, CurrentKg, CreateAt, IsActive)
        SELECT gm.TargetId, am.TargetId, g.GroupName, g.Description, g.CapacityKg, 0, @Now, 1
        FROM AreaGroups g
        JOIN #GroupMap gm ON gm.SourceId = g.Id
        JOIN #AreaMap am ON am.SourceId = g.AreaId;

        INSERT INTO StorageLocations
            (Id, WarehouseId, AreaId, AreaGroupId, LocationCode, AisleCode, RackCode,
             ShelfCode, BinCode, PreferredGarmentGroup, PreferredProcessingDirection,
             CapacityKg, CurrentWeightKg, Status, CreateAt, IsActive)
        SELECT NEWID(), @WarehouseId, am.TargetId, gm.TargetId,
               CONCAT('Q12-', s.LocationCode), s.AisleCode, s.RackCode, s.ShelfCode,
               s.BinCode, s.PreferredGarmentGroup, s.PreferredProcessingDirection,
               s.CapacityKg, 0, 'Available', @Now, 1
        FROM StorageLocations s
        JOIN #AreaMap am ON am.SourceId = s.AreaId
        JOIN #GroupMap gm ON gm.SourceId = s.AreaGroupId
        WHERE s.WarehouseId = @SourceWarehouseId AND s.IsActive = 1;

        DECLARE @ReceivingRoleId uniqueidentifier = (SELECT TOP (1) Id FROM Roles WHERE RoleName = 'ReceivingStaff');
        DECLARE @ClassificationRoleId uniqueidentifier = (SELECT TOP (1) Id FROM Roles WHERE RoleName = 'ClassificationStaff');
        DECLARE @WarehouseRoleId uniqueidentifier = (SELECT TOP (1) Id FROM Roles WHERE RoleName = 'WarehouseStaff');

        IF @ReceivingRoleId IS NULL OR @ClassificationRoleId IS NULL OR @WarehouseRoleId IS NULL
            THROW 51001, N'Production đang thiếu một hoặc nhiều role staff bắt buộc.', 1;

        INSERT INTO Users
            (Id, FullName, Email, PhoneNumber, RoleId, WarehouseId, PasswordHash,
             UserName, Address, UserStatus, DonationPoint, CreateAt, IsActive, EmailConfirmed)
        SELECT v.Id, v.FullName, v.Email, v.PhoneNumber, v.RoleId, @WarehouseId,
               @PasswordHash, v.UserName, @Address, 'Active', 0, @Now, 1, 1
        FROM (VALUES
            (CAST('D1200000-0000-4000-8100-000000000001' AS uniqueidentifier), N'Nguyễn Hoàng Minh', 'q12.receiving01@rethreads.local', '0901200101', @ReceivingRoleId, 'q12.receiving01'),
            (CAST('D1200000-0000-4000-8100-000000000002' AS uniqueidentifier), N'Trần Gia Hân',      'q12.receiving02@rethreads.local', '0901200102', @ReceivingRoleId, 'q12.receiving02'),
            (CAST('D1200000-0000-4000-8200-000000000001' AS uniqueidentifier), N'Lê Minh Khang',      'q12.classification01@rethreads.local', '0901200201', @ClassificationRoleId, 'q12.classification01'),
            (CAST('D1200000-0000-4000-8200-000000000002' AS uniqueidentifier), N'Phạm Ngọc Anh',      'q12.classification02@rethreads.local', '0901200202', @ClassificationRoleId, 'q12.classification02'),
            (CAST('D1200000-0000-4000-8300-000000000001' AS uniqueidentifier), N'Võ Quốc Bảo',        'q12.warehouse01@rethreads.local', '0901200301', @WarehouseRoleId, 'q12.warehouse01'),
            (CAST('D1200000-0000-4000-8300-000000000002' AS uniqueidentifier), N'Đặng Thảo Vy',       'q12.warehouse02@rethreads.local', '0901200302', @WarehouseRoleId, 'q12.warehouse02')
        ) v(Id, FullName, Email, PhoneNumber, RoleId, UserName);
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

SELECT w.Id, w.WarehouseName, w.Address, w.Latitude, w.Longitude,
       (SELECT COUNT(*) FROM WarehouseAreas a WHERE a.WarehouseId = w.Id AND a.IsActive = 1) AS AreaCount,
       (SELECT COUNT(*) FROM AreaGroups g JOIN WarehouseAreas a ON a.Id = g.AreaId WHERE a.WarehouseId = w.Id AND g.IsActive = 1) AS GroupCount,
       (SELECT COUNT(*) FROM StorageLocations l WHERE l.WarehouseId = w.Id AND l.IsActive = 1) AS LocationCount,
       (SELECT COUNT(*) FROM Users u WHERE u.WarehouseId = w.Id AND u.IsActive = 1) AS StaffCount
FROM Warehouses w
WHERE w.Id = @WarehouseId;
