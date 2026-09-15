namespace BLL.DTOs;

public record DonorNearestWarehouseDto(Guid Id, string WarehouseName, string Address);

public record DonorPickupWindowDto(
    Guid ShiftId,
    string ShiftName,
    TimeSpan StartTime,
    TimeSpan EndTime,
    string DisplayTime);

public record DonorPickupAvailabilityDto(
    Guid WarehouseId,
    List<DonorPickupWindowDto> Windows);
