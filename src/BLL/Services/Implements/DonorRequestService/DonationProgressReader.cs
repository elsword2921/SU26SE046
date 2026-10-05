using BLL.DTOs;
using DAL;
using Microsoft.EntityFrameworkCore;

namespace BLL.Services.Implements.DonorRequestService;

public static class DonationProgressReader
{
    public static async Task PopulateAsync(AppDbContext db, List<DonorRequestSearchResultDto> requests)
    {
        var ids = requests.Where(x => x.Status == "Classified").Select(x => x.Id).ToList();
        if (ids.Count == 0) return;

        // Provenance identifies related batches, not individual garments from each donor.
        var sources = await db.ClassifiedBatchDonationRequests.AsNoTracking()
            .Where(x => ids.Contains(x.DonationRequestId) && x.IsActive != false && x.ClassifiedBatch.IsActive != false)
            .Select(x => new { x.DonationRequestId, x.ClassifiedBatchId, x.ClassifiedBatch.Status }).ToListAsync();
        var batchIds = sources.Select(x => x.ClassifiedBatchId).Distinct().ToList();
        if (batchIds.Count == 0) return;
        var remaining = await db.Inventories.AsNoTracking()
            .Where(x => x.ClassifiedBatchId.HasValue && batchIds.Contains(x.ClassifiedBatchId.Value)
                && x.IsActive != false && x.TotalWeight > 0 && x.StorageLocationId != null)
            .Select(x => x.ClassifiedBatchId).Distinct().ToListAsync();

        var charity = await db.DistributionItems.AsNoTracking()
            .Where(x => x.Inventory.ClassifiedBatchId.HasValue && batchIds.Contains(x.Inventory.ClassifiedBatchId.Value) && x.IsActive != false
                && x.DistributionRequest.IsActive != false && x.IssuedWeight > 0)
            .Select(x => new { x.Inventory.ClassifiedBatchId, x.DistributionRequest.Status }).ToListAsync();
        var processing = await db.ProcessingOperationInputs.AsNoTracking()
            .Where(x => x.Inventory.ClassifiedBatchId.HasValue && batchIds.Contains(x.Inventory.ClassifiedBatchId.Value) && x.IsActive != false
                && x.ProcessingOperation.IsActive != false && x.IssuedWeight > 0)
            .Select(x => new { x.Inventory.ClassifiedBatchId, x.ProcessingOperation.OperationType,
                x.ProcessingOperation.Status, x.ProcessingOperation.ProcessingCompletedAt }).ToListAsync();

        foreach (var request in requests.Where(x => ids.Contains(x.Id)))
        {
            var related = sources.Where(x => x.DonationRequestId == request.Id).ToList();
            var labels = new List<string>();
            foreach (var batch in related)
            {
                var outcomes = new List<string>();
                foreach (var entry in charity.Where(x => x.ClassifiedBatchId == batch.ClassifiedBatchId))
                    outcomes.Add(entry.Status switch
                    {
                        "OrganizationReceived" => "Tổ chức từ thiện đã nhận",
                        "Returned" => "Hàng từ thiện đã trả về",
                        "Cancelled" or "ShipmentCancelled" => "Chuyến từ thiện đã hủy",
                        _ => "Đã xuất đi từ thiện",
                    });
                foreach (var entry in processing.Where(x => x.ClassifiedBatchId == batch.ClassifiedBatchId))
                {
                    var recycling = entry.OperationType == "Recycling";
                    outcomes.Add(entry.Status switch
                    {
                        "Cancelled" or "ShipmentCancelled" => recycling ? "Chuyến tái chế đã hủy" : "Chuyến tiêu hủy đã hủy",
                        "Returned" => recycling ? "Hàng tái chế đã trả về" : "Hàng tiêu hủy đã trả về",
                        _ when entry.ProcessingCompletedAt != null => recycling ? "Đã xử lý tái chế" : "Đã tiêu hủy",
                        "Processing" => recycling ? "Đang tái chế" : "Đang tiêu hủy",
                        "OrganizationReceived" => recycling ? "Tổ chức tái chế đã nhận" : "Tổ chức tiêu hủy đã nhận",
                        _ => recycling ? "Đã xuất đi tái chế" : "Đã xuất đi tiêu hủy",
                    });
                }
                if (outcomes.Count > 0)
                {
                    if (remaining.Contains(batch.ClassifiedBatchId)) labels.Add("Còn hàng trong kho");
                    labels.AddRange(outcomes);
                }
                else labels.Add(batch.Status switch
                {
                    "Stored" => "Đã lưu kho",
                    "WarehouseReceived" => "Kho đã nhận, chờ xếp vị trí",
                    "PendingWarehouseReceipt" => "Đang chờ kho tiếp nhận",
                    _ => "Đã phân loại, chờ chuyển kho",
                });
            }
            if (labels.Count == 0) continue;
            request.StatusText = string.Join(" · ", labels.Distinct());
            request.ProgressNote = "Tiến độ các lô hàng có liên kết với đơn. Một lô có thể gồm đồ từ nhiều người quyên góp; đây không phải xác nhận cho từng món đồ.";
        }
    }
}
