using BLL.DTOs;

namespace BLL.Services.Interfaces.ClassificationOperations;

public interface IClassificationOperationsService
{
    Task<IReadOnlyList<ClassificationBatchSummaryDto>> GetBatchesAsync(Guid staffId);
    Task<ClassificationBatchDetailDto?> GetBatchAsync(Guid staffId, Guid batchId);
    Task<ClassificationCatalogDto> GetCatalogAsync();
    Task<ClassificationAreaLayoutDto> GetClassificationAreaLayoutAsync(Guid staffId, DateTime? date);
    Task StartBatchAsync(Guid staffId, Guid batchId);
    Task ConfirmReceiptAsync(Guid staffId, Guid batchId);
    Task CountBatchAsync(Guid staffId, Guid batchId, CountClassificationBatchDto dto);
    Task<ClassificationItemDto> ClassifyItemAsync(Guid staffId, Guid batchId, ClassifyItemDto dto);
    Task<ClassificationItemDto> UpdateItemAsync(Guid staffId, Guid batchId, Guid itemId, ClassifyItemDto dto);
    Task DeleteItemAsync(Guid staffId, Guid batchId, Guid itemId);
    Task CompleteBatchAsync(Guid staffId, Guid batchId);
    Task<IReadOnlyList<GroupedClassifiedBatchDto>> GetGroupedBatchesAsync(DateTime? date);
    Task<GroupedClassifiedBatchDetailDto?> GetGroupedBatchAsync(Guid groupedBatchId);
    Task SendGroupedBatchToWarehouseAsync(Guid staffId, Guid groupedBatchId);
    Task<SendGroupedBatchesToWarehouseResultDto> SendGroupedBatchesToWarehouseAsync(
        Guid staffId, IReadOnlyList<Guid> groupedBatchIds);
    Task StartTeamAsync(Guid staffId, Guid teamId);
    Task CompleteTeamAsync(Guid staffId, Guid teamId);
    Task<ClassificationManagementBoardDto> GetManagementBoardAsync(Guid? warehouseId, DateTime? date);
    Task AssignBatchAsync(Guid managerId, Guid batchId, Guid teamId);
}
