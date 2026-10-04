using BLL.DTOs;
using BLL.Services.Implements.WarehouseOperations;
using DAL;
using DAL.Models;
using Microsoft.EntityFrameworkCore;

var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseSqlServer(
        $"Server=(localdb)\\MSSQLLocalDB;Database=TransferChecks_{Guid.NewGuid():N};Integrated Security=true;TrustServerCertificate=true"
    )
    .Options;
await using var db = new AppDbContext(options);
void Check(bool condition, string label)
{
    if (!condition)
        throw new Exception(label);
    Console.WriteLine("PASS " + label);
}
try
{
    await db.Database.EnsureCreatedAsync();
    var warehouse = new Warehouse { Id = Guid.NewGuid(), CurrentWeight = 10 };
    var area = new WarehouseArea
    {
        Id = Guid.NewGuid(),
        Warehouse = warehouse,
        CapacityKg = 100,
        CurrentKg = 10,
    };
    var targetArea = new WarehouseArea
    {
        Id = Guid.NewGuid(),
        Warehouse = warehouse,
        CapacityKg = 100,
    };
    StorageLocation Location(WarehouseArea a, string code, decimal kg) =>
        new()
        {
            Id = Guid.NewGuid(),
            Warehouse = warehouse,
            Area = a,
            LocationCode = code,
            CapacityKg = 100,
            CurrentWeightKg = kg,
            PreferredProcessingDirection = "Charity",
        };
    var source = Location(area, "SOURCE", 10);
    var target = Location(targetArea, "TARGET", 0);
    var role = new Role { Id = Guid.NewGuid(), RoleName = "WarehouseStaff" };
    var staff = new User
    {
        Id = Guid.NewGuid(),
        Role = role,
        Warehouse = warehouse,
    };
    var batch = new ClassifiedBatch
    {
        Id = Guid.NewGuid(),
        Warehouse = warehouse,
        BatchCode = "CB-TEST",
    };
    var stock = new Inventory
    {
        Id = Guid.NewGuid(),
        Warehouse = warehouse,
        StorageLocation = source,
        ClassifiedBatch = batch,
        ConditionRating = 1,
        TotalWeight = 10,
        Quantity = 3,
        Sku = "TEST",
    };
    db.AddRange(staff, stock, target);
    await db.SaveChangesAsync();
    db.ChangeTracker.Clear();
    var service = new WarehouseOperationsService(db);
    await service.MoveAsync(staff.Id, stock.Id, new(target.Id, "Rearrange", "Test move"));
    db.ChangeTracker.Clear();
    var transfer = await db.TransferRequests.Include(x => x.Items).SingleAsync();
    var movement = await db.InventoryTransactions.Include(x => x.Items).SingleAsync();
    Check(
        transfer.Status == "Completed" && transfer.ReceivedAt.HasValue,
        "completed transfer recorded"
    );
    Check(
        transfer.FromAreaId == area.Id && transfer.ToAreaId == targetArea.Id,
        "source and destination areas"
    );
    Check(
        transfer.Items.Single().ClassifiedBatchId == batch.Id
            && transfer.Items.Single().RequestStaffId == staff.Id,
        "batch and requesting staff recorded"
    );
    Check(
        movement.ReferenceType == "TransferRequest" && movement.ReferenceId == transfer.Id,
        "MOVE linked to transfer"
    );
    Check(
        movement.Items.Single().Weight == 10
            && movement.Items.Single().SourceLocationId == source.Id,
        "weight and exact positions preserved"
    );
    Check(
        (await db.Warehouses.FindAsync(warehouse.Id))!.CurrentWeight == 10,
        "warehouse total unchanged"
    );
    Check(
        (await db.WarehouseAreas.FindAsync(area.Id))!.CurrentKg == 0
            && (await db.WarehouseAreas.FindAsync(targetArea.Id))!.CurrentKg == 10,
        "area counters moved"
    );
    db.ChangeTracker.Clear();
    try
    {
        await service.MoveAsync(staff.Id, stock.Id, new(target.Id, "Retry", null));
        throw new Exception("Duplicate move accepted");
    }
    catch (InvalidOperationException) { }
    db.ChangeTracker.Clear();
    Check(
        await db.TransferRequests.CountAsync() == 1 && await db.TransferItems.CountAsync() == 1,
        "duplicate creates no extra records"
    );
    await db
        .StorageLocations.Where(x => x.Id == source.Id)
        .ExecuteUpdateAsync(x => x.SetProperty(y => y.CapacityKg, 1));
    try
    {
        await service.MoveAsync(staff.Id, stock.Id, new(source.Id, "Too small", null));
        throw new Exception("Overcapacity move accepted");
    }
    catch (InvalidOperationException) { }
    db.ChangeTracker.Clear();
    Check(
        await db.TransferRequests.CountAsync() == 1
            && (await db.Inventories.FindAsync(stock.Id))!.StorageLocationId == target.Id,
        "rejected move leaves stock and transfer history unchanged"
    );
}
finally
{
    await db.Database.EnsureDeletedAsync();
}
