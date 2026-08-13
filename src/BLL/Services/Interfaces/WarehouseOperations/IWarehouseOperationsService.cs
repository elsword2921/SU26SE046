using BLL.DTOs;

namespace BLL.Services.Interfaces.WarehouseOperations;

public interface IWarehouseOperationsService
{
    Task<WarehouseDashboardDto> GetDashboardAsync(Guid userId, Guid? warehouseId);
    Task<WarehouseLayoutDto> GetLayoutAsync(Guid userId, Guid? warehouseId);
    Task<IReadOnlyList<WarehouseInboundBatchDto>> GetInboundBatchesAsync(Guid userId, Guid? warehouseId);
    Task<IReadOnlyList<WarehouseIntakeTraceDto>> GetIntakeTracesAsync(Guid userId, Guid? warehouseId);
    Task<WarehouseDetailsDto> GetWarehouseAsync(Guid userId, Guid warehouseId);
    Task<Guid> CreateWarehouseAsync(Guid userId, CreateWarehouseDto dto);
    Task UpdateWarehouseAsync(Guid userId, Guid warehouseId, CreateWarehouseDto dto);
    Task DeleteWarehouseAsync(Guid userId, Guid warehouseId);
    Task<Guid> CreateAreaAsync(Guid userId, SaveWarehouseAreaDto dto);
    Task UpdateAreaAsync(Guid userId, Guid areaId, SaveWarehouseAreaDto dto);
    Task DeleteAreaAsync(Guid userId, Guid areaId);
    Task<Guid> CreateGroupAsync(Guid userId, SaveWarehouseGroupDto dto);
    Task UpdateGroupAsync(Guid userId, Guid groupId, SaveWarehouseGroupDto dto);
    Task DeleteGroupAsync(Guid userId, Guid groupId);
    Task<Guid> CreateLocationAsync(Guid userId, SaveStorageLocationDto dto);
    Task UpdateLocationAsync(Guid userId, Guid locationId, SaveStorageLocationDto dto);
    Task DeleteLocationAsync(Guid userId, Guid locationId);
    Task<WarehouseInboundBatchDto?> GetBatchAsync(Guid batchId);
    Task ConfirmReceiptAsync(Guid staffId, Guid batchId, ConfirmWarehouseReceiptDto dto);
    Task<IReadOnlyList<StorageLocationDto>> GetLocationsAsync(Guid batchId);
    Task PutawayAsync(Guid staffId, Guid batchId, PutawayBatchDto dto);
    Task<IReadOnlyList<WarehouseInventoryDto>> GetInventoryAsync(Guid userId, Guid? warehouseId, string? search);
    Task<IReadOnlyList<WarehouseInventoryDto>> GetLocationInventoryAsync(Guid userId, Guid locationId);
    Task<IReadOnlyList<WarehouseTransactionDto>> GetTransactionsAsync(Guid userId, Guid? warehouseId, string? type);
    Task IssueAsync(Guid staffId, Guid inventoryId, IssueInventoryDto dto);
    Task MoveAsync(Guid staffId, Guid inventoryId, MoveInventoryDto dto);
}
