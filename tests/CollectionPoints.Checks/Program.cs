using BLL.Services.Implements.WarehouseOperations;
using DAL;
using DAL.Models;
using Microsoft.EntityFrameworkCore;

var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer($"Server=(localdb)\\MSSQLLocalDB;Database=CollectionPointsChecks_{Guid.NewGuid():N};Integrated Security=true;TrustServerCertificate=true").Options;
await using var db = new AppDbContext(options);
void Check(bool ok, string name) { if (!ok) throw new Exception(name); Console.WriteLine("PASS " + name); }
try
{
    await db.Database.EnsureCreatedAsync();
    var warehouse = new Warehouse { Id=Guid.NewGuid(), CurrentWeight=22, TotalCapacityKg=60000 };
    var empty = new Warehouse { Id=Guid.NewGuid(), CurrentWeight=99 };
    var disabled = new Warehouse { Id=Guid.NewGuid(), IsActive=false };
    WarehouseArea Area(decimal kg, string type) => new() { Id=Guid.NewGuid(), Warehouse=warehouse, CurrentKg=kg, AreaType=type };
    var charity=Area(10,"Storage"); var disposal=Area(12,"Storage"); var classification=Area(30,"Unclassified");
    var receiving=Area(0,"Receiving"); var ignored=Area(500,"Storage"); ignored.IsActive=false;
    db.AddRange(warehouse,empty,disabled,charity,disposal,classification,receiving,ignored); await db.SaveChangesAsync(); db.ChangeTracker.Clear();
    var rows=await WarehouseCollectionPoints.ListAsync(db);
    Check(rows.Single(x=>x.Id==warehouse.Id).CurrentWeight==52,"22 stored + 30 classifying = 52 kg");
    Check(rows.Single(x=>x.Id==warehouse.Id).TotalCapacityKg==60000,"configured capacity unchanged");
    Check(rows.Count==2 && rows.Single(x=>x.Id==empty.Id).CurrentWeight==0,"inactive warehouses/areas excluded; empty warehouse resets stale display");
    Check((await db.Warehouses.AsNoTracking().SingleAsync(x=>x.Id==warehouse.Id)).CurrentWeight==22,"read endpoint does not mutate storage counter");
    await db.WarehouseAreas.Where(x=>x.Id==classification.Id).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.CurrentKg,0));
    await db.WarehouseAreas.Where(x=>x.Id==receiving.Id).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.CurrentKg,30));
    Check((await WarehouseCollectionPoints.ListAsync(db)).Single(x=>x.Id==warehouse.Id).CurrentWeight==52,"moving weight between areas counted once");
    await db.WarehouseAreas.Where(x=>x.Id==disposal.Id).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.CurrentKg,0));
    Check((await WarehouseCollectionPoints.ListAsync(db)).Single(x=>x.Id==warehouse.Id).CurrentWeight==40,"exported weight no longer counted");
}
finally { await db.Database.EnsureDeletedAsync(); }
