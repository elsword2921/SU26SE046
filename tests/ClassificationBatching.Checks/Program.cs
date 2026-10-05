using BLL.Services.Implements.ClassificationOperations;
using DAL;
using DAL.Models;
using Microsoft.EntityFrameworkCore;

// Disposable LocalDB only; never read application/production connection strings.
var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer($"Server=(localdb)\\MSSQLLocalDB;Database=BatchingChecks_{Guid.NewGuid():N};Integrated Security=true;TrustServerCertificate=true").Options;
var warehouse = new Warehouse { Id = Guid.NewGuid(), WarehouseName = "Test" };
var staff = new User { Id = Guid.NewGuid(), UserName = "test", Warehouse = warehouse, Role = new Role { Id = Guid.NewGuid(), RoleName = "ClassificationStaff" } };
var shift = new Shift { Id = Guid.NewGuid(), Warehouse = warehouse };
var source = new IntakeBatch { Id = Guid.NewGuid(), BatchCode = "INT-1", Warehouse = warehouse, Shift = shift };
var second = new IntakeBatch { Id = Guid.NewGuid(), BatchCode = "INT-2", Warehouse = warehouse, Shift = shift };
var rc = new IntakeBatch { Id = Guid.NewGuid(), BatchCode = "RC-1", Warehouse = warehouse, Shift = shift };
var request = new DonationRequest { Id = Guid.NewGuid(), RequestCode = "DON-1", Donor = staff, Warehouse = warehouse };
var batch = new ClassifiedBatch { Id = Guid.NewGuid(), BatchCode = "CB-1", GroupKey = "MANUAL|test", Status = "Draft", Warehouse = warehouse };
ClassifiedItem Item(IntakeBatch intake, string code) => new() { Id = Guid.NewGuid(), ItemCode = code, Batch = intake, ClassifiedByStaff = staff };
var items = new[] { Item(source,"CI-1"), Item(source,"CI-2"), Item(second,"CI-3"), Item(rc,"CI-4"), Item(source,"CI-5") };
async Task Run(Func<ClassificationOperationsService,Task> action) { await using var db = new AppDbContext(options); await action(new(db)); }
void Check(bool condition, string label) { if (!condition) throw new Exception(label); Console.WriteLine("PASS " + label); }
try
{
    await using (var db = new AppDbContext(options))
    {
        await db.Database.EnsureCreatedAsync();
        db.AddRange(items); db.Add(batch);
        foreach (var intake in new[] { source, second }) db.Add(new IntakeBatchDonationRequest { Id = Guid.NewGuid(), IntakeBatch = intake, DonationRequest = request, AddedByStaff = staff });
        await db.SaveChangesAsync();
    }
    await Run(s => s.AssignItemsAsync(staff.Id, batch.Id, [items[0].Id, items[1].Id, items[2].Id, items[3].Id, items[0].Id]));
    await using (var db = new AppDbContext(options))
    {
        Check(await db.ClassifiedItems.CountAsync(i => i.ClassifiedBatchId == batch.Id) == 4, "multiple items, duplicate selection and multiple intake sources assigned together");
        Check((await db.ClassifiedBatches.SingleAsync()).TotalItem == 4, "batch count matches assigned items");
        Check(await db.ClassifiedBatchDonationRequests.CountAsync() == 2, "one provenance per source/request; RC without donations supported");
    }
    await Run(s => s.AssignItemsAsync(staff.Id, batch.Id, [items[4].Id]));
    await using (var db = new AppDbContext(options)) Check(await db.ClassifiedBatchDonationRequests.CountAsync() == 2, "single-item add reuses persisted provenance");
    foreach (var item in new[] { items[0], items[1], items[4] }) await Run(s => s.RemoveItemAsync(staff.Id, batch.Id, item.Id));
    await using (var db = new AppDbContext(options)) Check(await db.ClassifiedBatchDonationRequests.CountAsync(x => x.IsActive != false) == 1, "last source item removal deactivates provenance");
    await Run(s => s.AssignItemsAsync(staff.Id, batch.Id, [items[0].Id, items[1].Id]));
    await using (var db = new AppDbContext(options))
    {
        Check(await db.ClassifiedBatchDonationRequests.CountAsync(x => x.IsActive != false) == 2, "re-adding source reactivates provenance without duplicates");
        Check((await db.ClassifiedBatches.SingleAsync()).TotalItem == 4, "count remains correct after remove and multi-add");
    }
    foreach (var invalid in new[] { 9.99m, 50.01m, 100m, 10.001m })
    {
        var rejected = false;
        try { await Run(s => s.FinalizeManualBatchAsync(staff.Id, batch.Id, new(invalid))); }
        catch (InvalidOperationException) { rejected = true; }
        Check(rejected, $"finalize rejects {invalid} kg");
    }
    await using (var db = new AppDbContext(options))
        Check((await db.ClassifiedBatches.SingleAsync()).Status == "Draft", "invalid weight leaves batch draft");
    await Run(s => s.FinalizeManualBatchAsync(staff.Id, batch.Id, new(50m)));
    await using (var db = new AppDbContext(options))
        Check((await db.ClassifiedBatches.SingleAsync()).TotalWeight == 50m, "finalize accepts upper boundary 50 kg");

}
finally { await using var db = new AppDbContext(options); await db.Database.EnsureDeletedAsync(); }
