namespace BLL.Services.Interfaces.ProcessingOperations;

using BLL.DTOs;

public interface IProcessingOperationsService
{
    Task ScheduleReturnAsync(Guid organizationId, Guid operationId, ScheduleRecyclingReturnDto dto);
    Task DispatchReturnAsync(Guid organizationId, Guid operationId, DispatchRecyclingReturnDto dto);
    Task ReceiveReturnAsync(Guid staffId, Guid operationId, ReceiveRecyclingReturnDto dto);
    Task<RecyclingReceiptOptionsDto> ReturnReceiptOptionsAsync(Guid staffId, Guid operationId);
    Task CreateGhnShipmentAsync(Guid staffId, Guid operationId, CreateProcessingGhnShipmentDto dto);
    Task RefreshGhnAsync(Guid userId, Guid operationId);
    Task<ProcessingCatalogDto> CatalogAsync(Guid userId, Guid? warehouseId, string? operationType);
    Task ReceiveAsync(Guid organizationId, Guid operationId);
    Task CompleteAsync(Guid organizationId, Guid operationId, CompleteProcessingOperationDto dto);
    Task CancelAsync(Guid managerId, Guid operationId, ProcessingOperationDecisionDto dto);
    Task<ProcessingOperationCreatedDto> CreateAsync(
        Guid userId,
        CreateProcessingOperationDto dto);

    Task ApproveByOrganizationAsync(
        Guid organizationId,
        Guid operationId);

    Task RejectByOrganizationAsync(
        Guid organizationId,
        Guid operationId,
        ProcessingOperationDecisionDto dto);

    Task ApproveByManagerAsync(
        Guid managerId,
        Guid operationId);

    Task RejectByManagerAsync(
        Guid managerId,
        Guid operationId,
        ProcessingOperationDecisionDto dto);

    Task<List<ProcessingOperationListDto>> GetListAsync(
        Guid userId,
        string? status);

    Task<ProcessingOperationDetailDto> GetByIdAsync(
        Guid userId,
        Guid operationId);

    Task IssueAsync(
        Guid staffId,
        Guid operationId,
        IssueProcessingOperationDto dto);
}
