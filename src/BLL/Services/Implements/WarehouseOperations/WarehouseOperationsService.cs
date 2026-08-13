using BLL.DTOs;
using BLL.Common;
using BLL.Services.Interfaces.WarehouseOperations;
using BLL.Services.Implements.Notifications;
using DAL;
using DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace BLL.Services.Implements.WarehouseOperations;

public class WarehouseOperationsService(AppDbContext context) : IWarehouseOperationsService
{
    public async Task<WarehouseDetailsDto> GetWarehouseAsync(Guid userId, Guid warehouseId)
    {
        await RequireManagerAsync(userId);
        var warehouse = await context.Warehouses.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == warehouseId && x.IsActive != false)
            ?? throw new InvalidOperationException("Warehouse not found.");
        var allocatedAreaCapacity = await context.WarehouseAreas.AsNoTracking()
            .Where(x => x.WarehouseId == warehouseId && x.IsActive != false)
            .SumAsync(x => (decimal?)x.CapacityKg) ?? 0;
        var actualWeight = await GetWarehouseActualWeightAsync(warehouseId);
        return new WarehouseDetailsDto(warehouse.Id, warehouse.WarehouseName, warehouse.Address,
            warehouse.PhoneNumber, warehouse.Email, warehouse.Description, warehouse.TotalCapacityKg,
            actualWeight, allocatedAreaCapacity);
    }

    public async Task<Guid> CreateWarehouseAsync(Guid userId, CreateWarehouseDto dto)
    {
        await RequireManagerAsync(userId);
        var name = dto.WarehouseName?.Trim() ?? string.Empty;
        var address = dto.Address?.Trim() ?? string.Empty;
        if (name.Length is < 3 or > 150)
            throw new InvalidOperationException("Warehouse name must contain 3-150 characters.");
        if (address.Length is < 10 or > 500)
            throw new InvalidOperationException("Warehouse address must contain 10-500 characters.");
        if (dto.TotalCapacityKg <= 0)
            throw new InvalidOperationException("Warehouse capacity must be greater than zero.");
        if (dto.TotalCapacityKg > 10_000_000)
            throw new InvalidOperationException("Warehouse capacity is too large.");
        if (!string.IsNullOrWhiteSpace(dto.Email)
            && !System.Net.Mail.MailAddress.TryCreate(dto.Email.Trim(), out _))
            throw new InvalidOperationException("Warehouse email format is invalid.");

        var normalizedName = name.ToLower();
        var normalizedAddress = address.ToLower();
        if (await context.Warehouses.AnyAsync(x => x.IsActive != false
                && x.WarehouseName.ToLower() == normalizedName))
            throw new InvalidOperationException("An active warehouse with this name already exists.");
        if (await context.Warehouses.AnyAsync(x => x.IsActive != false
                && x.Address.ToLower() == normalizedAddress))
            throw new InvalidOperationException("An active warehouse with this address already exists.");

        var warehouse = new Warehouse
        {
            Id = Guid.NewGuid(), WarehouseName = name, Address = address,
            PhoneNumber = string.IsNullOrWhiteSpace(dto.PhoneNumber) ? null : dto.PhoneNumber.Trim(),
            Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim().ToLowerInvariant(),
            Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
            TotalCapacityKg = dto.TotalCapacityKg, CurrentWeight = 0,
            CreateAt = DateTime.UtcNow, CreatedBy = userId, IsActive = true
        };
        context.Warehouses.Add(warehouse);
        await context.SaveChangesAsync();
        await EnsureDefaultLayoutAsync(warehouse.Id);
        return warehouse.Id;
    }

    public async Task UpdateWarehouseAsync(Guid userId, Guid warehouseId, CreateWarehouseDto dto)
    {
        await RequireManagerAsync(userId);
        ValidateWarehouse(dto);
        var warehouse = await context.Warehouses
            .FirstOrDefaultAsync(x => x.Id == warehouseId && x.IsActive != false)
            ?? throw new InvalidOperationException("Warehouse not found.");
        var name = dto.WarehouseName.Trim();
        var address = dto.Address.Trim();
        var normalizedName = name.ToLower();
        var normalizedAddress = address.ToLower();
        if (await context.Warehouses.AnyAsync(x => x.Id != warehouseId && x.IsActive != false
                && x.WarehouseName.ToLower() == normalizedName))
            throw new InvalidOperationException("An active warehouse with this name already exists.");
        if (await context.Warehouses.AnyAsync(x => x.Id != warehouseId && x.IsActive != false
                && x.Address.ToLower() == normalizedAddress))
            throw new InvalidOperationException("An active warehouse with this address already exists.");

        var allocatedAreaCapacity = await context.WarehouseAreas
            .Where(x => x.WarehouseId == warehouseId && x.IsActive != false)
            .SumAsync(x => (decimal?)x.CapacityKg) ?? 0;
        var actualWeight = await GetWarehouseActualWeightAsync(warehouseId);
        var minimumCapacity = Math.Max(allocatedAreaCapacity, actualWeight);
        if (dto.TotalCapacityKg < minimumCapacity)
            throw new InvalidOperationException(
                $"Warehouse capacity cannot be lower than {minimumCapacity} kg currently allocated or stored.");

        warehouse.WarehouseName = name;
        warehouse.Address = address;
        warehouse.PhoneNumber = string.IsNullOrWhiteSpace(dto.PhoneNumber) ? null : dto.PhoneNumber.Trim();
        warehouse.Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim().ToLowerInvariant();
        warehouse.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
        warehouse.TotalCapacityKg = dto.TotalCapacityKg;
        warehouse.CurrentWeight = actualWeight;
        warehouse.UpdateAt = DateTime.UtcNow;
        warehouse.UpdatedBy = userId;
        await context.SaveChangesAsync();
    }

    public async Task DeleteWarehouseAsync(Guid userId, Guid warehouseId)
    {
        await RequireManagerAsync(userId);
        var warehouse = await context.Warehouses
            .FirstOrDefaultAsync(x => x.Id == warehouseId && x.IsActive != false)
            ?? throw new InvalidOperationException("Warehouse not found.");
        if (await context.Warehouses.CountAsync(x => x.IsActive != false) <= 1)
            throw new InvalidOperationException("The last active warehouse cannot be deleted.");

        var hasUsers = await context.Users.AnyAsync(x => x.WarehouseId == warehouseId && x.IsActive != false);
        var hasOperations = await context.DonationRequests.AnyAsync(x => x.WarehouseId == warehouseId && x.IsActive != false)
            || await context.IntakeBatches.AnyAsync(x => x.WarehouseId == warehouseId && x.IsActive != false)
            || await context.ClassifiedBatches.AnyAsync(x => x.WarehouseId == warehouseId && x.IsActive != false)
            || await context.Inventories.AnyAsync(x => x.WarehouseId == warehouseId && x.IsActive != false)
            || await context.Shifts.AnyAsync(x => x.WarehouseId == warehouseId && x.IsActive != false)
            || await context.DistributionRequests.AnyAsync(x => x.WarehouseId == warehouseId && x.IsActive != false)
            || await context.TransferRequests.AnyAsync(x => x.WarehouseId == warehouseId && x.IsActive != false);
        if (hasUsers || hasOperations)
            throw new InvalidOperationException(
                "Warehouse cannot be deleted while it still has staff, requests, shifts, batches or inventory.");

        var now = DateTime.UtcNow;
        var areas = await context.WarehouseAreas.Where(x => x.WarehouseId == warehouseId && x.IsActive != false).ToListAsync();
        var areaIds = areas.Select(x => x.Id).ToList();
        var groups = await context.AreaGroups.Where(x => areaIds.Contains(x.AreaId) && x.IsActive != false).ToListAsync();
        var locations = await context.StorageLocations.Where(x => x.WarehouseId == warehouseId && x.IsActive != false).ToListAsync();
        var templates = await context.WorkScheduleTemplates.Where(x => x.WarehouseId == warehouseId && x.IsActive != false).ToListAsync();
        foreach (var entity in areas.Cast<DAL.Models.Commons.BaseEntity>()
                     .Concat(groups).Concat(locations).Concat(templates).Append(warehouse))
        {
            entity.IsActive = false;
            entity.DeleteAt = now;
            entity.DeletedBy = userId;
        }
        await context.SaveChangesAsync();
    }

    public async Task<WarehouseLayoutDto> GetLayoutAsync(Guid userId, Guid? requestedWarehouseId)
    {
        var warehouseId = await ResolveWarehouseIdAsync(userId, requestedWarehouseId);
        var warehouse = await context.Warehouses.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == warehouseId && x.IsActive != false);
        if (warehouse is null) throw new InvalidOperationException("Warehouse not found for this staff account.");
        await EnsureDefaultLayoutAsync(warehouse.Id);

        var areas = await context.WarehouseAreas.AsNoTracking()
            .Where(x => x.WarehouseId == warehouse.Id && x.IsActive != false)
            .OrderBy(x => x.AreaName).ToListAsync();
        var groups = await context.AreaGroups.AsNoTracking()
            .Where(x => areas.Select(a => a.Id).Contains(x.AreaId) && x.IsActive != false)
            .OrderBy(x => x.GroupName).ToListAsync();
        var locations = await context.StorageLocations.AsNoTracking()
            .Where(x => x.WarehouseId == warehouse.Id && x.IsActive != false)
            .OrderBy(x => x.AisleCode).ThenBy(x => x.RackCode).ThenBy(x => x.ShelfCode).ThenBy(x => x.BinCode)
            .ToListAsync();
        var inventoryStats = await context.Inventories.AsNoTracking()
            .Where(x => x.WarehouseId == warehouse.Id && x.StorageLocationId.HasValue && x.IsActive != false)
            .GroupBy(x => x.StorageLocationId!.Value)
            .Select(x => new
            {
                LocationId = x.Key,
                Count = x.Count(),
                Quantity = x.Sum(i => i.Quantity),
                WeightKg = x.Sum(i => i.TotalWeight)
            })
            .ToDictionaryAsync(x => x.LocationId);
        var stagingBatches = await context.IntakeBatches.AsNoTracking()
            .Include(x => x.ClassificationTeam)
            .Include(x => x.CurrentAreaGroup)
            .Include(x => x.CurrentStorageLocation)
            .Include(x => x.WarehouseReceivedByStaff)
            .Where(x => x.WarehouseId == warehouse.Id && x.CurrentAreaId.HasValue && x.IsActive != false)
            .Select(x => new
            {
                x.Id, x.CurrentAreaId, x.CurrentStorageLocationId, x.BatchCode, x.Status,
                StorageAreaId = x.CurrentStorageLocation != null
                    ? (Guid?)x.CurrentStorageLocation.AreaId : null,
                x.TotalWeight, x.IntakeDate,
                DonationRequests = x.IntakeBatchDonationRequests.Count(r => r.IsActive != false),
                TeamName = x.ClassificationTeam != null ? x.ClassificationTeam.TeamName : null,
                LocationCode = x.CurrentStorageLocation != null
                    ? x.CurrentStorageLocation.LocationCode : null,
                GroupName = x.CurrentAreaGroup != null ? x.CurrentAreaGroup.GroupName : null,
                x.WarehouseReceivedAt,
                WarehouseReceivedBy = x.WarehouseReceivedByStaff != null
                    ? x.WarehouseReceivedByStaff.FullName : null
            }).ToListAsync();

        var stagingPlacements = await context.IntakeBatches.AsNoTracking()
            .Where(x => x.WarehouseId == warehouse.Id && x.CurrentStorageLocationId.HasValue
                && x.IsActive != false)
            .Select(x => new
            {
                x.CurrentAreaId,
                x.CurrentAreaGroupId,
                LocationId = x.CurrentStorageLocationId!.Value,
                x.TotalWeight
            })
            .ToListAsync();
        var stagingAreaStats = stagingPlacements.Where(x => x.CurrentAreaId.HasValue)
            .GroupBy(x => x.CurrentAreaId!.Value)
            .ToDictionary(x => x.Key, x => new { BatchCount = x.Count(), WeightKg = x.Sum(y => y.TotalWeight) });
        var stagingGroupStats = stagingPlacements.Where(x => x.CurrentAreaGroupId.HasValue)
            .GroupBy(x => x.CurrentAreaGroupId!.Value)
            .ToDictionary(x => x.Key, x => new { BatchCount = x.Count(), WeightKg = x.Sum(y => y.TotalWeight) });
        var stagingLocationStats = stagingPlacements.GroupBy(x => x.LocationId)
            .ToDictionary(x => x.Key, x => new { BatchCount = x.Count(), WeightKg = x.Sum(y => y.TotalWeight) });

        var areaDtos = areas.Select(area =>
        {
            var isStagingArea = !string.Equals(area.AreaType, "Storage", StringComparison.OrdinalIgnoreCase);
            stagingAreaStats.TryGetValue(area.Id, out var areaStats);
            return new WarehouseAreaLayoutDto(
            area.Id, area.AreaName, area.Description, area.AreaType, area.CapacityKg,
            isStagingArea ? areaStats?.WeightKg ?? area.CurrentKg : area.CurrentKg,
            groups.Where(x => x.AreaId == area.Id).Select(x =>
            {
                stagingGroupStats.TryGetValue(x.Id, out var groupStats);
                return new WarehouseGroupLayoutDto(x.Id, x.GroupName, x.Description, x.CapacityKg,
                    isStagingArea ? groupStats?.WeightKg ?? x.CurrentKg : x.CurrentKg);
            }).ToList(),
            locations.Where(x => x.AreaId == area.Id).Select(x =>
            {
                inventoryStats.TryGetValue(x.Id, out var stats);
                stagingLocationStats.TryGetValue(x.Id, out var stagingStats);
                return new WarehouseLocationLayoutDto(x.Id, x.AreaGroupId, x.LocationCode, x.AisleCode, x.RackCode,
                    x.ShelfCode, x.BinCode, x.PreferredGarmentGroup, x.PreferredProcessingDirection,
                    x.CapacityKg,
                    isStagingArea ? stagingStats?.WeightKg ?? x.CurrentWeightKg : stats?.WeightKg ?? x.CurrentWeightKg,
                    x.Status,
                    isStagingArea ? stagingStats?.BatchCount ?? 0 : stats?.Count ?? 0,
                    isStagingArea ? stagingStats?.BatchCount ?? 0 : stats?.Quantity ?? 0);
            }).ToList(),
            // The physical location is authoritative. CurrentAreaId is retained as a
            // fallback for legacy/staging records that have not selected a location yet.
            stagingBatches.Where(x => (x.StorageAreaId ?? x.CurrentAreaId) == area.Id)
                .Select(x => new WarehouseStagingBatchDto(x.Id, x.BatchCode, x.Status,
                    x.TotalWeight, x.IntakeDate, x.DonationRequests, x.TeamName,
                    x.CurrentStorageLocationId, x.LocationCode, x.GroupName,
                    x.WarehouseReceivedAt, x.WarehouseReceivedBy)).ToList());
        }).ToList();
        // The warehouse total must represent everything physically present in its areas:
        // intake batches in staging areas plus classified inventory in storage areas.
        // Each area's value above already comes from its authoritative source, so summing
        // the area DTOs keeps the headline total in sync with the visible layout.
        var actualWarehouseWeightKg = areaDtos.Sum(x => x.CurrentWeightKg);
        // The configured warehouse capacity is the physical ceiling. A migration repairs
        // legacy warehouses that were bootstrapped with area totals above that ceiling.
        var allocatedAreaCapacityKg = areaDtos.Sum(x => x.CapacityKg);
        var configuredWarehouseCapacityKg = warehouse.TotalCapacityKg;
        if (allocatedAreaCapacityKg > configuredWarehouseCapacityKg)
            configuredWarehouseCapacityKg = allocatedAreaCapacityKg;
        return new WarehouseLayoutDto(warehouse.Id, warehouse.WarehouseName, warehouse.Address,
            configuredWarehouseCapacityKg, actualWarehouseWeightKg, areaDtos);
    }

    public async Task<WarehouseDashboardDto> GetDashboardAsync(Guid userId, Guid? requestedWarehouseId)
    {
        var warehouseId = await ResolveWarehouseIdAsync(userId, requestedWarehouseId);
        var pending = await context.ClassifiedBatches.CountAsync(x => x.WarehouseId == warehouseId
            && x.IsActive != false && x.Status == "PendingWarehouseReceipt");
        var putaway = await context.ClassifiedBatches.CountAsync(x => x.WarehouseId == warehouseId
            && x.IsActive != false && x.Status == "WarehouseReceived");
        var stored = await context.ClassifiedBatches.CountAsync(x => x.WarehouseId == warehouseId
            && x.IsActive != false && x.Status == "Stored");
        var inventory = await context.Inventories.AsNoTracking()
            .Where(x => x.WarehouseId == warehouseId && x.IsActive != false).ToListAsync();
        var warehouse = await context.Warehouses.AsNoTracking().FirstAsync(x => x.Id == warehouseId);
        var allocatedAreaCapacity = await context.WarehouseAreas.AsNoTracking()
            .Where(x => x.WarehouseId == warehouseId && x.IsActive != false)
            .SumAsync(x => (decimal?)x.CapacityKg) ?? 0;
        // Math.Max keeps old databases readable until the normalization migration is run.
        var capacity = Math.Max(warehouse.TotalCapacityKg, allocatedAreaCapacity);
        var stagingWeight = await context.IntakeBatches.AsNoTracking()
            .Where(x => x.WarehouseId == warehouseId && x.IsActive != false
                && x.CurrentStorageLocationId.HasValue
                && x.CurrentArea != null && x.CurrentArea.AreaType != "Storage")
            .SumAsync(x => (decimal?)x.TotalWeight) ?? 0;
        var current = inventory.Sum(x => x.TotalWeight) + stagingWeight;
        return new WarehouseDashboardDto(pending, putaway, stored,
            inventory.Sum(x => Math.Max(0, x.Quantity - x.ReservedQuantity)),
            inventory.Count(x => Math.Max(0, x.Quantity - x.ReservedQuantity) > 0),
            inventory.Sum(x => Math.Max(0, x.TotalWeight - x.ReservedWeight)),
            capacity <= 0 ? 0 : Math.Round(current / capacity * 100, 2),
            current, capacity);
    }

    public async Task<IReadOnlyList<WarehouseInboundBatchDto>> GetInboundBatchesAsync(
        Guid userId, Guid? requestedWarehouseId)
    {
        var warehouseId = await ResolveWarehouseIdAsync(userId, requestedWarehouseId);
        var batches = await BatchQuery()
            .Where(x => x.WarehouseId == warehouseId && (x.Status == "PendingWarehouseReceipt"
                || x.Status == "WarehouseReceived" || x.Status == "Stored"))
            .OrderByDescending(x => x.SentToWarehouseAt ?? x.ClassificationDate)
            .ToListAsync();
        return batches.Select(MapBatch).ToList();
    }

    public async Task<WarehouseInboundBatchDto?> GetBatchAsync(Guid batchId)
    {
        var batch = await BatchQuery().FirstOrDefaultAsync(x => x.Id == batchId);
        return batch is null ? null : MapBatch(batch);
    }

    public async Task<IReadOnlyList<WarehouseIntakeTraceDto>> GetIntakeTracesAsync(
        Guid userId, Guid? requestedWarehouseId)
    {
        var warehouseId = await ResolveWarehouseIdAsync(userId, requestedWarehouseId);
        var intakes = await context.IntakeBatches.AsNoTracking()
            .Include(x => x.IntakeBatchDonationRequests)
            .Include(x => x.ClassifiedItems.Where(i => i.IsActive != false))
                .ThenInclude(x => x.ClassifiedBatch)
                    .ThenInclude(x => x!.DonationRequestSources)
                        .ThenInclude(x => x.DonationRequest)
            .Where(x => x.WarehouseId == warehouseId && x.IsActive != false)
            .OrderByDescending(x => x.IntakeDate)
            .Take(200)
            .ToListAsync();
        var classifiedBatchIds = intakes.SelectMany(x => x.ClassifiedItems)
            .Where(x => x.ClassifiedBatchId.HasValue)
            .Select(x => x.ClassifiedBatchId!.Value).Distinct().ToList();
        var inventoryByBatch = await context.Inventories.AsNoTracking()
            .Include(x => x.StorageLocation)
            .Where(x => x.WarehouseId == warehouseId && x.IsActive != false
                && x.ClassifiedBatchId.HasValue && classifiedBatchIds.Contains(x.ClassifiedBatchId.Value))
            .ToDictionaryAsync(x => x.ClassifiedBatchId!.Value);

        return intakes.Select(intake => new WarehouseIntakeTraceDto(
            intake.Id, intake.BatchCode, intake.IntakeDate, intake.Status, intake.RouteName,
            intake.IntakeBatchDonationRequests.Count(x => x.IsActive != false),
            intake.ClassifiedItems.Count,
            intake.ClassifiedItems.Where(x => x.ClassifiedBatch is not null)
                .GroupBy(x => x.ClassifiedBatchId!.Value)
                .Select(group =>
                {
                    var classified = group.First().ClassifiedBatch!;
                    inventoryByBatch.TryGetValue(classified.Id, out var inventory);
                    return new WarehouseClassifiedBatchTraceDto(classified.Id, classified.BatchCode,
                        classified.Status, classified.ClothingType, Grade(classified.ConditionRating),
                        classified.ProcessingDirection, classified.TotalItem, classified.TotalWeight,
                        inventory?.Sku, inventory?.StorageLocation?.LocationCode,
                        classified.DonationRequestSources.Where(x => x.IsActive != false)
                            .Select(x => x.DonationRequest.RequestCode).Distinct().OrderBy(x => x).ToList());
                }).OrderBy(x => x.BatchCode).ToList())).ToList();
    }

    public async Task<Guid> CreateAreaAsync(Guid userId, SaveWarehouseAreaDto dto)
    {
        await RequireManagerAsync(userId);
        ValidateNameAndCapacity(dto.AreaName, dto.CapacityKg, "Area");
        var warehouse = await context.Warehouses.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == dto.WarehouseId && x.IsActive != false)
            ?? throw new InvalidOperationException("Warehouse not found.");
        var allocatedAreaCapacity = await context.WarehouseAreas.AsNoTracking()
            .Where(x => x.WarehouseId == dto.WarehouseId && x.IsActive != false)
            .SumAsync(x => (decimal?)x.CapacityKg) ?? 0;
        if (allocatedAreaCapacity + dto.CapacityKg > warehouse.TotalCapacityKg)
            throw new InvalidOperationException(
                $"Area capacity exceeds the warehouse limit. Remaining capacity: "
                + $"{Math.Max(0, warehouse.TotalCapacityKg - allocatedAreaCapacity)} kg.");
        if (await context.WarehouseAreas.AnyAsync(x => x.WarehouseId == dto.WarehouseId
                && x.IsActive != false && x.AreaName == dto.AreaName.Trim()))
            throw new InvalidOperationException("An active area with this name already exists in the warehouse.");
        var area = new WarehouseArea
        {
            Id = Guid.NewGuid(), WarehouseId = dto.WarehouseId, AreaName = dto.AreaName.Trim(),
            Description = dto.Description?.Trim(), CapacityKg = dto.CapacityKg, CurrentKg = 0,
            CreateAt = DateTime.UtcNow, CreatedBy = userId, IsActive = true
        };
        context.WarehouseAreas.Add(area);
        await context.SaveChangesAsync();
        return area.Id;
    }

    public async Task UpdateAreaAsync(Guid userId, Guid areaId, SaveWarehouseAreaDto dto)
    {
        await RequireManagerAsync(userId);
        ValidateNameAndCapacity(dto.AreaName, dto.CapacityKg, "Area");
        var area = await context.WarehouseAreas.FirstOrDefaultAsync(x => x.Id == areaId && x.IsActive != false)
            ?? throw new InvalidOperationException("Warehouse area not found.");
        if (area.WarehouseId != dto.WarehouseId)
            throw new InvalidOperationException("An area cannot be moved to another warehouse.");
        var warehouseCapacity = await context.Warehouses.AsNoTracking()
            .Where(x => x.Id == area.WarehouseId)
            .Select(x => x.TotalCapacityKg)
            .SingleAsync();
        var currentAreaCapacity = await context.WarehouseAreas.AsNoTracking()
            .Where(x => x.WarehouseId == area.WarehouseId && x.IsActive != false)
            .SumAsync(x => (decimal?)x.CapacityKg) ?? 0;
        var otherAreaCapacity = currentAreaCapacity - area.CapacityKg;
        if (otherAreaCapacity + dto.CapacityKg > warehouseCapacity)
            throw new InvalidOperationException(
                $"Total area capacity cannot exceed the warehouse capacity of "
                + $"{warehouseCapacity} kg.");
        var allocatedCapacity = await context.AreaGroups
            .Where(x => x.AreaId == areaId && x.IsActive != false).SumAsync(x => (decimal?)x.CapacityKg) ?? 0;
        var inventoryWeight = await context.Inventories.AsNoTracking()
            .Where(x => x.IsActive != false && x.StorageLocation != null
                && x.StorageLocation.AreaId == areaId)
            .SumAsync(x => (decimal?)x.TotalWeight) ?? 0;
        var intakeBatchWeight = await context.IntakeBatches.AsNoTracking()
            .Where(x => x.IsActive != false && x.CurrentAreaId == areaId)
            .SumAsync(x => (decimal?)x.TotalWeight) ?? 0;
        var actualAreaWeight = inventoryWeight + intakeBatchWeight;
        if (dto.CapacityKg < actualAreaWeight)
            throw new InvalidOperationException(
                $"Area capacity cannot be lower than its current {actualAreaWeight} kg stock.");
        if (dto.CapacityKg < allocatedCapacity)
            throw new InvalidOperationException(
                $"Area capacity cannot be lower than {allocatedCapacity} kg already allocated to active rows.");
        if (await context.WarehouseAreas.AnyAsync(x => x.Id != areaId && x.WarehouseId == area.WarehouseId
                && x.IsActive != false && x.AreaName == dto.AreaName.Trim()))
            throw new InvalidOperationException("An active area with this name already exists in the warehouse.");
        area.AreaName = dto.AreaName.Trim();
        area.Description = dto.Description?.Trim();
        area.CapacityKg = dto.CapacityKg;
        area.UpdateAt = DateTime.UtcNow;
        area.UpdatedBy = userId;
        await context.SaveChangesAsync();
    }

    public async Task DeleteAreaAsync(Guid userId, Guid areaId)
    {
        await RequireManagerAsync(userId);
        var area = await context.WarehouseAreas.FirstOrDefaultAsync(x => x.Id == areaId && x.IsActive != false)
            ?? throw new InvalidOperationException("Warehouse area not found.");
        var hasStock = area.CurrentKg > 0 || await context.Inventories.AnyAsync(x => x.IsActive != false
            && (x.StorageLocation != null && x.StorageLocation.AreaId == areaId));
        var hasIntakeBatches = await context.IntakeBatches.AnyAsync(x =>
            x.IsActive != false && x.CurrentAreaId == areaId);
        if (hasStock || hasIntakeBatches)
            throw new InvalidOperationException("Move all inventory and intake batches from this area before deleting it.");
        var now = DateTime.UtcNow;
        var groups = await context.AreaGroups.Where(x => x.AreaId == areaId && x.IsActive != false).ToListAsync();
        var locations = await context.StorageLocations.Where(x => x.AreaId == areaId && x.IsActive != false).ToListAsync();
        foreach (var entity in groups.Cast<DAL.Models.Commons.BaseEntity>().Concat(locations))
        { entity.IsActive = false; entity.DeleteAt = now; entity.DeletedBy = userId; }
        area.IsActive = false; area.DeleteAt = now; area.DeletedBy = userId;
        await context.SaveChangesAsync();
    }

    public async Task<Guid> CreateGroupAsync(Guid userId, SaveWarehouseGroupDto dto)
    {
        await RequireManagerAsync(userId);
        ValidateNameAndCapacity(dto.GroupName, dto.CapacityKg, "Row");
        var area = await context.WarehouseAreas.FirstOrDefaultAsync(x => x.Id == dto.AreaId && x.IsActive != false)
            ?? throw new InvalidOperationException("Warehouse area not found.");
        var allocated = await context.AreaGroups.Where(x => x.AreaId == area.Id && x.IsActive != false)
            .SumAsync(x => (decimal?)x.CapacityKg) ?? 0;
        if (allocated + dto.CapacityKg > area.CapacityKg)
            throw new InvalidOperationException(
                $"Row capacity exceeds the area limit. Remaining capacity: {area.CapacityKg - allocated} kg.");
        if (await context.AreaGroups.AnyAsync(x => x.AreaId == area.Id && x.IsActive != false
                && x.GroupName == dto.GroupName.Trim()))
            throw new InvalidOperationException("An active row with this name already exists in the area.");
        var group = new AreaGroup
        {
            Id = Guid.NewGuid(), AreaId = area.Id, GroupName = dto.GroupName.Trim(),
            Description = dto.Description?.Trim(), CapacityKg = dto.CapacityKg, CurrentKg = 0,
            CreateAt = DateTime.UtcNow, CreatedBy = userId, IsActive = true
        };
        context.AreaGroups.Add(group);
        await context.SaveChangesAsync();
        return group.Id;
    }

    public async Task UpdateGroupAsync(Guid userId, Guid groupId, SaveWarehouseGroupDto dto)
    {
        await RequireManagerAsync(userId);
        ValidateNameAndCapacity(dto.GroupName, dto.CapacityKg, "Row");
        var group = await context.AreaGroups.Include(x => x.Area)
            .FirstOrDefaultAsync(x => x.Id == groupId && x.IsActive != false)
            ?? throw new InvalidOperationException("Warehouse row not found.");
        if (group.AreaId != dto.AreaId)
            throw new InvalidOperationException("A row cannot be moved to another area.");
        var inventoryWeight = await context.Inventories.AsNoTracking()
            .Where(x => x.AreaGroupId == group.Id && x.IsActive != false)
            .SumAsync(x => (decimal?)x.TotalWeight) ?? 0;
        var intakeBatchWeight = await context.IntakeBatches.AsNoTracking()
            .Where(x => x.CurrentAreaGroupId == group.Id && x.IsActive != false)
            .SumAsync(x => (decimal?)x.TotalWeight) ?? 0;
        var actualGroupWeight = inventoryWeight + intakeBatchWeight;
        if (dto.CapacityKg < actualGroupWeight)
            throw new InvalidOperationException(
                $"Row capacity cannot be lower than its current {actualGroupWeight} kg stock.");
        var allocatedLocationCapacity = await context.StorageLocations.AsNoTracking()
            .Where(x => x.AreaGroupId == group.Id && x.IsActive != false)
            .SumAsync(x => (decimal?)x.CapacityKg) ?? 0;
        if (dto.CapacityKg < allocatedLocationCapacity)
            throw new InvalidOperationException(
                $"Row capacity cannot be lower than {allocatedLocationCapacity} kg "
                + "already allocated to active locations.");
        var otherCapacity = await context.AreaGroups.Where(x => x.AreaId == group.AreaId
                && x.Id != group.Id && x.IsActive != false).SumAsync(x => (decimal?)x.CapacityKg) ?? 0;
        if (otherCapacity + dto.CapacityKg > group.Area.CapacityKg)
            throw new InvalidOperationException(
                $"Total row capacity cannot exceed the area capacity of {group.Area.CapacityKg} kg.");
        if (await context.AreaGroups.AnyAsync(x => x.Id != groupId && x.AreaId == group.AreaId
                && x.IsActive != false && x.GroupName == dto.GroupName.Trim()))
            throw new InvalidOperationException("An active row with this name already exists in the area.");
        group.GroupName = dto.GroupName.Trim();
        group.Description = dto.Description?.Trim();
        group.CapacityKg = dto.CapacityKg;
        group.UpdateAt = DateTime.UtcNow;
        group.UpdatedBy = userId;
        await context.SaveChangesAsync();
    }

    public async Task DeleteGroupAsync(Guid userId, Guid groupId)
    {
        await RequireManagerAsync(userId);
        var group = await context.AreaGroups.FirstOrDefaultAsync(x => x.Id == groupId && x.IsActive != false)
            ?? throw new InvalidOperationException("Warehouse row not found.");
        var hasStock = group.CurrentKg > 0 || await context.Inventories.AnyAsync(x =>
            x.AreaGroupId == groupId && x.IsActive != false && x.Quantity > 0);
        var hasIntakeBatches = await context.IntakeBatches.AnyAsync(x =>
            x.IsActive != false && x.CurrentAreaGroupId == groupId);
        if (hasStock || hasIntakeBatches)
            throw new InvalidOperationException("Move all inventory and intake batches from this row before deleting it.");
        var now = DateTime.UtcNow;
        var locations = await context.StorageLocations
            .Where(x => x.AreaGroupId == groupId && x.IsActive != false).ToListAsync();
        foreach (var location in locations)
        { location.IsActive = false; location.DeleteAt = now; location.DeletedBy = userId; }
        group.IsActive = false; group.DeleteAt = now; group.DeletedBy = userId;
        await context.SaveChangesAsync();
    }

    public async Task<Guid> CreateLocationAsync(Guid userId, SaveStorageLocationDto dto)
    {
        await RequireManagerAsync(userId);
        ValidateLocation(dto);
        var group = await context.AreaGroups.Include(x => x.Area)
            .FirstOrDefaultAsync(x => x.Id == dto.AreaGroupId && x.IsActive != false)
            ?? throw new InvalidOperationException("Warehouse row not found.");
        await ValidateLocationCapacityAsync(group, null, dto.CapacityKg);
        if (await context.StorageLocations.AnyAsync(x => x.WarehouseId == group.Area.WarehouseId
                && x.IsActive != false && x.LocationCode == dto.LocationCode.Trim()))
            throw new InvalidOperationException("An active location with this code already exists in the warehouse.");
        var location = new StorageLocation
        {
            Id = Guid.NewGuid(), WarehouseId = group.Area.WarehouseId, AreaId = group.AreaId,
            AreaGroupId = group.Id, LocationCode = dto.LocationCode.Trim().ToUpperInvariant(),
            AisleCode = dto.AisleCode.Trim().ToUpperInvariant(),
            RackCode = dto.RackCode.Trim().ToUpperInvariant(),
            ShelfCode = dto.ShelfCode.Trim().ToUpperInvariant(),
            BinCode = dto.BinCode.Trim().ToUpperInvariant(),
            PreferredGarmentGroup = dto.PreferredGarmentGroup?.Trim(),
            PreferredProcessingDirection = dto.PreferredProcessingDirection?.Trim(),
            CapacityKg = dto.CapacityKg, CurrentWeightKg = 0, Status = dto.Status,
            CreateAt = DateTime.UtcNow, CreatedBy = userId, IsActive = true
        };
        context.StorageLocations.Add(location);
        await context.SaveChangesAsync();
        return location.Id;
    }

    public async Task UpdateLocationAsync(Guid userId, Guid locationId, SaveStorageLocationDto dto)
    {
        await RequireManagerAsync(userId);
        ValidateLocation(dto);
        var location = await context.StorageLocations.Include(x => x.AreaGroup)!.ThenInclude(x => x!.Area)
            .FirstOrDefaultAsync(x => x.Id == locationId && x.IsActive != false)
            ?? throw new InvalidOperationException("Storage location not found.");
        if (location.AreaGroupId != dto.AreaGroupId)
            throw new InvalidOperationException("A location cannot be moved to another warehouse row.");
        var inventoryWeight = await context.Inventories.AsNoTracking()
            .Where(x => x.StorageLocationId == location.Id && x.IsActive != false)
            .SumAsync(x => (decimal?)x.TotalWeight) ?? 0;
        var intakeBatchWeight = await context.IntakeBatches.AsNoTracking()
            .Where(x => x.CurrentStorageLocationId == location.Id && x.IsActive != false)
            .SumAsync(x => (decimal?)x.TotalWeight) ?? 0;
        var actualLocationWeight = inventoryWeight + intakeBatchWeight;
        if (dto.CapacityKg < actualLocationWeight)
            throw new InvalidOperationException(
                $"Location capacity cannot be lower than its current {actualLocationWeight} kg stock.");
        await ValidateLocationCapacityAsync(location.AreaGroup!, location.Id, dto.CapacityKg);
        if (await context.StorageLocations.AnyAsync(x => x.Id != locationId
                && x.WarehouseId == location.WarehouseId && x.IsActive != false
                && x.LocationCode == dto.LocationCode.Trim()))
            throw new InvalidOperationException("An active location with this code already exists in the warehouse.");
        location.LocationCode = dto.LocationCode.Trim().ToUpperInvariant();
        location.AisleCode = dto.AisleCode.Trim().ToUpperInvariant();
        location.RackCode = dto.RackCode.Trim().ToUpperInvariant();
        location.ShelfCode = dto.ShelfCode.Trim().ToUpperInvariant();
        location.BinCode = dto.BinCode.Trim().ToUpperInvariant();
        location.PreferredGarmentGroup = dto.PreferredGarmentGroup?.Trim();
        location.PreferredProcessingDirection = dto.PreferredProcessingDirection?.Trim();
        location.CapacityKg = dto.CapacityKg;
        location.Status = dto.Status;
        location.UpdateAt = DateTime.UtcNow;
        location.UpdatedBy = userId;
        await context.SaveChangesAsync();
    }

    public async Task DeleteLocationAsync(Guid userId, Guid locationId)
    {
        await RequireManagerAsync(userId);
        var location = await context.StorageLocations
            .FirstOrDefaultAsync(x => x.Id == locationId && x.IsActive != false)
            ?? throw new InvalidOperationException("Storage location not found.");
        var hasInventory = location.CurrentWeightKg > 0 || await context.Inventories.AnyAsync(x =>
            x.StorageLocationId == locationId && x.IsActive != false && x.Quantity > 0);
        var hasIntakeBatches = await context.IntakeBatches.AnyAsync(x =>
            x.IsActive != false && x.CurrentStorageLocationId == locationId);
        if (hasInventory || hasIntakeBatches)
            throw new InvalidOperationException("Move all inventory and intake batches from this location before deleting it.");
        location.IsActive = false;
        location.DeleteAt = DateTime.UtcNow;
        location.DeletedBy = userId;
        await context.SaveChangesAsync();
    }

    public async Task ConfirmReceiptAsync(Guid staffId, Guid batchId, ConfirmWarehouseReceiptDto dto)
    {
        if (dto.ActualItemCount <= 0 || dto.ActualWeightKg <= 0)
            throw new InvalidOperationException("Actual item count and weight must be greater than zero.");
        if (!dto.SealIntact && string.IsNullOrWhiteSpace(dto.DiscrepancyNotes))
            throw new InvalidOperationException("A discrepancy note is required when the seal is not intact.");

        await using var transaction = await context.Database.BeginTransactionAsync();
        var batch = await context.ClassifiedBatches.Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == batchId && x.IsActive != false)
            ?? throw new InvalidOperationException("Classified batch not found.");
        if (batch.Status != "PendingWarehouseReceipt")
            throw new InvalidOperationException("Only a batch pending warehouse receipt can be confirmed.");

        var expectedCount = batch.Items.Count(x => x.IsActive != false);
        var notes = BuildReceiptNotes(expectedCount, dto);
        batch.Status = "WarehouseReceived";
        batch.WarehouseReceivedAt = DateTime.UtcNow;
        batch.WarehouseReceivedByStaffId = staffId;
        batch.ReceivedItemCount = dto.ActualItemCount;
        batch.ReceivedWeight = dto.ActualWeightKg;
        batch.WarehouseReceiptNotes = notes;
        batch.TotalItem = dto.ActualItemCount;
        batch.TotalWeight = dto.ActualWeightKg;
        batch.UpdateAt = DateTime.UtcNow;
        batch.UpdatedBy = staffId;

        var inventory = new Inventory
        {
            Id = Guid.NewGuid(), WarehouseId = batch.WarehouseId, ClassifiedBatchId = batch.Id,
            FabricTypeId = batch.FabricTypeId, GarmentGroupId = batch.GarmentGroupId,
            ClothingTypeId = batch.ClothingTypeId, GenderId = batch.GenderId,
            TargetUserId = batch.TargetUserId, SizeId = batch.SizeId,
            ConditionGradeId = batch.ConditionGradeId,
            Sku = $"SKU-{batch.BatchCode}", FabricType = batch.FabricType,
            GarmentGroup = batch.GarmentGroup, ClothingType = batch.ClothingType,
            Gender = batch.Gender, TargetUser = batch.TargetUser, Size = batch.Size,
            ProcessingDirection = batch.ProcessingDirection, ConditionRating = batch.ConditionRating,
            Quantity = dto.ActualItemCount, TotalWeight = dto.ActualWeightKg,
            Status = "AwaitingPutaway", CreateAt = DateTime.UtcNow, CreatedBy = staffId
        };
        context.Inventories.Add(inventory);
        AddTransaction(staffId, batch.WarehouseId, "RECEIPT", "ClassifiedBatch", batch.Id,
            notes, inventory, dto.ActualItemCount, dto.ActualWeightKg, 0, dto.ActualItemCount,
            0, dto.ActualWeightKg, null, null);
        var sourceIds = await context.ClassifiedBatchDonationRequests.Where(x => x.ClassifiedBatchId == batch.Id)
            .Select(x => x.DonationRequestId).ToListAsync();
        var actor = await NotificationWriter.ActorNameAsync(context, staffId);
        await NotificationWriter.NotifyDonorsAsync(context, sourceIds, "WarehouseReceived", "Kho đã nhận hàng",
            _ => $"batch {batch.BatchCode} được {actor} xác nhận nhập kho lúc {NotificationWriter.FormatTime(DateTime.UtcNow)}.", staffId);
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    public async Task<IReadOnlyList<StorageLocationDto>> GetLocationsAsync(Guid batchId)
    {
        var batch = await context.ClassifiedBatches.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == batchId && x.IsActive != false)
            ?? throw new InvalidOperationException("Classified batch not found.");
        await EnsureDefaultLayoutAsync(batch.WarehouseId);
        var requiredDirection = ProcessingDirectionForGrade(batch.ConditionRating);
        var requiredCapacity = batch.ReceivedWeight ?? batch.TotalWeight;
        var locations = await context.StorageLocations.AsNoTracking()
            .Include(x => x.Area)
            .Where(x => x.WarehouseId == batch.WarehouseId
                && x.IsActive != false
                && x.Status != "Blocked"
                && x.PreferredProcessingDirection == requiredDirection
                && x.CapacityKg - x.CurrentWeightKg >= requiredCapacity)
            .ToListAsync();
        return locations.Select(x => MapLocation(x, batch)).OrderByDescending(x => x.MatchScore)
            .ThenBy(x => x.LocationCode).ToList();
    }

    public async Task PutawayAsync(Guid staffId, Guid batchId, PutawayBatchDto dto)
    {
        await using var transaction = await context.Database.BeginTransactionAsync();
        var batch = await context.ClassifiedBatches
            .FirstOrDefaultAsync(x => x.Id == batchId && x.IsActive != false)
            ?? throw new InvalidOperationException("Classified batch not found.");
        if (batch.Status != "WarehouseReceived")
            throw new InvalidOperationException("Confirm physical receipt before putaway.");
        var inventory = await context.Inventories
            .FirstOrDefaultAsync(x => x.ClassifiedBatchId == batchId && x.IsActive != false)
            ?? throw new InvalidOperationException("Receiving inventory record not found.");
        var location = await context.StorageLocations
            .Include(x => x.Area).Include(x => x.Warehouse)
            .FirstOrDefaultAsync(x => x.Id == dto.LocationId && x.WarehouseId == batch.WarehouseId && x.IsActive != false)
            ?? throw new InvalidOperationException("Storage location not found.");
        if (location.Status == "Blocked")
            throw new InvalidOperationException("Storage location is blocked.");
        var requiredDirection = ProcessingDirectionForGrade(batch.ConditionRating);
        if (location.PreferredProcessingDirection != requiredDirection)
            throw new InvalidOperationException(
                $"Grade {Grade(batch.ConditionRating)} inventory must be stored in the {requiredDirection} area.");
        if (location.CapacityKg - location.CurrentWeightKg < inventory.TotalWeight)
            throw new InvalidOperationException("Storage location does not have enough remaining capacity.");

        inventory.StorageLocationId = location.Id;
        inventory.AreaGroupId = location.AreaGroupId;
        inventory.Status = "Available";
        inventory.UpdateAt = DateTime.UtcNow;
        inventory.UpdatedBy = staffId;
        location.CurrentWeightKg += inventory.TotalWeight;
        location.Area.CurrentKg += inventory.TotalWeight;
        location.Warehouse.CurrentWeight += inventory.TotalWeight;
        batch.AreaId = location.AreaId;
        batch.GroupId = location.AreaGroupId;
        batch.Status = "Stored";
        batch.StoredAt = DateTime.UtcNow;
        batch.StoredByStaffId = staffId;
        batch.UpdateAt = DateTime.UtcNow;
        batch.UpdatedBy = staffId;

        AddTransaction(staffId, batch.WarehouseId, "PUTAWAY", "ClassifiedBatch", batch.Id,
            dto.Notes, inventory, inventory.Quantity, inventory.TotalWeight,
            inventory.Quantity, inventory.Quantity, inventory.TotalWeight, inventory.TotalWeight,
            null, location.Id);
        var sourceIds = await context.ClassifiedBatchDonationRequests.Where(x => x.ClassifiedBatchId == batch.Id)
            .Select(x => x.DonationRequestId).ToListAsync();
        await NotificationWriter.NotifyDonorsAsync(context, sourceIds, "DonationStored", "Đã lưu trữ trong kho",
            _ => $"batch {batch.BatchCode} được lưu tại {location.LocationCode} lúc {NotificationWriter.FormatTime(DateTime.UtcNow)}.", staffId);
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    public async Task<IReadOnlyList<WarehouseInventoryDto>> GetInventoryAsync(
        Guid userId, Guid? requestedWarehouseId, string? search)
    {
        var warehouseId = await ResolveWarehouseIdAsync(userId, requestedWarehouseId);
        var query = context.Inventories.AsNoTracking()
            .Include(x => x.ClassifiedBatch).Include(x => x.StorageLocation)!.ThenInclude(x => x!.Area)
            .Where(x => x.WarehouseId == warehouseId && x.IsActive != false);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => x.Sku.Contains(term) || x.ClothingType.Contains(term)
                || (x.StorageLocation != null && x.StorageLocation.LocationCode.Contains(term))
                || (x.ClassifiedBatch != null && x.ClassifiedBatch.DonationRequestSources
                    .Any(source => source.DonationRequest.RequestCode.Contains(term))));
        }
        return await query.OrderBy(x => x.StorageLocation!.LocationCode).ThenBy(x => x.Sku)
            .Select(x => new WarehouseInventoryDto(x.Id, x.Sku, x.ClassifiedBatchId!.Value,
                x.ClassifiedBatch!.BatchCode, x.StorageLocation != null ? x.StorageLocation.LocationCode : "RECEIVING",
                x.StorageLocation != null ? x.StorageLocation.Area.AreaName : "Khu tiếp nhận",
                x.FabricType, x.GarmentGroup, x.ClothingType, x.Gender, x.TargetUser, x.Size,
                Grade(x.ConditionRating), x.ProcessingDirection, x.Quantity, x.ReservedQuantity,
                x.Quantity - x.ReservedQuantity, x.TotalWeight, x.ReservedWeight,
                 x.TotalWeight - x.ReservedWeight, x.Status, x.ClassifiedBatch.StoredAt,
                 x.ClassifiedBatch.DonationRequestSources.Where(source => source.IsActive != false)
                    .Select(source => source.DonationRequest.RequestCode).Distinct().OrderBy(code => code).ToList()))
            .ToListAsync();
    }

    public async Task<IReadOnlyList<WarehouseInventoryDto>> GetLocationInventoryAsync(
        Guid userId, Guid locationId)
    {
        var location = await context.StorageLocations.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == locationId && x.IsActive != false)
            ?? throw new InvalidOperationException("Storage location not found.");
        var accessibleWarehouseId = await ResolveWarehouseIdAsync(userId, location.WarehouseId);
        if (accessibleWarehouseId != location.WarehouseId)
            throw new UnauthorizedAccessException("This storage location belongs to another warehouse.");

        return await context.Inventories.AsNoTracking()
            .Include(x => x.ClassifiedBatch)
            .Include(x => x.StorageLocation)!.ThenInclude(x => x!.Area)
            .Where(x => x.StorageLocationId == locationId && x.IsActive != false)
            .OrderBy(x => x.Sku)
            .Select(x => new WarehouseInventoryDto(x.Id, x.Sku, x.ClassifiedBatchId!.Value,
                x.ClassifiedBatch!.BatchCode, x.StorageLocation!.LocationCode,
                x.StorageLocation.Area.AreaName, x.FabricType, x.GarmentGroup, x.ClothingType,
                x.Gender, x.TargetUser, x.Size, Grade(x.ConditionRating), x.ProcessingDirection,
                x.Quantity, x.ReservedQuantity, x.Quantity - x.ReservedQuantity,
                x.TotalWeight, x.ReservedWeight, x.TotalWeight - x.ReservedWeight,
                x.Status, x.ClassifiedBatch.StoredAt,
                x.ClassifiedBatch.DonationRequestSources.Where(source => source.IsActive != false)
                    .Select(source => source.DonationRequest.RequestCode).Distinct().OrderBy(code => code).ToList()))
            .ToListAsync();
    }

    public async Task<IReadOnlyList<WarehouseTransactionDto>> GetTransactionsAsync(
        Guid userId, Guid? requestedWarehouseId, string? type)
    {
        var warehouseId = await ResolveWarehouseIdAsync(userId, requestedWarehouseId);
        var query = context.InventoryTransactions.AsNoTracking()
            .Include(x => x.PerformedByStaff).Include(x => x.Items).ThenInclude(x => x.Inventory)
            .Include(x => x.Items).ThenInclude(x => x.ClassifiedBatch)
                .ThenInclude(x => x!.DonationRequestSources)
                    .ThenInclude(x => x.DonationRequest)
            .Include(x => x.Items).ThenInclude(x => x.SourceLocation)
            .Include(x => x.Items).ThenInclude(x => x.DestinationLocation)
            .Where(x => x.WarehouseId == warehouseId && x.IsActive != false);
        if (!string.IsNullOrWhiteSpace(type)) query = query.Where(x => x.TransactionType == type);
        var transactions = await query.OrderByDescending(x => x.PerformedAt).Take(200).ToListAsync();
        return transactions.Select(x => new WarehouseTransactionDto(x.Id, x.TransactionCode,
            x.TransactionType, x.ReferenceType, x.ReferenceId, x.Status, x.Notes, x.PerformedAt,
            x.PerformedByStaff.FullName, x.Items.Select(i => new WarehouseTransactionItemDto(i.Id,
                i.InventoryId, i.Inventory.Sku, i.ClassifiedBatch?.BatchCode, i.Quantity, i.Weight,
                i.QuantityBefore, i.QuantityAfter, i.WeightBefore, i.WeightAfter,
                i.SourceLocation?.LocationCode, i.DestinationLocation?.LocationCode, i.Notes,
                i.ClassifiedBatch?.DonationRequestSources.Where(source => source.IsActive != false)
                    .Select(source => source.DonationRequest.RequestCode).Distinct().OrderBy(code => code).ToList()
                    ?? [])).ToList())).ToList();
    }

    public async Task IssueAsync(Guid staffId, Guid inventoryId, IssueInventoryDto dto)
    {
        if (dto.Quantity <= 0 || dto.WeightKg <= 0 || string.IsNullOrWhiteSpace(dto.Reason))
            throw new InvalidOperationException("Quantity, weight and issue reason are required.");
        await using var transaction = await context.Database.BeginTransactionAsync();
        var inventory = await InventoryForMutation(inventoryId);
        if (inventory.Status != "Available") throw new InvalidOperationException("Inventory is not available for issue.");
        if (inventory.Quantity - inventory.ReservedQuantity < dto.Quantity
            || inventory.TotalWeight - inventory.ReservedWeight < dto.WeightKg)
            throw new InvalidOperationException("Requested issue exceeds available inventory.");
        var beforeQuantity = inventory.Quantity;
        var beforeWeight = inventory.TotalWeight;
        inventory.Quantity -= dto.Quantity;
        inventory.TotalWeight -= dto.WeightKg;
        inventory.Status = inventory.Quantity == 0 ? "Depleted" : "Available";
        inventory.UpdateAt = DateTime.UtcNow;
        inventory.UpdatedBy = staffId;
        AdjustLocationWeight(inventory, -dto.WeightKg);
        AddTransaction(staffId, inventory.WarehouseId, "OUT", dto.ReferenceType, dto.ReferenceId,
            $"{dto.Reason}. {dto.Notes}".Trim(), inventory, dto.Quantity, dto.WeightKg,
            beforeQuantity, inventory.Quantity, beforeWeight, inventory.TotalWeight,
            inventory.StorageLocationId, null);
        var sourceIds = await context.ClassifiedBatchDonationRequests
            .Where(x => x.ClassifiedBatchId == inventory.ClassifiedBatchId)
            .Select(x => x.DonationRequestId).ToListAsync();
        await NotificationWriter.NotifyDonorsAsync(context, sourceIds, "DonationDistributed", "Đã xuất kho để phân phối",
            _ => $"hàng được xuất kho lúc {NotificationWriter.FormatTime(DateTime.UtcNow)}. Mục đích: {dto.Reason}.", staffId);
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    public async Task MoveAsync(Guid staffId, Guid inventoryId, MoveInventoryDto dto)
    {
        await using var transaction = await context.Database.BeginTransactionAsync();
        var inventory = await InventoryForMutation(inventoryId);
        if (!inventory.StorageLocationId.HasValue) throw new InvalidOperationException("Inventory has not been put away.");
        if (inventory.StorageLocationId == dto.DestinationLocationId) throw new InvalidOperationException("Destination must differ from source.");
        var destination = await context.StorageLocations.Include(x => x.Area).Include(x => x.Warehouse)
            .FirstOrDefaultAsync(x => x.Id == dto.DestinationLocationId && x.WarehouseId == inventory.WarehouseId && x.IsActive != false)
            ?? throw new InvalidOperationException("Destination location not found.");
        var requiredDirection = ProcessingDirectionForGrade(inventory.ConditionRating);
        if (destination.PreferredProcessingDirection != requiredDirection)
            throw new InvalidOperationException(
                $"Grade {Grade(inventory.ConditionRating)} inventory can only be moved within the {requiredDirection} area.");
        if (destination.CapacityKg - destination.CurrentWeightKg < inventory.TotalWeight)
            throw new InvalidOperationException("Destination location does not have enough capacity.");
        var sourceId = inventory.StorageLocationId;
        AdjustLocationWeight(inventory, -inventory.TotalWeight);
        destination.CurrentWeightKg += inventory.TotalWeight;
        destination.Area.CurrentKg += inventory.TotalWeight;
        destination.Warehouse.CurrentWeight += inventory.TotalWeight;
        inventory.StorageLocationId = destination.Id;
        inventory.AreaGroupId = destination.AreaGroupId;
        inventory.UpdateAt = DateTime.UtcNow;
        inventory.UpdatedBy = staffId;
        AddTransaction(staffId, inventory.WarehouseId, "MOVE", "Inventory", inventory.Id,
            $"{dto.Reason}. {dto.Notes}".Trim(), inventory, inventory.Quantity, inventory.TotalWeight,
            inventory.Quantity, inventory.Quantity, inventory.TotalWeight, inventory.TotalWeight,
            sourceId, destination.Id);
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    private IQueryable<ClassifiedBatch> BatchQuery() => context.ClassifiedBatches.AsNoTracking()
        .Include(x => x.Items.Where(i => i.IsActive != false))
        .Include(x => x.DonationRequestSources.Where(source => source.IsActive != false))
            .ThenInclude(x => x.DonationRequest)
        .Where(x => x.IsActive != false);

    private async Task<Guid> ResolveWarehouseIdAsync(Guid userId, Guid? requestedWarehouseId)
    {
        var user = await context.Users.AsNoTracking().Include(x => x.Role)
            .FirstOrDefaultAsync(x => x.Id == userId && x.IsActive != false)
            ?? throw new InvalidOperationException("User not found.");
        if (user.Role.RoleName == "Manager")
        {
            if (requestedWarehouseId.HasValue && await context.Warehouses
                    .AnyAsync(x => x.Id == requestedWarehouseId && x.IsActive != false))
                return requestedWarehouseId.Value;
            return await context.Warehouses.Where(x => x.IsActive != false)
                .OrderBy(x => x.WarehouseName).Select(x => x.Id).FirstOrDefaultAsync();
        }
        if (!user.WarehouseId.HasValue)
            throw new InvalidOperationException("No warehouse is assigned to this staff account.");
        if (requestedWarehouseId.HasValue && requestedWarehouseId != user.WarehouseId)
            throw new UnauthorizedAccessException("Warehouse staff can only access their assigned warehouse.");
        return user.WarehouseId.Value;
    }

    private async Task RequireManagerAsync(Guid userId)
    {
        if (!await context.Users.AsNoTracking().AnyAsync(x => x.Id == userId && x.IsActive != false
                && x.Role.RoleName == "Manager"))
            throw new UnauthorizedAccessException("Only managers can change the warehouse layout.");
    }

    private static void ValidateNameAndCapacity(string name, decimal capacityKg, string entity)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException($"{entity} name is required.");
        if (capacityKg <= 0)
            throw new InvalidOperationException($"{entity} capacity must be greater than zero.");
    }

    private static void ValidateLocation(SaveStorageLocationDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.LocationCode) || string.IsNullOrWhiteSpace(dto.AisleCode)
            || string.IsNullOrWhiteSpace(dto.RackCode) || string.IsNullOrWhiteSpace(dto.ShelfCode)
            || string.IsNullOrWhiteSpace(dto.BinCode))
            throw new InvalidOperationException("Location, aisle, rack, shelf and bin codes are required.");
        if (dto.CapacityKg <= 0)
            throw new InvalidOperationException("Location capacity must be greater than zero.");
        if (dto.Status is not ("Available" or "Blocked" or "Maintenance"))
            throw new InvalidOperationException("Location status must be Available, Blocked or Maintenance.");
    }

    private async Task ValidateLocationCapacityAsync(AreaGroup group, Guid? excludingLocationId,
        decimal requestedCapacity)
    {
        var allocated = await context.StorageLocations.Where(x => x.AreaGroupId == group.Id
                && x.IsActive != false && (!excludingLocationId.HasValue || x.Id != excludingLocationId))
            .SumAsync(x => (decimal?)x.CapacityKg) ?? 0;
        if (allocated + requestedCapacity > group.CapacityKg)
            throw new InvalidOperationException(
                $"Location capacity exceeds the row limit. Remaining capacity: {group.CapacityKg - allocated} kg.");
    }

    private async Task<Inventory> InventoryForMutation(Guid id) => await context.Inventories
        .Include(x => x.StorageLocation)!.ThenInclude(x => x!.Area)
        .Include(x => x.StorageLocation)!.ThenInclude(x => x!.Warehouse)
        .FirstOrDefaultAsync(x => x.Id == id && x.IsActive != false)
        ?? throw new InvalidOperationException("Inventory not found.");

    private void AdjustLocationWeight(Inventory inventory, decimal delta)
    {
        if (inventory.StorageLocation is null) return;
        inventory.StorageLocation.CurrentWeightKg += delta;
        inventory.StorageLocation.Area.CurrentKg += delta;
        inventory.StorageLocation.Warehouse.CurrentWeight += delta;
    }

    private void AddTransaction(Guid staffId, Guid warehouseId, string type, string? referenceType,
        Guid? referenceId, string? notes, Inventory inventory, int quantity, decimal weight,
        int quantityBefore, int quantityAfter, decimal weightBefore, decimal weightAfter,
        Guid? sourceLocationId, Guid? destinationLocationId)
    {
        var now = DateTime.UtcNow;
        context.InventoryTransactions.Add(new InventoryTransaction
        {
            Id = Guid.NewGuid(), WarehouseId = warehouseId,
            TransactionCode = $"TX-{type}-{now:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..32].ToUpperInvariant(),
            TransactionType = type, ReferenceType = referenceType, ReferenceId = referenceId,
            Status = "Posted", Notes = notes, PerformedByStaffId = staffId, PerformedAt = now,
            CreateAt = now, CreatedBy = staffId,
            Items =
            [
                new TransactionItem
                {
                    Id = Guid.NewGuid(), InventoryId = inventory.Id,
                    ClassifiedBatchId = inventory.ClassifiedBatchId, Quantity = quantity, Weight = weight,
                    QuantityBefore = quantityBefore, QuantityAfter = quantityAfter,
                    WeightBefore = weightBefore, WeightAfter = weightAfter,
                    SourceLocationId = sourceLocationId, DestinationLocationId = destinationLocationId,
                    Notes = notes, CreateAt = now, CreatedBy = staffId
                }
            ]
        });
    }

    private async Task EnsureDefaultLayoutAsync(Guid warehouseId)
    {
        var warehouseCapacity = await context.Warehouses.Where(x => x.Id == warehouseId)
            .Select(x => x.TotalCapacityKg).SingleAsync();
        // Six default areas share the physical warehouse capacity: three staging areas
        // and three classified-storage areas. Never assign the full warehouse capacity
        // to every staging area, otherwise the hierarchy is overallocated at creation.
        var defaultAreaCapacity = warehouseCapacity / 6m;
        var stagingDefinitions = new[]
        {
            ("Receiving", "Khu nhận đồ", "Khu tiếp nhận Intake Batch do Receiving Staff đưa về."),
            ("Unclassified", "Khu chưa phân loại", "Khu Intake Batch chờ Classification Staff xử lý."),
            ("Classified", "Khu đã phân loại", "Khu Intake Batch đã hoàn tất phân loại.")
        };
        foreach (var (type, name, description) in stagingDefinitions)
        {
            var area = await context.WarehouseAreas.Include(x => x.Groups)
                .FirstOrDefaultAsync(x => x.WarehouseId == warehouseId
                    && x.AreaType == type && x.IsActive != false);
            if (area is null)
            {
                // Respect an intentional soft delete. This bootstrap runs on every layout load,
                // so checking active rows only would resurrect a manager-deleted area.
                var existsInHistory = await context.WarehouseAreas.AnyAsync(x =>
                    x.WarehouseId == warehouseId && x.AreaType == type);
                if (existsInHistory) continue;

                area = new WarehouseArea
                {
                    Id = Guid.NewGuid(), WarehouseId = warehouseId, AreaType = type,
                    AreaName = name, Description = description, CapacityKg = defaultAreaCapacity,
                    CurrentKg = 0, CreateAt = VietnamTime.Now, IsActive = true
                };
                context.WarehouseAreas.Add(area);
            }
            var prefix = type switch
            {
                "Receiving" => "RECEIVING",
                "Unclassified" => "UNCLASSIFIED",
                _ => "CLASSIFIED"
            };
            for (var index = 1; index <= 2; index++)
                // Include inactive rows so a deleted default row stays deleted after reload.
                if (!area.Groups.Any(x => x.GroupName == $"Dãy {prefix}-{index:00}"))
                    context.AreaGroups.Add(new AreaGroup
                    {
                        Id = Guid.NewGuid(), AreaId = area.Id,
                        GroupName = $"Dãy {prefix}-{index:00}",
                        Description = $"Dãy trung chuyển {name.ToLowerInvariant()} số {index:00}",
                        CapacityKg = defaultAreaCapacity / 2m, CurrentKg = 0,
                        CreateAt = VietnamTime.Now, IsActive = true
                    });
        }

        await context.SaveChangesAsync();

        var stagingGroups = await context.AreaGroups
            .Include(x => x.Area)
            .Include(x => x.StorageLocations)
            .Where(x => x.Area.WarehouseId == warehouseId
                && x.Area.AreaType == "Receiving"
                && x.IsActive != false)
            .ToListAsync();
        foreach (var group in stagingGroups)
        {
            for (var shelf = 1; shelf <= 3; shelf++)
            {
                var shelfCode = $"S{shelf:00}";
                // Include inactive locations for the same reason as default rows above.
                if (group.StorageLocations.Any(x => x.ShelfCode == shelfCode))
                    continue;
                context.StorageLocations.Add(new StorageLocation
                {
                    Id = Guid.NewGuid(), WarehouseId = warehouseId, AreaId = group.AreaId,
                    AreaGroupId = group.Id,
                    LocationCode = $"RECEIVING-{group.Id:N}"[..18].ToUpperInvariant()
                        + $"-R01-{shelfCode}-B01",
                    AisleCode = "A01", RackCode = "R01", ShelfCode = shelfCode, BinCode = "B01",
                    PreferredProcessingDirection = "ReceivingStaging",
                    CapacityKg = group.CapacityKg / 3m, CurrentWeightKg = 0,
                    Status = "Available", CreateAt = VietnamTime.Now, IsActive = true
                });
            }
        }
        await context.SaveChangesAsync();

        if (await context.StorageLocations.AnyAsync(x => x.WarehouseId == warehouseId
            && x.Area.AreaType == "Storage" && x.IsActive != false))
        {
            return;
        }
        var areaCapacity = defaultAreaCapacity;
        var locationCapacity = areaCapacity / 6m;
        var definitions = new[]
        {
            ("CHARITY", "Khu hàng từ thiện", "Charity"),
            ("RECYCLE", "Khu hàng tái chế", "Recycling"),
            ("DISPOSAL", "Khu cách ly/tiêu hủy", "Disposal")
        };
        foreach (var (areaCode, areaName, direction) in definitions)
        {
            var area = new WarehouseArea
            {
                Id = Guid.NewGuid(), WarehouseId = warehouseId, AreaName = areaName,
                AreaType = "Storage",
                Description = $"Khu vực kiểm soát cho hướng xử lý {direction}", CapacityKg = areaCapacity,
                CurrentKg = 0, CreateAt = DateTime.UtcNow, IsActive = true
            };
            var group = new AreaGroup
            {
                Id = Guid.NewGuid(), AreaId = area.Id, GroupName = $"Dãy {areaCode}-A",
                Description = "Dãy lưu trữ tiêu chuẩn", CapacityKg = areaCapacity, CurrentKg = 0,
                CreateAt = DateTime.UtcNow, IsActive = true
            };
            context.WarehouseAreas.Add(area);
            context.AreaGroups.Add(group);
            for (var rack = 1; rack <= 2; rack++)
            for (var shelf = 1; shelf <= 3; shelf++)
            {
                var code = $"{areaCode}-A01-R{rack:00}-S{shelf:00}-B01";
                context.StorageLocations.Add(new StorageLocation
                {
                    Id = Guid.NewGuid(), WarehouseId = warehouseId, AreaId = area.Id,
                    AreaGroupId = group.Id, LocationCode = code, AisleCode = "A01",
                    RackCode = $"R{rack:00}", ShelfCode = $"S{shelf:00}", BinCode = "B01",
                    PreferredProcessingDirection = direction, CapacityKg = locationCapacity,
                    Status = "Available", CreateAt = DateTime.UtcNow, IsActive = true
                });
            }
        }
        await context.SaveChangesAsync();
    }

    private static StorageLocationDto MapLocation(StorageLocation x, ClassifiedBatch batch)
    {
        var score = 0;
        if (x.PreferredProcessingDirection == batch.ProcessingDirection) score += 70;
        if (string.IsNullOrWhiteSpace(x.PreferredGarmentGroup) || x.PreferredGarmentGroup == batch.GarmentGroup) score += 20;
        if (x.CapacityKg - x.CurrentWeightKg >= (batch.ReceivedWeight ?? batch.TotalWeight)) score += 10;
        return new StorageLocationDto(x.Id, x.LocationCode, x.Area.AreaName, x.AisleCode,
            x.RackCode, x.ShelfCode, x.BinCode, x.PreferredGarmentGroup,
            x.PreferredProcessingDirection, x.CapacityKg, x.CurrentWeightKg,
            x.CapacityKg - x.CurrentWeightKg, x.Status, score);
    }

    private static WarehouseInboundBatchDto MapBatch(ClassifiedBatch x) => new(x.Id, x.BatchCode,
        x.ClassificationDate, x.FabricType, x.GarmentGroup, x.ClothingType, x.Gender, x.TargetUser,
        x.Size, Grade(x.ConditionRating), x.ProcessingDirection, x.TotalItem, x.TotalWeight, x.Status,
        x.SentToWarehouseAt, x.WarehouseReceivedAt, x.ReceivedWeight, x.ReceivedItemCount,
        x.WarehouseReceiptNotes,
        x.DonationRequestSources.Select(source => source.DonationRequest.RequestCode)
            .Distinct().OrderBy(code => code).ToList(),
        x.Items.OrderBy(i => i.ItemCode).Select(i =>
            new ClassificationItemDto(i.Id, i.ItemCode, i.FabricType, i.GarmentGroup,
                i.ClothingType, i.Gender, i.TargetUser, i.Size, Grade(i.ConditionRating),
                i.ProcessingDirection, i.ImageUrls ?? [], i.Notes, i.ClassifiedAt,
                i.FabricTypeId, i.GarmentGroupId, i.ClothingTypeId, i.GenderId,
                i.TargetUserId, i.SizeId, [])).ToList());

    private static string BuildReceiptNotes(int expectedCount, ConfirmWarehouseReceiptDto dto)
    {
        var variance = dto.ActualItemCount - expectedCount;
        var seal = dto.SealIntact ? "Seal intact" : "Seal discrepancy";
        return $"{seal}; item variance: {variance:+#;-#;0}. {dto.DiscrepancyNotes}".Trim();
    }

    private static void ValidateWarehouse(CreateWarehouseDto dto)
    {
        var name = dto.WarehouseName?.Trim() ?? string.Empty;
        var address = dto.Address?.Trim() ?? string.Empty;
        if (name.Length is < 3 or > 150)
            throw new InvalidOperationException("Warehouse name must contain 3-150 characters.");
        if (address.Length is < 10 or > 500)
            throw new InvalidOperationException("Warehouse address must contain 10-500 characters.");
        if (dto.TotalCapacityKg <= 0 || dto.TotalCapacityKg > 10_000_000)
            throw new InvalidOperationException("Warehouse capacity must be between 1 and 10,000,000 kg.");
        if (!string.IsNullOrWhiteSpace(dto.Email)
            && !System.Net.Mail.MailAddress.TryCreate(dto.Email.Trim(), out _))
            throw new InvalidOperationException("Warehouse email format is invalid.");
    }

    private async Task<decimal> GetWarehouseActualWeightAsync(Guid warehouseId)
    {
        var inventoryWeight = await context.Inventories.AsNoTracking()
            .Where(x => x.WarehouseId == warehouseId && x.IsActive != false)
            .SumAsync(x => (decimal?)x.TotalWeight) ?? 0;
        var stagingWeight = await context.IntakeBatches.AsNoTracking()
            .Where(x => x.WarehouseId == warehouseId && x.IsActive != false
                && x.CurrentStorageLocationId.HasValue)
            .SumAsync(x => (decimal?)x.TotalWeight) ?? 0;
        return inventoryWeight + stagingWeight;
    }

    private static string Grade(int rating) => rating == 1 ? "A" : rating == 2 ? "B" : "C";
    private static string ProcessingDirectionForGrade(int rating) =>
        rating == 1 ? "Charity" : rating == 2 ? "Recycling" : "Disposal";
}
