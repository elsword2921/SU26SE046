using BLL.Services.Implements.WarehouseOperations;
using DAL;
using DAL.Models;
using Microsoft.EntityFrameworkCore;

internal static class WarehouseReceiptWeightChecks
{
    public static async Task RunAsync(AppDbContext db, WarehouseOperationsService service, Guid warehouseId, Guid staffId)
    {
        foreach (var weight in new[] { 9.99m, 50.01m, 100m, 10.001m, 10m, 50m })
        {
            var batch = new ClassifiedBatch
            {
                Id = Guid.NewGuid(), WarehouseId = warehouseId,
                BatchCode = $"WEIGHT-{weight}", GroupKey = $"WEIGHT-{weight}", TotalWeight = weight,
                TotalItem = 1, Status = "PendingWarehouseReceipt",
            };
            db.ClassifiedBatches.Add(batch);
            await db.SaveChangesAsync();
            var valid = weight is 10m or 50m;
            try
            {
                await service.ConfirmReceiptAsync(staffId, batch.Id, new(weight, 1, true, null));
                if (!valid) throw new Exception($"Accepted invalid receipt weight {weight}");
            }
            catch (InvalidOperationException) when (!valid) { }
            db.ChangeTracker.Clear();
            var saved = await db.ClassifiedBatches.SingleAsync(x => x.Id == batch.Id);
            var inventory = await db.Inventories.SingleOrDefaultAsync(x => x.ClassifiedBatchId == batch.Id);
            if (valid ? saved.Status != "WarehouseReceived" || inventory?.TotalWeight != weight
                      : saved.Status != "PendingWarehouseReceipt" || inventory != null)
                throw new Exception($"Unexpected receipt state for {weight}");
            // Keep the existing transfer assertions independent of these receipt fixtures.
            var transactions = await db.InventoryTransactions.Where(x => x.ReferenceId == batch.Id).ToListAsync();
            foreach (var transaction in transactions)
            {
                db.TransactionItems.RemoveRange(await db.TransactionItems.Where(x => x.TransactionId == transaction.Id).ToListAsync());
            }
            db.InventoryTransactions.RemoveRange(transactions);
            if (inventory != null) db.Inventories.Remove(inventory);
            db.ClassifiedBatches.Remove(saved);
            await db.SaveChangesAsync();
            Console.WriteLine($"PASS warehouse receipt weight {weight}");
        }
    }
}
