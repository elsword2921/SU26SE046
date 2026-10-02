using DAL;
using DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace BLL.Services.Implements.WarehouseOperations;

public static class WarehouseCollectionPoints
{
    public static async Task<List<Warehouse>> ListAsync(AppDbContext context)
    {
        var warehouses = await context.Warehouses.AsNoTracking()
            .Where(x => x.IsActive != false).OrderByDescending(x => x.CreateAt).ToListAsync();
        // Area counters cover receiving, classification and storage. The warehouse
        // counter is maintained by storage transactions only. Do not add inventory
        // or historical batches again: their occupied weight is already in an area.
        var weights = await context.WarehouseAreas.AsNoTracking()
            .Where(x => x.IsActive != false)
            .GroupBy(x => x.WarehouseId)
            .Select(g => new { WarehouseId = g.Key, Weight = g.Sum(x => x.CurrentKg) })
            .ToDictionaryAsync(x => x.WarehouseId, x => x.Weight);
        foreach (var warehouse in warehouses)
            warehouse.CurrentWeight = weights.GetValueOrDefault(warehouse.Id);
        return warehouses;
    }
}
