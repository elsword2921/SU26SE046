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

public record EligibleWarehouseDto(
    Guid Id,
    string Name,
    string Address,
    double DistanceKm,
    decimal AvailableCapacityKg,
    decimal MaxBatchWeightKg,
    int MaxBatchItemCount,
    decimal MaxBatchVolumeLiters);
