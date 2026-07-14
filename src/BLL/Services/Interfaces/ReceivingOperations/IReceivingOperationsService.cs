using BLL.DTOs;

namespace BLL.Services.Interfaces.ReceivingOperations;

public interface IReceivingOperationsService
{
    Task GenerateStandardShiftsAsync(GenerateShiftsDto dto);
    Task<Guid> CreateTeamAsync(CreateReceivingTeamDto dto);
    Task<int> PlanShiftAsync(PlanReceivingShiftDto dto);
    Task<List<ReceivingBatchDto>> GetMyBatchesAsync(Guid staffId);
    Task<ReceivingBatchDto?> GetMyBatchAsync(Guid staffId, Guid batchId);
    Task StartBatchAsync(Guid staffId, Guid batchId);
    Task CompleteShiftAsync(Guid staffId, Guid shiftId);
    Task ConfirmPickupAsync(Guid staffId, Guid batchId, Guid requestId, ConfirmPickupDto dto);
    Task RescheduleAsync(Guid staffId, Guid batchId, Guid requestId, ReschedulePickupDto dto);
    Task RejectAsync(Guid staffId, Guid batchId, Guid requestId, RejectPickupDto dto);
    Task CompleteBatchAsync(Guid staffId, Guid batchId);
}
