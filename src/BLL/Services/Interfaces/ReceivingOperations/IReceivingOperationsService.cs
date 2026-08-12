using BLL.DTOs;

namespace BLL.Services.Interfaces.ReceivingOperations;

public interface IReceivingOperationsService
{
    Task GenerateStandardShiftsAsync(GenerateShiftsDto dto);
    Task<GenerateMonthShiftsResultDto> GenerateMonthShiftsAsync(GenerateMonthShiftsDto dto);
    Task<GenerateYearShiftsResultDto> GenerateYearShiftsAsync(GenerateYearShiftsDto dto);
    Task<GenerateShiftsResultDto> GenerateShiftsAsync(GenerateShiftsV2Dto dto);
    Task<DeleteYearShiftsResultDto> DeleteYearShiftsAsync(DeleteYearShiftsDto dto);
    Task UpdateShiftAsync(Guid shiftId, UpdateManagerShiftDto dto);
    Task DeleteShiftAsync(Guid shiftId);
    Task<Guid> CreateTeamAsync(CreateReceivingTeamDto dto);
    Task UpdateTeamAsync(Guid teamId, UpdateReceivingTeamDto dto);
    Task DeleteTeamAsync(Guid teamId);
    Task<int> PlanShiftAsync(PlanReceivingShiftDto dto);
    Task<AutoBalanceResultDto> AutoBalanceShiftAsync(Guid shiftId);
    Task<ReceivingDispatchBoardDto> GetDispatchBoardAsync();
    Task<ManagerReceivingSetupDto> GetManagerSetupAsync();
    Task<List<ManagerWarehouseOptionDto>> GetManagerWarehousesAsync();
    Task<List<ManagerStaffOptionDto>> GetManagerReceivingStaffAsync(Guid? warehouseId = null);
    Task<List<ManagerShiftOverviewDto>> GetManagerShiftsAsync(
        Guid? warehouseId = null, DateTime? fromDate = null, DateTime? toDate = null);
    Task AssignRequestAsync(AssignDonationRequestDto dto);
    Task<List<ReceivingBatchDto>> GetMyBatchesAsync(Guid staffId);
    Task<ReceivingBatchDto?> GetMyBatchAsync(Guid staffId, Guid batchId);
    Task StartBatchAsync(Guid staffId, Guid batchId);
    Task CompleteShiftAsync(Guid staffId, Guid shiftId);
    Task ConfirmPickupAsync(Guid staffId, Guid batchId, Guid requestId, ConfirmPickupDto dto);
    Task<WarehouseDropOffBoardDto> GetMyWarehouseDropOffsAsync(Guid staffId);
    Task ConfirmWarehouseDropOffAsync(Guid staffId, Guid requestId, ConfirmPickupDto dto);
    Task RescheduleAsync(Guid staffId, Guid batchId, Guid requestId, ReschedulePickupDto dto);
    Task RejectAsync(Guid staffId, Guid batchId, Guid requestId, RejectPickupDto dto);
    Task CompleteBatchAsync(Guid staffId, Guid batchId);
    Task ReceiveBatchAtWarehouseAsync(Guid staffId, Guid batchId, ReceiveIntakeBatchAtWarehouseDto dto);
    Task SendToClassificationAsync(Guid staffId, Guid batchId);
}
