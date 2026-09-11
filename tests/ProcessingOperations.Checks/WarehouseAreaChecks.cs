using BLL.DTOs;
using BLL.Services.Implements.WarehouseOperations;
using DAL;
using DAL.Models;
using Microsoft.EntityFrameworkCore;

internal static class WarehouseAreaChecks
{
    public static async Task Run(Func<AppDbContext> factory, Guid managerId)
    {
        var warehouse = new Warehouse { Id = Guid.NewGuid(), WarehouseName = "Area configuration checks", TotalCapacityKg = 10000 };
        await using (var db = factory()) { db.Add(warehouse); await db.SaveChangesAsync(); }
        async Task<T> Run<T>(Func<WarehouseOperationsService, Task<T>> work)
        { await using var db = factory(); return await work(new(db)); }
        async Task Denied(Func<WarehouseOperationsService, Task> work)
        {
            try { await Run(async s => { await work(s); return true; }); }
            catch (InvalidOperationException) { return; }
            throw new Exception("Expected invalid area/location configuration to be rejected.");
        }
        foreach (var direction in new[] { "Charity", "Recycling", "Disposal" })
        {
            var dto = new SaveWarehouseAreaDto(warehouse.Id, direction, null, 1000, "Storage", direction);
            var id = await Run(s => s.CreateAreaAsync(managerId, dto));
            var group = await Run(s => s.CreateGroupAsync(managerId, new(id, "A01", null, 1000)));
            var locationDto = new SaveStorageLocationDto(group, direction + "-01", "A01", "R01", "S01", "B01", null, null, 500, "Available");
            var location = await Run(s => s.CreateLocationAsync(managerId, locationDto));
            var layout = await Run(s => s.GetLayoutAsync(managerId, warehouse.Id));
            var area = layout.Areas.Single(x => x.Id == id);
            if (area.ProcessingDirection != direction || area.Locations.Single().PreferredProcessingDirection != direction)
                throw new Exception("Area direction must persist and be inherited by new locations.");
            await Denied(s => s.UpdateLocationAsync(managerId, location, locationDto with { PreferredProcessingDirection = "Wrong" }));
            await using (var db = factory())
            { (await db.WarehouseAreas.SingleAsync(x => x.Id == id)).CurrentKg = 1; await db.SaveChangesAsync(); }
            await Denied(s => s.UpdateAreaAsync(managerId, id, dto with { ProcessingDirection = direction == "Charity" ? "Disposal" : "Charity" }));
        }
        await Denied(async s => { await s.CreateAreaAsync(managerId, new(warehouse.Id, "Invalid", null, 100, "Recycled", "Recycling")); });
        var recycled = await Run(s => s.CreateAreaAsync(managerId, new(warehouse.Id, "Returned clothing", null, 100, "Recycled")));
        if (!(await Run(s => s.GetLayoutAsync(managerId, warehouse.Id))).Areas.Any(x => x.Id == recycled && x.AreaType == "Recycled" && x.ProcessingDirection == null))
            throw new Exception("Recycled staging must remain separate from recycling storage.");
        Console.WriteLine("PASS: three storage purposes persist, location inheritance, incompatible direction and occupied-area guards, separate Recycled staging");
    }
}
