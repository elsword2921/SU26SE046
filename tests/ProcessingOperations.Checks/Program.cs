using BLL.DTOs;
using BLL.Services.Implements.ProcessingOperations;
using DAL;
using DAL.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using System.Security.Authentication;
using Microsoft.Extensions.Configuration;

// Always creates a fresh, isolated LocalDB database; never reads app connection strings.
var database = "ReThreadsProcessingChecks_" + Guid.NewGuid().ToString("N");
WeightedScoringChecks.Run();
var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseSqlServer($"Server=(localdb)\\MSSQLLocalDB;Database={database};Integrated Security=true;TrustServerCertificate=true").Options;
AppDbContext Db() => new(options);
var carrier = new FakeGhnHandler();
using var ghnClient = new HttpClient(carrier) { BaseAddress = new Uri("https://ghn.invalid/") };
var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
    { ["Ghn:Token"] = "test-token", ["Ghn:ShopId"] = "123" }).Build();
var shipment = new CreateProcessingGhnShipmentDto(1, "KHONGCHOXEMHANG", 1444, "20308",
    "Check warehouse", "0901234567", "Check pickup address", 1444, "20308",
    "Ho Chi Minh", "District 10", "Ward 14", "Ho Chi Minh", "District 10", "Ward 14");
async Task<T> Run<T>(Func<ProcessingOperationsService, Task<T>> action)
{
    await using var db = Db();
    return await action(new(db, ghnClient, configuration));
}
async Task Do(Func<ProcessingOperationsService, Task> action) => await Run(async s => { await action(s); return true; });
void Check(bool value, string label)
{
    if (!value) throw new Exception("FAIL: " + label);
    Console.WriteLine("PASS: " + label);
}
async Task Denied(Func<ProcessingOperationsService, Task> action, string label)
{
    try { await Do(action); }
    catch (Exception e) when (e is InvalidOperationException or AuthenticationException) { Check(true, label); return; }
    throw new Exception("FAIL: expected rejection: " + label);
}
var warehouse = new Warehouse { Id = Guid.NewGuid(), WarehouseName = "Check warehouse", CurrentWeight = 200 };
var area = new WarehouseArea { Id = Guid.NewGuid(), Warehouse = warehouse, CurrentKg = 200 };
var location = new StorageLocation { Id = Guid.NewGuid(), Warehouse = warehouse, Area = area, CurrentWeightKg = 200, LocationCode = "CHECK" };
User User(string role, Guid? warehouseId = null) => new()
{
    Id = Guid.NewGuid(), UserName = Guid.NewGuid().ToString("N"), FullName = role,
    Role = new Role { Id = Guid.NewGuid(), RoleName = role }, WarehouseId = warehouseId,
    PhoneNumber = "0901234567", Address = "Check recipient address"
};
var manager = User("Manager");
var recycler = User("RecyclingOrganization");
var disposer = User("DisposalOrganization");
var stranger = User("RecyclingOrganization");
var staff = User("WarehouseStaff", warehouse.Id);
var wrongStaff = User("WarehouseStaff");
Inventory Stock(string direction, int grade, int quantity = 10) => new()
{
    Id = Guid.NewGuid(), Sku = Guid.NewGuid().ToString("N"), Warehouse = warehouse, StorageLocation = location,
    ProcessingDirection = direction, ConditionRating = grade, Quantity = quantity, TotalWeight = 20
};
var recycle = Stock("Recycling", 2);
var disposal = Stock("Disposal", 3, 0);
var invalid = Stock("Recycling", 1);
var race = Stock("Recycling", 2);
CreateProcessingOperationDto Request(Inventory inv, User org) => new(inv.ProcessingDirection, warehouse.Id, org.Id, "Test", [new(inv.Id)]);
async Task<Guid> Create(Inventory inv, User org) => (await Run(s => s.CreateAsync(manager.Id, Request(inv, org)))).Id;
try
{
    await using (var db = Db())
    {
        await db.Database.EnsureCreatedAsync();
        db.AddRange(manager, recycler, disposer, stranger, staff, wrongStaff, recycle, disposal, invalid, race);
        await db.SaveChangesAsync();
    }
    var catalog = await Run(s => s.CatalogAsync(manager.Id, warehouse.Id, null));
    Check(catalog.Items.Count == 3 && catalog.Items.All(x => x.Grade is "B" or "C"), "catalog only B/C with correct direction");
    await Denied(async s => { await s.CreateAsync(manager.Id, Request(invalid, recycler)); }, "reject grade A");
    await Denied(async s => { await s.CreateAsync(manager.Id, Request(recycle, disposer)); }, "reject wrong organization type");
    await Denied(async s => { await s.CreateAsync(manager.Id, Request(recycle, recycler) with { Inputs = [new(recycle.Id, 10, 1)] }); }, "reject partial batch / inconsistent weight");
    var id = await Create(recycle, recycler);
    await Denied(s => s.ApproveByManagerAsync(manager.Id, id), "manager cannot bypass organization");
    await Denied(s => s.ApproveByOrganizationAsync(stranger.Id, id), "organization ownership");
    await Denied(async s => { await s.GetByIdAsync(wrongStaff.Id, id); }, "warehouse detail scope");
    Check((await Run(s => s.GetByIdAsync(staff.Id, id))).Id == id, "warehouse can read own detail");
    await Do(s => s.RejectByOrganizationAsync(recycler.Id, id, new("Not available")));
    id = await Create(recycle, recycler);
    Check(true, "organization rejection unlocks batch");
    await Do(s => s.ApproveByOrganizationAsync(recycler.Id, id));
    await Do(s => s.RejectByManagerAsync(manager.Id, id, new("Reschedule")));
    id = await Create(recycle, recycler);
    Check(true, "manager rejection unlocks batch");
    await Do(s => s.ApproveByOrganizationAsync(recycler.Id, id));
    await Do(s => s.ApproveByManagerAsync(manager.Id, id));
    await Do(s => s.CancelAsync(manager.Id, id, new("Cancel delivery")));
    await using (var db = Db())
        Check((await db.Inventories.SingleAsync(x => x.Id == recycle.Id)).ReservedWeight == 0, "cancel releases reservation");
    id = await Create(recycle, recycler);
    await Do(s => s.ApproveByOrganizationAsync(recycler.Id, id));
    await Do(s => s.ApproveByManagerAsync(manager.Id, id));
    await Denied(s => s.IssueAsync(wrongStaff.Id, id, new(null)), "wrong warehouse cannot issue");
    await Denied(s => s.CreateGhnShipmentAsync(staff.Id, id, shipment), "cannot book GHN before issue");
    Check((await Run(s => s.GetListAsync(staff.Id, null))).Any(x => x.Id == id), "warehouse work queue");

    async Task<bool> TryConcurrent(Func<ProcessingOperationsService, Task> action)
    {
        try { await Do(action); return true; }
        catch (Exception e) when (e is InvalidOperationException or DbUpdateException || e is SqlException { Number: 1205 }) { return false; }
    }
    var issued = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ => TryConcurrent(s => s.IssueAsync(staff.Id, id, new("Check")))));
    Check(issued.Count(x => x) == 1, "concurrent issue succeeds exactly once");
    await using (var db = Db())
    {
        var inv = await db.Inventories.SingleAsync(x => x.Id == recycle.Id);
        Check(inv.Quantity == 0 && inv.TotalWeight == 0 && inv.ReservedWeight == 0 && inv.Status == "Depleted", "issue consumes full stock and reservation");
        Check(await db.InventoryTransactions.CountAsync(x => x.ReferenceId == id) == 1, "exactly one OUT transaction");
        Check((await db.StorageLocations.SingleAsync()).CurrentWeightKg == 180
            && (await db.WarehouseAreas.SingleAsync()).CurrentKg == 180
            && (await db.Warehouses.SingleAsync()).CurrentWeight == 180, "location, area, warehouse weight updated once");
    }
    await Denied(s => s.CompleteAsync(recycler.Id, id, new("Done")), "cannot complete before receipt");
    Check((await Run(s => s.GetByIdAsync(staff.Id, id))).Status == "ReadyForGhn", "issued operation waits for GHN booking");
    await Denied(s => s.ReceiveAsync(stranger.Id, id), "manual receipt checks organization ownership");
    await Denied(s => s.CreateGhnShipmentAsync(wrongStaff.Id, id, shipment), "GHN booking checks warehouse ownership");
    await Denied(s => s.CreateGhnShipmentAsync(staff.Id, id, shipment with { FromWardCode = "" }), "GHN address validation");
    carrier.RejectNext = true;
    await Denied(s => s.CreateGhnShipmentAsync(staff.Id, id, shipment), "HTTP 200 with GHN error is rejected");
    carrier.TimeoutAfterCreate = true;
    await Denied(s => s.CreateGhnShipmentAsync(staff.Id, id, shipment), "ambiguous GHN timeout does not mark operation booked");
    Check((await Run(s => s.GetByIdAsync(staff.Id, id))).Shipping!.GhnOrderCode == null, "timeout leaves booking retryable");
    await Do(s => s.CreateGhnShipmentAsync(staff.Id, id, shipment));
    var booked = await Run(s => s.GetByIdAsync(staff.Id, id));
    Check(booked.Status == "GhnBooked" && carrier.Orders.Count == 1, "retry recovers same GHN order after timeout");
    var attempts = carrier.CreateAttempts;
    await Do(s => s.CreateGhnShipmentAsync(staff.Id, id, shipment));
    Check(carrier.CreateAttempts == attempts, "repeat booking uses persisted order without external call");
    Check(carrier.LastPayload.GetProperty("weight").GetInt32() == 20000
        && carrier.LastPayload.GetProperty("service_type_id").GetInt32() == 5
        && carrier.LastPayload.GetProperty("to_phone").GetString() == recycler.PhoneNumber,
        "GHN payload uses issued grams and organization recipient");
    await Denied(s => s.RefreshGhnAsync(stranger.Id, id), "GHN tracking checks organization ownership");
    await Do(s => s.ReceiveAsync(recycler.Id, id));
    var received = await Run(s => s.GetByIdAsync(recycler.Id, id));
    Check(received.Status == "OrganizationReceived" && received.OrganizationReceivedAt != null, "organization can receive while GHN is awaiting pickup");
    await Denied(s => s.ReceiveAsync(recycler.Id, id), "cannot receive twice");
    carrier.Status = "delivered";
    await Do(s => s.RefreshGhnAsync(recycler.Id, id));
    await Do(s => s.RefreshGhnAsync(manager.Id, id));
    Check((await Run(s => s.GetByIdAsync(manager.Id, id))).Shipping!.History.Count == 2, "tracking history does not duplicate unchanged status");
    Check((await Run(s => s.GetByIdAsync(recycler.Id, id))).Status == "OrganizationReceived", "GHN refresh preserves manual receipt");
    await Denied(s => s.CompleteAsync(recycler.Id, id, new(" ")), "completion requires result");
    await Denied(s => s.CompleteAsync(recycler.Id, id, new("Recycled externally")), "recycling cannot skip return workflow");
    await RecyclingReturnChecks.Run(Db, Do, id, warehouse.Id, recycler.Id, stranger.Id, staff.Id, wrongStaff.Id, manager.Id);
    carrier.Status = "ready_to_pick";
    await Do(s => s.RefreshGhnAsync(manager.Id, id));
    Check((await Run(s => s.GetByIdAsync(manager.Id, id))).Status == "Completed", "outbound GHN refresh cannot revert completed reclassification");
    var disposalId = await Create(disposal, disposer);
    await Do(s => s.ApproveByOrganizationAsync(disposer.Id, disposalId));
    await Do(s => s.ApproveByManagerAsync(manager.Id, disposalId));
    await Denied(s => s.ReceiveAsync(disposer.Id, disposalId), "cannot receive before warehouse issue");
    await Do(s => s.IssueAsync(staff.Id, disposalId, new(null)));
    await Do(s => s.ReceiveAsync(disposer.Id, disposalId));
    await Do(s => s.CompleteAsync(disposer.Id, disposalId, new("Disposed externally")));
    Check(true, "grade C / weight-only batch lifecycle");
    var created = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ => TryConcurrent(async s => { await s.CreateAsync(manager.Id, Request(race, recycler)); })));
    Check(created.Count(x => x) == 1, "concurrent create locks batch exactly once");
    await using (var first = Db())
    await using (var second = Db())
    {
        var firstLocation = await first.StorageLocations.SingleAsync(x => x.Id == location.Id);
        var secondLocation = await second.StorageLocations.SingleAsync(x => x.Id == location.Id);
        firstLocation.CurrentWeightKg -= 1;
        await first.SaveChangesAsync();
        secondLocation.CurrentWeightKg -= 2;
        var conflicted = false;
        try { await second.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { conflicted = true; }
        Check(conflicted, "stale capacity update cannot overwrite another workflow");
    }
    await WarehouseAreaChecks.Run(Db, manager.Id);
    await CharityReceiptChecks.Run(Db, warehouse.Id, manager.Id);
    Console.WriteLine("All processing checks passed.");
}
finally
{
    await using var db = Db();
    // The name is generated above and this context never points at an application database.
    await db.Database.EnsureDeletedAsync();
}
