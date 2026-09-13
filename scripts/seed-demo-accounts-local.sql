-- Seed demo environment for local testing of feedback 1109.
-- Creates one Q12 warehouse with Receiving area/groups/locations and the 05-demo staff accounts.
DECLARE @Now datetime2 = SYSUTCDATETIME();
DECLARE @Hash nvarchar(max) = '$2a$11$cBjjgdFX6yIzoSj7KpCIReKi8UwMvd8BNSrlSFhHBdBZWd9o2ZJcy'; -- ReThreads@2026
DECLARE @RoleReceiving uniqueidentifier = (SELECT Id FROM Roles WHERE RoleName='ReceivingStaff');
DECLARE @RoleClassification uniqueidentifier = (SELECT Id FROM Roles WHERE RoleName='ClassificationStaff');
DECLARE @RoleWarehouse uniqueidentifier = (SELECT Id FROM Roles WHERE RoleName='WarehouseStaff');
DECLARE @RoleCharity uniqueidentifier = (SELECT Id FROM Roles WHERE RoleName='CharityOrganization');

-- Warehouse Q12
DECLARE @WarehouseId uniqueidentifier = NEWID();
IF NOT EXISTS (SELECT 1 FROM Warehouses)
BEGIN
    INSERT INTO Warehouses (Id, WarehouseName, Address, PhoneNumber, Email, Description,
        TotalCapacityKg, CurrentWeight, Latitude, Longitude, ServiceRadiusKm, CreateAt, IsActive)
    VALUES (@WarehouseId, N'Kho Quận 12', N'Đường Nguyễn Văn Quá, Quận 12, TP Hồ Chí Minh',
        '0900000000', 'q12@rethreads.local', N'Kho tiếp nhận chính', 6000, 0,
        10.8650869, 106.7545289, 24, @Now, 1);
END
ELSE
    SELECT @WarehouseId = Id FROM Warehouses WHERE IsActive = 1 ORDER BY WarehouseName;

-- Receiving area + 2 groups + locations
DECLARE @AreaId uniqueidentifier = NEWID();
IF NOT EXISTS (SELECT 1 FROM WarehouseAreas WHERE WarehouseId = @WarehouseId AND AreaType = 'Receiving')
BEGIN
    INSERT INTO WarehouseAreas (Id, WarehouseId, AreaName, AreaType, Description, CapacityKg, CurrentKg, CreateAt, IsActive)
    VALUES (@AreaId, @WarehouseId, N'Khu nhận đồ', 'Receiving', N'Khu tiếp nhận Intake Batch', 1000, 0, @Now, 1);
END
ELSE
    SELECT @AreaId = Id FROM WarehouseAreas WHERE WarehouseId = @WarehouseId AND AreaType = 'Receiving';

DECLARE @GroupIds TABLE (Id uniqueidentifier);
IF NOT EXISTS (SELECT 1 FROM AreaGroups WHERE AreaId = @AreaId)
BEGIN
    INSERT INTO AreaGroups (Id, AreaId, GroupName, Description, CapacityKg, CurrentKg, CreateAt, IsActive)
    OUTPUT inserted.Id INTO @GroupIds
    VALUES (NEWID(), @AreaId, N'Dãy RECEIVING-01', N'Dãy trung chuyển khu nhận đồ số 01', 500, 0, @Now, 1),
           (NEWID(), @AreaId, N'Dãy RECEIVING-02', N'Dãy trung chuyển khu nhận đồ số 02', 500, 0, @Now, 1);
END
ELSE
    INSERT INTO @GroupIds SELECT Id FROM AreaGroups WHERE AreaId = @AreaId;

IF NOT EXISTS (SELECT 1 FROM StorageLocations WHERE AreaId = @AreaId)
BEGIN
    INSERT INTO StorageLocations (Id, WarehouseId, AreaId, AreaGroupId, LocationCode, AisleCode, RackCode,
        ShelfCode, BinCode, CapacityKg, CurrentWeightKg, Status, PreferredProcessingDirection, CreateAt, IsActive)
    SELECT NEWID(), @WarehouseId, @AreaId, g.Id,
        N'RECEIVING-A01-R01-' + s.ShelfCode + N'-B01', N'A01', N'R01', s.ShelfCode, N'B01',
        160, 0, N'Available', N'ReceivingStaging', @Now, 1
    FROM @GroupIds g CROSS JOIN (VALUES (N'S01'),(N'S02'),(N'S03')) AS s(ShelfCode);
END

-- Demo staff 01-05 per role (warehouse-scoped)
IF NOT EXISTS (SELECT 1 FROM Users WHERE UserName = 'receiving.demo01')
BEGIN
    INSERT INTO Users (Id, FullName, UserName, Email, PhoneNumber, Address, PasswordHash,
        RoleId, WarehouseId, UserStatus, EmailConfirmed, CreateAt, IsActive)
    VALUES
    (NEWID(), N'Nhân viên tiếp nhận 01', 'receiving.demo01', 'receiving.demo01@rethreads.local', '0901000101', N'Quận 12, TP HCM', @Hash, @RoleReceiving, @WarehouseId, 'Active', 1, @Now, 1),
    (NEWID(), N'Nhân viên tiếp nhận 02', 'receiving.demo02', 'receiving.demo02@rethreads.local', '0901000102', N'Quận 12, TP HCM', @Hash, @RoleReceiving, @WarehouseId, 'Active', 1, @Now, 1),
    (NEWID(), N'Nhân viên tiếp nhận 03', 'receiving.demo03', 'receiving.demo03@rethreads.local', '0901000103', N'Quận 12, TP HCM', @Hash, @RoleReceiving, @WarehouseId, 'Active', 1, @Now, 1),
    (NEWID(), N'Nhân viên tiếp nhận 04', 'receiving.demo04', 'receiving.demo04@rethreads.local', '0901000104', N'Quận 12, TP HCM', @Hash, @RoleReceiving, @WarehouseId, 'Active', 1, @Now, 1),
    (NEWID(), N'Nhân viên tiếp nhận 05', 'receiving.demo05', 'receiving.demo05@rethreads.local', '0901000105', N'Quận 12, TP HCM', @Hash, @RoleReceiving, @WarehouseId, 'Active', 1, @Now, 1);
END

IF NOT EXISTS (SELECT 1 FROM Users WHERE UserName = 'classification.demo01')
BEGIN
    INSERT INTO Users (Id, FullName, UserName, Email, PhoneNumber, Address, PasswordHash,
        RoleId, WarehouseId, UserStatus, EmailConfirmed, CreateAt, IsActive)
    VALUES
    (NEWID(), N'Nhân viên phân loại 01', 'classification.demo01', 'classification.demo01@rethreads.local', '0902000101', N'Quận 12, TP HCM', @Hash, @RoleClassification, @WarehouseId, 'Active', 1, @Now, 1),
    (NEWID(), N'Nhân viên phân loại 02', 'classification.demo02', 'classification.demo02@rethreads.local', '0902000102', N'Quận 12, TP HCM', @Hash, @RoleClassification, @WarehouseId, 'Active', 1, @Now, 1),
    (NEWID(), N'Nhân viên phân loại 03', 'classification.demo03', 'classification.demo03@rethreads.local', '0902000103', N'Quận 12, TP HCM', @Hash, @RoleClassification, @WarehouseId, 'Active', 1, @Now, 1),
    (NEWID(), N'Nhân viên phân loại 04', 'classification.demo04', 'classification.demo04@rethreads.local', '0902000104', N'Quận 12, TP HCM', @Hash, @RoleClassification, @WarehouseId, 'Active', 1, @Now, 1),
    (NEWID(), N'Nhân viên phân loại 05', 'classification.demo05', 'classification.demo05@rethreads.local', '0902000105', N'Quận 12, TP HCM', @Hash, @RoleClassification, @WarehouseId, 'Active', 1, @Now, 1);
END

IF NOT EXISTS (SELECT 1 FROM Users WHERE UserName = 'warehouse.demo01')
BEGIN
    INSERT INTO Users (Id, FullName, UserName, Email, PhoneNumber, Address, PasswordHash,
        RoleId, WarehouseId, UserStatus, EmailConfirmed, CreateAt, IsActive)
    VALUES
    (NEWID(), N'Chuyên viên kho 01', 'warehouse.demo01', 'warehouse.demo01@rethreads.local', '0903000101', N'Quận 12, TP HCM', @Hash, @RoleWarehouse, @WarehouseId, 'Active', 1, @Now, 1),
    (NEWID(), N'Chuyên viên kho 02', 'warehouse.demo02', 'warehouse.demo02@rethreads.local', '0903000102', N'Quận 12, TP HCM', @Hash, @RoleWarehouse, @WarehouseId, 'Active', 1, @Now, 1),
    (NEWID(), N'Chuyên viên kho 03', 'warehouse.demo03', 'warehouse.demo03@rethreads.local', '0903000103', N'Quận 12, TP HCM', @Hash, @RoleWarehouse, @WarehouseId, 'Active', 1, @Now, 1),
    (NEWID(), N'Chuyên viên kho 04', 'warehouse.demo04', 'warehouse.demo04@rethreads.local', '0903000104', N'Quận 12, TP HCM', @Hash, @RoleWarehouse, @WarehouseId, 'Active', 1, @Now, 1),
    (NEWID(), N'Chuyên viên kho 05', 'warehouse.demo05', 'warehouse.demo05@rethreads.local', '0903000105', N'Quận 12, TP HCM', @Hash, @RoleWarehouse, @WarehouseId, 'Active', 1, @Now, 1);
END

IF NOT EXISTS (SELECT 1 FROM Users WHERE UserName = 'charity.demo01')
BEGIN
    INSERT INTO Users (Id, FullName, UserName, Email, PhoneNumber, Address, PasswordHash,
        RoleId, UserStatus, EmailConfirmed, CreateAt, IsActive)
    VALUES
    (NEWID(), N'Tổ chức từ thiện 01', 'charity.demo01', 'charity.demo01@rethreads.local', '0904000101', N'Quận 1, TP HCM', @Hash, @RoleCharity, 'Active', 1, @Now, 1),
    (NEWID(), N'Tổ chức từ thiện 02', 'charity.demo02', 'charity.demo02@rethreads.local', '0904000102', N'Quận 1, TP HCM', @Hash, @RoleCharity, 'Active', 1, @Now, 1),
    (NEWID(), N'Tổ chức từ thiện 03', 'charity.demo03', 'charity.demo03@rethreads.local', '0904000103', N'Quận 1, TP HCM', @Hash, @RoleCharity, 'Active', 1, @Now, 1),
    (NEWID(), N'Tổ chức từ thiện 04', 'charity.demo04', 'charity.demo04@rethreads.local', '0904000104', N'Quận 1, TP HCM', @Hash, @RoleCharity, 'Active', 1, @Now, 1),
    (NEWID(), N'Tổ chức từ thiện 05', 'charity.demo05', 'charity.demo05@rethreads.local', '0904000105', N'Quận 1, TP HCM', @Hash, @RoleCharity, 'Active', 1, @Now, 1);
END

SELECT UserName, UserStatus FROM Users WHERE UserName LIKE '%.demo%' ORDER BY UserName;
SELECT COUNT(*) AS Areas FROM WarehouseAreas WHERE WarehouseId = @WarehouseId;
SELECT COUNT(*) AS Groups FROM AreaGroups g JOIN WarehouseAreas a ON a.Id = g.AreaId WHERE a.WarehouseId = @WarehouseId;
SELECT COUNT(*) AS Locations FROM StorageLocations WHERE WarehouseId = @WarehouseId;
