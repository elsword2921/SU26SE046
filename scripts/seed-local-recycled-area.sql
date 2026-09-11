-- Local test warehouse only. Re-running does not duplicate the area.
SET NOCOUNT ON;
SET XACT_ABORT ON;
IF DB_NAME() <> 'UsedClothingDb' THROW 50000, 'Unexpected database.', 1;
DECLARE @WarehouseId uniqueidentifier = 'B17468FF-CBE1-46A0-8375-890B50CD2F99';
DECLARE @Capacity decimal(18,2) = 1000, @Now datetime2 = SYSUTCDATETIME();
BEGIN TRY
    BEGIN TRANSACTION;
    DECLARE @Total decimal(18,2), @Allocated decimal(18,2), @Source uniqueidentifier;
    SELECT @Total=TotalCapacityKg FROM Warehouses WITH (UPDLOCK,HOLDLOCK)
    WHERE Id=@WarehouseId AND IsActive=1;
    IF @Total IS NULL THROW 50000, 'Test warehouse not found.', 1;
    IF EXISTS (SELECT 1 FROM WarehouseAreas WITH (UPDLOCK,HOLDLOCK)
               WHERE WarehouseId=@WarehouseId AND AreaType='Recycled' AND IsActive=1)
    BEGIN
        COMMIT;
        PRINT 'Recycled area already exists; no changes.';
        RETURN;
    END;
    SELECT @Allocated=COALESCE(SUM(CapacityKg),0) FROM WarehouseAreas WITH (UPDLOCK,HOLDLOCK)
    WHERE WarehouseId=@WarehouseId AND IsActive=1;
    DECLARE @Needed decimal(18,2)=@Capacity-(@Total-@Allocated);
    IF @Needed>0
    BEGIN
        SELECT TOP (1) @Source=a.Id FROM WarehouseAreas a WITH (UPDLOCK,HOLDLOCK)
        WHERE a.WarehouseId=@WarehouseId AND a.AreaType='Receiving' AND a.IsActive=1
          AND a.CapacityKg-@Needed >= a.CurrentKg
          AND a.CapacityKg-@Needed >= (SELECT COALESCE(SUM(g.CapacityKg),0)
              FROM AreaGroups g WITH (UPDLOCK,HOLDLOCK) WHERE g.AreaId=a.Id AND g.IsActive=1)
        ORDER BY a.Id;
        IF @Source IS NULL THROW 50000, 'Insufficient unallocated receiving area capacity.', 1;
        UPDATE WarehouseAreas SET CapacityKg=CapacityKg-@Needed,UpdateAt=@Now WHERE Id=@Source;
    END;
    DECLARE @Area uniqueidentifier=NEWID();
    INSERT WarehouseAreas (Id,WarehouseId,AreaName,AreaType,Description,CapacityKg,CurrentKg,CreateAt,IsActive)
    VALUES (@Area,@WarehouseId,N'Khu đồ đã tái chế','Recycled',N'Nhận đồ tái chế trả về, chờ phân công phân loại lại.',@Capacity,0,@Now,1);
    DECLARE @Row int=1;
    WHILE @Row<=2
    BEGIN
        DECLARE @Group uniqueidentifier=NEWID(), @Aisle nvarchar(10)=CONCAT('A0',@Row);
        INSERT AreaGroups (Id,AreaId,GroupName,Description,CapacityKg,CurrentKg,CreateAt,IsActive)
        VALUES (@Group,@Area,CONCAT(N'Dãy đồ đã tái chế ',@Aisle),N'Chờ phân loại lại',500,0,@Now,1);
        DECLARE @Bin int=1;
        WHILE @Bin<=2
        BEGIN
            INSERT StorageLocations (Id,WarehouseId,AreaId,AreaGroupId,LocationCode,AisleCode,RackCode,ShelfCode,BinCode,CapacityKg,CurrentWeightKg,Status,CreateAt,IsActive)
            VALUES (NEWID(),@WarehouseId,@Area,@Group,CONCAT('RECYCLED-',@Aisle,'-R01-S01-B0',@Bin),@Aisle,'R01','S01',CONCAT('B0',@Bin),250,0,'Available',@Now,1);
            SET @Bin+=1;
        END;
        SET @Row+=1;
    END;
    COMMIT;
    PRINT 'Created Recycled area: 1000 kg, 2 rows, 4 locations.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT>0 ROLLBACK;
    THROW;
END CATCH;
