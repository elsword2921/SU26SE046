using BLL.DTOs;
using BLL.Services.Implements.DonorRequestService;
using DAL;
using DAL.Models;
using Microsoft.EntityFrameworkCore;

internal static class DonationProgressChecks
{
    public static async Task RunAsync(DbContextOptions<AppDbContext> options, Guid batchId, Guid requestId, Guid actorId, Guid warehouseId)
    {
        await using var db = new AppDbContext(options);
        var batch = await db.ClassifiedBatches.FindAsync(batchId) ?? throw new Exception("Missing batch");
        batch.Status = "Stored";
        var inventory = new Inventory { Id = Guid.NewGuid(), WarehouseId = warehouseId, ClassifiedBatchId = batchId, Sku = "PROGRESS" };
        db.Inventories.Add(inventory);
        await db.SaveChangesAsync();
        async Task<string> Read()
        {
            var rows = new List<DonorRequestSearchResultDto> { new() { Id = requestId, Status = "Classified", StatusText = "Original" } };
            await DonationProgressReader.PopulateAsync(db, rows);
            return rows.Single().StatusText;
        }
        if (await Read() != "Đã lưu kho") throw new Exception("Stored progress missing");
        var distribution = new DistributionRequest { Id = Guid.NewGuid(), UserId = actorId, WarehouseId = warehouseId, RequestCode = "PROGRESS", Status = "Approved" };
        var charity = new DistributionItem { Id = Guid.NewGuid(), Inventory = inventory, DistributionRequest = distribution };
        db.DistributionItems.Add(charity);
        await db.SaveChangesAsync();
        if (await Read() != "Đã lưu kho") throw new Exception("Unissued allocation reported as shipped");
        charity.IssuedWeight = 5;
        distribution.Status = "OrganizationReceived";
        foreach (var type in new[] { "Recycling", "Disposal" })
        {
            db.ProcessingOperationInputs.Add(new ProcessingOperationInput {
                Id = Guid.NewGuid(), Inventory = inventory, ClassifiedBatchId = batchId, IssuedWeight = 1,
                ProcessingOperation = new ProcessingOperation {
                    Id = Guid.NewGuid(), WarehouseId = warehouseId, OrganizationId = actorId,
                    OperationCode = "PROGRESS-" + type, OperationType = type, Status = "Completed", ProcessingCompletedAt = DateTime.UtcNow
                }
            });
        }
        await db.SaveChangesAsync();
        var text = await Read();
        if (!text.Contains("Tổ chức từ thiện đã nhận") || !text.Contains("Đã xử lý tái chế") || !text.Contains("Đã tiêu hủy"))
            throw new Exception("Mixed outcomes lost");
        var unrelated = new DonorRequestSearchResultDto { Id = Guid.NewGuid(), Status = "Classified", StatusText = "Original" };
        await DonationProgressReader.PopulateAsync(db, [unrelated]);
        if (unrelated.StatusText != "Original") throw new Exception("Unrelated donor changed");
        Console.WriteLine("PASS donor progress: stored, unissued, mixed outcomes, unrelated request");
    }
}
