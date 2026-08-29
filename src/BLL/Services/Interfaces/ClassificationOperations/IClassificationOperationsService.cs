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
    Task<IReadOnlyList<GroupedClassifiedBatchDto>> GetGroupedBatchesAsync(Guid staffId, DateTime? date);
    Task<GroupedClassifiedBatchDetailDto?> GetGroupedBatchAsync(Guid staffId, Guid groupedBatchId);
    Task<IReadOnlyList<UnassignedClassifiedItemDto>> GetUnassignedItemsAsync(Guid staffId);
    Task<GroupedClassifiedBatchDetailDto> CreateManualBatchAsync(Guid staffId, CreateManualClassifiedBatchDto dto);
    Task AssignItemsAsync(Guid staffId, Guid groupedBatchId, IReadOnlyList<Guid> itemIds);
    Task RemoveItemAsync(Guid staffId, Guid groupedBatchId, Guid itemId);
    Task FinalizeManualBatchAsync(Guid staffId, Guid groupedBatchId);
    Task PlaceGroupedBatchAsync(Guid staffId, Guid groupedBatchId, PlaceGroupedClassifiedBatchDto dto);
    Task SendGroupedBatchToWarehouseAsync(Guid staffId, Guid groupedBatchId);
    Task<SendGroupedBatchesToWarehouseResultDto> SendGroupedBatchesToWarehouseAsync(
        Guid staffId, IReadOnlyList<Guid> groupedBatchIds);
    Task StartTeamAsync(Guid staffId, Guid teamId);
    Task CompleteTeamAsync(Guid staffId, Guid teamId);
    Task<ClassificationManagementBoardDto> GetManagementBoardAsync(Guid? warehouseId, DateTime? date);
    Task AssignBatchAsync(Guid managerId, Guid batchId, Guid teamId);
    Task<AutoBalanceClassificationResultDto> AutoBalanceBatchesAsync(Guid managerId, Guid shiftId);
}
