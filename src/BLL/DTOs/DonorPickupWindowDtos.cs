namespace BLL.DTOs;

public record DonorPickupWindowDto(
    Guid ShiftId,
    string ShiftName,
    TimeSpan StartTime,
    TimeSpan EndTime,
    string DisplayTime);

public record DonorPickupAvailabilityDto(
    Guid WarehouseId,
    List<DonorPickupWindowDto> Windows);
