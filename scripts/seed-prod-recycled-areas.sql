-- ReThreadsDb only. Default is a rollback preview; set @Apply=1 to persist.
-- Matches the local Recycled layout, without copying local stock or IDs.
SET NOCOUNT ON;
SET XACT_ABORT ON;
DECLARE @Apply bit = 0;
IF DB_NAME() <> 'ReThreadsDb' THROW 50000, 'Unexpected database.', 1;
DECLARE @Changes TABLE (WarehouseId uniqueidentifier, Action nvarchar(100), Detail nvarchar(200));
DECLARE @Targets TABLE (WarehouseId uniqueidentifier PRIMARY KEY);
INSERT @Targets VALUES ('b17468ff-cbe1-46a0-8375-890b50cd2f99'), ('d1200000-0000-4000-8000-000000000001');
BEGIN TRY
    BEGIN TRANSACTION;
    DECLARE @LockResult int;
    EXEC @LockResult=sys.sp_getapplock @Resource='SeedRecycledAreas', @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=15000;
    IF @LockResult<0 THROW 50000, 'Could not acquire seed lock.', 1;
    DECLARE @Warehouse uniqueidentifier, @Now datetime2=SYSUTCDATETIME();
    WHILE EXISTS (SELECT 1 FROM @Targets)
    BEGIN
        SELECT TOP(1) @Warehouse=WarehouseId FROM @Targets ORDER BY WarehouseId;
        DECLARE @Total decimal(18,2)=NULL, @Allocated decimal(18,2), @Needed decimal(18,2);
        SELECT @Total=TotalCapacityKg FROM Warehouses WITH (UPDLOCK,HOLDLOCK) WHERE Id=@Warehouse AND IsActive=1;
        IF @Total IS NULL THROW 50000, 'Target warehouse missing or inactive.', 1;
        IF EXISTS (SELECT 1 FROM WarehouseAreas WITH (UPDLOCK,HOLDLOCK) WHERE WarehouseId=@Warehouse AND AreaType='Recycled' AND IsActive=1)
        BEGIN
            INSERT @Changes VALUES (@Warehouse,N'Skipped',N'Active Recycled area already exists.');
            DELETE @Targets WHERE WarehouseId=@Warehouse;
            CONTINUE;
        END;
        IF EXISTS (SELECT 1 FROM StorageLocations WITH (UPDLOCK,HOLDLOCK)
            WHERE WarehouseId=@Warehouse AND LocationCode IN ('RECYCLED-A01-R01-S01-B01','RECYCLED-A01-R01-S01-B02','RECYCLED-A02-R01-S01-B01','RECYCLED-A02-R01-S01-B02'))
            THROW 50000, 'Recycled location code already exists; inspect before seeding.', 1;
        SELECT @Allocated=COALESCE(SUM(CapacityKg),0) FROM WarehouseAreas WITH (UPDLOCK,HOLDLOCK) WHERE WarehouseId=@Warehouse AND IsActive=1;
        SET @Needed=1000-(@Total-@Allocated);
        IF @Needed>0
        BEGIN
            -- First use capacity not yet assigned to receiving rows.
            DECLARE @SourceArea uniqueidentifier=NULL, @SourceGroup uniqueidentifier=NULL, @SourceLocation uniqueidentifier=NULL;
            SELECT TOP(1) @SourceArea=a.Id FROM WarehouseAreas a WITH (UPDLOCK,HOLDLOCK)
            WHERE a.WarehouseId=@Warehouse AND a.AreaType='Receiving' AND a.IsActive=1
                AND a.CapacityKg-@Needed>=a.CurrentKg
                AND a.CapacityKg-@Needed>=(SELECT COALESCE(SUM(g.CapacityKg),0) FROM AreaGroups g WITH (UPDLOCK,HOLDLOCK) WHERE g.AreaId=a.Id AND g.IsActive=1)
            ORDER BY a.Id;
            IF @SourceArea IS NULL
            BEGIN
                -- If rows already use all capacity, shrink one unused receiving bin,
                -- its row and its area equally. Never touch a bin referenced by stock.
                SELECT TOP(1) @SourceArea=a.Id,@SourceGroup=g.Id,@SourceLocation=l.Id
                FROM WarehouseAreas a WITH (UPDLOCK,HOLDLOCK)
                JOIN AreaGroups g WITH (UPDLOCK,HOLDLOCK) ON g.AreaId=a.Id AND g.IsActive=1
                JOIN StorageLocations l WITH (UPDLOCK,HOLDLOCK) ON l.AreaId=a.Id AND l.AreaGroupId=g.Id AND l.WarehouseId=@Warehouse AND l.IsActive=1
                WHERE a.WarehouseId=@Warehouse AND a.AreaType='Receiving' AND a.IsActive=1
                    AND l.CurrentWeightKg=0 AND l.Status='Available' AND l.CapacityKg>@Needed
                    AND g.CapacityKg-@Needed>=g.CurrentKg AND a.CapacityKg-@Needed>=a.CurrentKg
                    AND a.CapacityKg>=(SELECT COALESCE(SUM(x.CapacityKg),0) FROM AreaGroups x WITH (UPDLOCK,HOLDLOCK) WHERE x.AreaId=a.Id AND x.IsActive=1)
                    AND g.CapacityKg>=(SELECT COALESCE(SUM(x.CapacityKg),0) FROM StorageLocations x WITH (UPDLOCK,HOLDLOCK) WHERE x.AreaGroupId=g.Id AND x.IsActive=1)
                    AND NOT EXISTS (SELECT 1 FROM IntakeBatches b WITH (UPDLOCK,HOLDLOCK) WHERE b.CurrentStorageLocationId=l.Id)
                    AND NOT EXISTS (SELECT 1 FROM ClassifiedBatches b WITH (UPDLOCK,HOLDLOCK) WHERE b.StorageLocationId=l.Id)
                    AND NOT EXISTS (SELECT 1 FROM Inventories i WITH (UPDLOCK,HOLDLOCK) WHERE i.StorageLocationId=l.Id)
                    AND NOT EXISTS (SELECT 1 FROM TransactionItems t WITH (UPDLOCK,HOLDLOCK) WHERE t.SourceLocationId=l.Id OR t.DestinationLocationId=l.Id)
                ORDER BY l.LocationCode;
                IF @SourceLocation IS NULL THROW 50000, 'No safely reusable receiving capacity; no changes committed.', 1;
                UPDATE StorageLocations SET CapacityKg=CapacityKg-@Needed,UpdateAt=@Now WHERE Id=@SourceLocation;
                UPDATE AreaGroups SET CapacityKg=CapacityKg-@Needed,UpdateAt=@Now WHERE Id=@SourceGroup;
            END;
            UPDATE WarehouseAreas SET CapacityKg=CapacityKg-@Needed,UpdateAt=@Now WHERE Id=@SourceArea;
            INSERT @Changes VALUES (@Warehouse,N'Reallocated receiving capacity',CONCAT(@Needed,N' kg; source location ',COALESCE(CONVERT(nvarchar(36),@SourceLocation),N'unassigned area capacity')));
        END;
        DECLARE @Area uniqueidentifier=NEWID();
        INSERT WarehouseAreas (Id,WarehouseId,AreaName,AreaType,ProcessingDirection,Description,CapacityKg,CurrentKg,CreateAt,IsActive)
        VALUES (@Area,@Warehouse,N'Khu đồ đã tái chế','Recycled',NULL,N'Nhận đồ tái chế trả về, chờ phân công phân loại lại.',1000,0,@Now,1);
        DECLARE @Row int=1;
        WHILE @Row<=2
        BEGIN
            DECLARE @Group uniqueidentifier=NEWID(),@Aisle nvarchar(10)=CONCAT('A0',@Row);
            INSERT AreaGroups (Id,AreaId,GroupName,Description,CapacityKg,CurrentKg,CreateAt,IsActive)
            VALUES (@Group,@Area,CONCAT(N'Dãy đồ đã tái chế ',@Aisle),N'Chờ phân loại lại',500,0,@Now,1);
            DECLARE @Bin int=1;
            WHILE @Bin<=2
            BEGIN
                INSERT StorageLocations (Id,WarehouseId,AreaId,AreaGroupId,LocationCode,AisleCode,RackCode,ShelfCode,BinCode,CapacityKg,CurrentWeightKg,Status,CreateAt,IsActive)
                VALUES (NEWID(),@Warehouse,@Area,@Group,CONCAT('RECYCLED-',@Aisle,'-R01-S01-B0',@Bin),@Aisle,'R01','S01',CONCAT('B0',@Bin),250,0,'Available',@Now,1);
                SET @Bin+=1;
            END;
            SET @Row+=1;
        END;
        IF (SELECT SUM(CapacityKg) FROM WarehouseAreas WHERE WarehouseId=@Warehouse AND IsActive=1)>@Total
            THROW 50000, 'Area capacities exceed warehouse capacity.', 1;
        INSERT @Changes VALUES (@Warehouse,N'Created Recycled area',N'1000 kg; 2 rows x 500 kg; 4 bins x 250 kg; empty stock.');
        DELETE @Targets WHERE WarehouseId=@Warehouse;
    END;
    SELECT w.WarehouseName,c.Action,c.Detail FROM @Changes c JOIN Warehouses w ON w.Id=c.WarehouseId;
    IF @Apply=1 COMMIT; ELSE ROLLBACK;
    SELECT CASE WHEN @Apply=1 THEN 'COMMITTED' ELSE 'PREVIEW ROLLED BACK' END Result;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT>0 ROLLBACK;
    THROW;
END CATCH;
