using BLL.DTOs;
using BLL.Common;
using BLL.Services.Interfaces.ClassificationOperations;
using BLL.Services.Implements.Notifications;
using DAL;
using DAL.Models;
using DAL.Models.Enum;
using Microsoft.EntityFrameworkCore;

namespace BLL.Services.Implements.ClassificationOperations;

public class ClassificationOperationsService(AppDbContext context) : IClassificationOperationsService
{
    private const string FabricType = "FabricType";
    private const string GarmentGroup = "GarmentGroup";
    private const string ClothingType = "ClothingType";
    private const string Gender = "Gender";
    private const string TargetUser = "TargetUser";
    private const string Size = "Size";
    private const string ConditionGrade = "ConditionGrade";

    public async Task<IReadOnlyList<ClassificationBatchSummaryDto>> GetBatchesAsync(Guid staffId) =>
        await context.IntakeBatches.AsNoTracking()
            .Where(x => x.IsActive != false && x.ClassificationTeamId != null
                && x.ClassificationTeam!.Members.Any(m => m.StaffId == staffId && m.IsActive != false)
                && (x.Status == "AssignedToClassification"
                || x.Status == "AwaitingClassificationCount" || x.Status == "ReadyForClassification"
                || x.Status == "Classifying"
                || (x.Status == "InClassifiedArea" && x.ClassifiedItems.Any(item =>
                    item.IsActive != false && item.ClassifiedBatch != null
                    && item.ClassifiedBatch.IsActive != false
                    && item.ClassifiedBatch.Status == "Open"))))
            .OrderByDescending(x => x.IntakeDate)
            .Select(x => new ClassificationBatchSummaryDto(x.Id, x.BatchCode, x.RouteName, x.IntakeDate,
                x.TotalWeight, x.Status,
                x.IntakeBatchDonationRequests.Count, x.ClassifiedItems.Count(i => i.IsActive != false),
                x.CountedItemCount, x.CountedTotalWeight, x.CountedAt,
                x.ClassificationAreaName, x.ClassifiedAreaPlacedAt, x.ClassificationTeamId,
                x.ClassificationTeam!.TeamName, x.ClassificationTeam.Status,
                x.CurrentArea != null ? x.CurrentArea.AreaName : null,
                x.ClassificationTeam.Shift.ShiftDate, x.ClassificationTeam.Shift.StartTime,
                x.ClassificationTeam.Shift.EndTime)).ToListAsync();

    public async Task<ClassificationBatchDetailDto?> GetBatchAsync(Guid staffId, Guid batchId)
    {
        var batch = await context.IntakeBatches.AsNoTracking()
            .Include(x => x.IntakeBatchDonationRequests)
            .Include(x => x.ClassifiedItems.Where(i => i.IsActive != false))
                .ThenInclude(i => i.InspectionAnswers.Where(a => a.IsActive != false))
            .FirstOrDefaultAsync(x => x.Id == batchId && x.IsActive != false
                && x.ClassificationTeam != null && x.ClassificationTeam.Members.Any(m =>
                    m.StaffId == staffId && m.IsActive != false));
        return batch is null ? null : MapBatch(batch);
    }

    public async Task<ClassificationCatalogDto> GetCatalogAsync()
    {
        var categories = await context.Categories.AsNoTracking()
            .Where(x => x.IsActive != false)
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Name)
            .ToListAsync();
        IReadOnlyList<CategoryOptionDto> OfType(string type) => categories
            .Where(x => x.Type == type)
            .Select(x => new CategoryOptionDto(x.Id, x.Code, x.Name, x.ParentId, x.SortOrder))
            .ToList();
        var questions = await context.ConditionQuestions.AsNoTracking().Where(x => x.IsActive != false)
            .Include(x => x.Answers.Where(a => a.IsActive != false)).OrderBy(x => x.DisplayOrder).ToListAsync();
        return new ClassificationCatalogDto(OfType(FabricType), OfType(GarmentGroup),
            OfType(ClothingType), OfType(Gender), OfType(TargetUser), OfType(Size), OfType(ConditionGrade),
            questions.Select(q => new ClassificationQuestionDto(q.Id, q.QuestionText, q.DisplayOrder,
                q.Answers.OrderBy(a => a.ConditionRating).Select(a => new ClassificationOptionDto(
                    a.Id, a.AnswerText, Grade(a.ConditionRating))).ToList())).ToList());
    }

    public async Task StartBatchAsync(Guid staffId, Guid batchId)
    {
        var batch = await RequireBatch(batchId);
        await RequireActiveClassificationTeamAsync(staffId, batch);
        if (batch.Status == "InClassifiedArea") throw new InvalidOperationException("Batch has already been classified.");
        if (batch.Status is not ("ReadyForClassification" or "Classifying"))
            throw new InvalidOperationException("Count the items and total batch weight before starting classification.");
        var now = DateTime.UtcNow;
        batch.Status = "Classifying";
        batch.ClassificationStartedAt ??= now;
        batch.ClassificationStartedByStaffId ??= staffId;
        batch.UpdateAt = now;
        batch.UpdatedBy = staffId;
        await context.SaveChangesAsync();
    }

    public async Task ConfirmReceiptAsync(Guid staffId, Guid batchId)
    {
        await using var transaction = await context.Database.BeginTransactionAsync();
        var batch = await RequireBatch(batchId);
        await RequireActiveClassificationTeamAsync(staffId, batch);
        if (batch.Status != "AssignedToClassification")
            throw new InvalidOperationException("Only an assigned intake batch can be confirmed.");
        var receivedAt = VietnamTime.Now;
        var area = await EnsureAreaAsync(batch.WarehouseId, "Unclassified", "Khu chưa phân loại",
            "Khu lưu Intake Batch đã bàn giao và đang chờ phân loại.");
        await MoveBatchToStagingAreaAsync(batch, area);
        batch.Status = "AwaitingClassificationCount";
        batch.CurrentAreaId = area.Id;
        batch.ClassificationReceivedAt = receivedAt;
        batch.ClassificationReceivedByStaffId = staffId;
        batch.UpdateAt = receivedAt;
        batch.UpdatedBy = staffId;
        var actor = await NotificationWriter.ActorNameAsync(context, staffId);
        var sourceIds = await context.IntakeBatchDonationRequests.Where(x => x.IntakeBatchId == batchId)
            .Select(x => x.DonationRequestId).ToListAsync();
        await context.DonationRequests.Where(x => sourceIds.Contains(x.Id))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, DonationRequestStatus.Classifying)
                .SetProperty(x => x.UpdateAt, receivedAt)
                .SetProperty(x => x.UpdatedBy, staffId));
        await NotificationWriter.NotifyDonorsAsync(context, sourceIds, "ClassificationReceived",
            "Bộ phận phân loại đã nhận lô",
            _ => $"lô {batch.BatchCode} được {actor} xác nhận nhận lúc {NotificationWriter.FormatTime(receivedAt)}.", staffId);
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    public async Task CountBatchAsync(Guid staffId, Guid batchId, CountClassificationBatchDto dto)
    {
        if (dto.ItemCount <= 0)
            throw new InvalidOperationException("The counted item quantity must be greater than zero.");
        if (dto.TotalWeightKg <= 0)
            throw new InvalidOperationException("The counted total weight must be greater than zero.");

        var batch = await RequireBatch(batchId);
        await RequireActiveClassificationTeamAsync(staffId, batch);
        if (batch.Status is not ("AwaitingClassificationCount" or "ReadyForClassification"))
            throw new InvalidOperationException("Only a received batch that has not started classification can be counted.");
        if (await context.ClassifiedItems.AnyAsync(x => x.BatchId == batchId && x.IsActive != false))
            throw new InvalidOperationException("The count can no longer be changed after item classification has started.");

        var now = DateTime.UtcNow;
        batch.CountedItemCount = dto.ItemCount;
        batch.CountedTotalWeight = decimal.Round(dto.TotalWeightKg, 2);
        batch.CountingNotes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim();
        batch.CountedAt = now;
        batch.CountedByStaffId = staffId;
        batch.Status = "ReadyForClassification";
        batch.UpdateAt = now;
        batch.UpdatedBy = staffId;
        await context.SaveChangesAsync();
    }

    public async Task<ClassificationItemDto> ClassifyItemAsync(Guid staffId, Guid batchId, ClassifyItemDto dto)
    {
        var batch = await RequireBatch(batchId);
        await RequireActiveClassificationTeamAsync(staffId, batch);
        if (batch.Status != "Classifying") throw new InvalidOperationException("Start the batch before classifying items.");
        if (!batch.CountedItemCount.HasValue)
            throw new InvalidOperationException("The batch count has not been recorded.");
        var classifiedCount = await context.ClassifiedItems.CountAsync(x => x.BatchId == batchId && x.IsActive != false);
        if (classifiedCount >= batch.CountedItemCount.Value)
            throw new InvalidOperationException("All counted items in this batch have already been classified.");
        var categorySelection = await ResolveCategoriesAsync(dto);
        var (rating, grade) = await ResolveConditionGradeAsync(dto);
        var item = new ClassifiedItem
        {
            Id = Guid.NewGuid(), BatchId = batchId, ItemCode = $"CI-{VietnamTime.Now:yyyyMMdd}-{Guid.NewGuid():N}"[..20].ToUpperInvariant(),
            FabricTypeId = categorySelection.Fabric.Id, GarmentGroupId = categorySelection.Group.Id,
            ClothingTypeId = categorySelection.Clothing.Id, GenderId = categorySelection.Gender.Id,
            TargetUserId = categorySelection.Target.Id, SizeId = categorySelection.Size.Id, ConditionGradeId = grade.Id,
            FabricType = categorySelection.Fabric.Name, GarmentGroup = categorySelection.Group.Name,
            ClothingType = categorySelection.Clothing.Name, Gender = categorySelection.Gender.Name,
            TargetUser = categorySelection.Target.Name, Size = categorySelection.Size.Name, ConditionRating = rating,
            ProcessingDirection = rating == 1 ? "Charity" : rating == 2 ? "Recycling" : "Disposal",
            ImageUrls = dto.ImageUrls, Notes = dto.Notes, ClassifiedByStaffId = staffId, ClassifiedAt = DateTime.UtcNow,
            CreateAt = DateTime.UtcNow, CreatedBy = staffId
        };
        var groupedBatch = await GetOrCreateGroupedBatchAsync(batch, item, categorySelection, staffId);
        item.ClassifiedBatchId = groupedBatch.Id;
        await LinkBatchProvenanceAsync(groupedBatch.Id, batchId, staffId);
        groupedBatch.TotalItem++;
        groupedBatch.UpdateAt = DateTime.UtcNow;
        groupedBatch.UpdatedBy = staffId;
        context.ClassifiedItems.Add(item);
        context.InspectionAnswers.AddRange(dto.Answers.Select(x => new InspectionAnswer
        {
            Id = Guid.NewGuid(), ClassifiedItemId = item.Id, ConditionQuestionId = x.QuestionId,
            ConditionAnswerId = x.AnswerId, CreateAt = DateTime.UtcNow, CreatedBy = staffId
        }));
        await context.SaveChangesAsync();
        return MapItem(item);
    }

    public async Task<ClassificationItemDto> UpdateItemAsync(
        Guid staffId, Guid batchId, Guid itemId, ClassifyItemDto dto)
    {
        var batch = await RequireBatch(batchId);
        await RequireActiveClassificationTeamAsync(staffId, batch);
        if (batch.Status != "Classifying")
            throw new InvalidOperationException("Only items in a batch being classified can be edited.");
        var item = await context.ClassifiedItems
            .Include(x => x.InspectionAnswers)
            .FirstOrDefaultAsync(x => x.Id == itemId && x.BatchId == batchId && x.IsActive != false)
            ?? throw new InvalidOperationException("Classified item not found.");
        var oldGroup = item.ClassifiedBatchId.HasValue
            ? await context.ClassifiedBatches.FirstOrDefaultAsync(x => x.Id == item.ClassifiedBatchId.Value)
            : null;
        if (oldGroup is not null && oldGroup.Status != "Open")
            throw new InvalidOperationException("This item has already entered the warehouse workflow.");

        var selection = await ResolveCategoriesAsync(dto);
        var (rating, grade) = await ResolveConditionGradeAsync(dto);
        item.FabricTypeId = selection.Fabric.Id;
        item.GarmentGroupId = selection.Group.Id;
        item.ClothingTypeId = selection.Clothing.Id;
        item.GenderId = selection.Gender.Id;
        item.TargetUserId = selection.Target.Id;
        item.SizeId = selection.Size.Id;
        item.ConditionGradeId = grade.Id;
        item.FabricType = selection.Fabric.Name;
        item.GarmentGroup = selection.Group.Name;
        item.ClothingType = selection.Clothing.Name;
        item.Gender = selection.Gender.Name;
        item.TargetUser = selection.Target.Name;
        item.Size = selection.Size.Name;
        item.ConditionRating = rating;
        item.ProcessingDirection = rating == 1 ? "Charity" : rating == 2 ? "Recycling" : "Disposal";
        item.ImageUrls = dto.ImageUrls;
        item.Notes = dto.Notes;
        item.UpdateAt = DateTime.UtcNow;
        item.UpdatedBy = staffId;

        var newGroup = await GetOrCreateGroupedBatchAsync(batch, item, selection, staffId);
        if (oldGroup?.Id != newGroup.Id)
        {
            if (oldGroup is not null)
            {
                oldGroup.TotalItem = Math.Max(0, oldGroup.TotalItem - 1);
                oldGroup.UpdateAt = DateTime.UtcNow;
                oldGroup.UpdatedBy = staffId;
                if (oldGroup.TotalItem == 0) oldGroup.IsActive = false;
            }
            newGroup.TotalItem++;
            newGroup.UpdateAt = DateTime.UtcNow;
            newGroup.UpdatedBy = staffId;
            item.ClassifiedBatchId = newGroup.Id;
            await LinkBatchProvenanceAsync(newGroup.Id, batchId, staffId);
        }

        var answerUpdates = dto.Answers
            .GroupBy(x => x.QuestionId)
            .ToDictionary(x => x.Key, x => x.Last().AnswerId);
        var now = DateTime.UtcNow;
        foreach (var answer in item.InspectionAnswers)
        {
            if (answerUpdates.Remove(answer.ConditionQuestionId, out var answerId))
            {
                answer.ConditionAnswerId = answerId;
                answer.IsActive = true;
            }
            else
            {
                answer.IsActive = false;
            }
            answer.UpdateAt = now;
            answer.UpdatedBy = staffId;
        }
        context.InspectionAnswers.AddRange(answerUpdates.Select(x => new InspectionAnswer
        {
            Id = Guid.NewGuid(), ClassifiedItemId = item.Id, ConditionQuestionId = x.Key,
            ConditionAnswerId = x.Value, CreateAt = now, CreatedBy = staffId, IsActive = true
        }));
        await context.SaveChangesAsync();
        return MapItem(item);
    }

    public async Task DeleteItemAsync(Guid staffId, Guid batchId, Guid itemId)
    {
        var batch = await RequireBatch(batchId);
        await RequireActiveClassificationTeamAsync(staffId, batch);
        if (batch.Status != "Classifying")
            throw new InvalidOperationException("Only items in a batch being classified can be deleted.");
        var item = await context.ClassifiedItems.Include(x => x.InspectionAnswers)
            .FirstOrDefaultAsync(x => x.Id == itemId && x.BatchId == batchId && x.IsActive != false)
            ?? throw new InvalidOperationException("Classified item not found.");
        var group = item.ClassifiedBatchId.HasValue
            ? await context.ClassifiedBatches.FirstOrDefaultAsync(x => x.Id == item.ClassifiedBatchId.Value)
            : null;
        if (group is not null && group.Status != "Open")
            throw new InvalidOperationException("This item has already entered the warehouse workflow.");
        var now = DateTime.UtcNow;
        item.IsActive = false;
        item.UpdateAt = now;
        item.UpdatedBy = staffId;
        foreach (var answer in item.InspectionAnswers)
        {
            answer.IsActive = false;
            answer.UpdateAt = now;
            answer.UpdatedBy = staffId;
        }
        if (group is not null)
        {
            group.TotalItem = Math.Max(0, group.TotalItem - 1);
            group.UpdateAt = now;
            group.UpdatedBy = staffId;
            if (group.TotalItem == 0) group.IsActive = false;
        }
        await context.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<GroupedClassifiedBatchDto>> GetGroupedBatchesAsync(DateTime? date)
    {
        var query = context.ClassifiedBatches.AsNoTracking().Where(x => x.IsActive != false);
        if (date.HasValue) query = query.Where(x => x.ClassificationDate == date.Value.Date);
        return await query.OrderByDescending(x => x.ClassificationDate).ThenBy(x => x.BatchCode)
            .Select(x => new GroupedClassifiedBatchDto(x.Id, x.BatchCode, x.ClassificationDate,
                x.FabricType, x.GarmentGroup, x.ClothingType, x.Gender, x.TargetUser, x.Size,
                x.ConditionRating == 1 ? "A" : x.ConditionRating == 2 ? "B" : "C",
                x.ProcessingDirection, x.TotalItem, x.Status, x.TotalWeight,
                x.ClassificationAreaName, x.PlacedInClassificationAreaAt,
                x.StorageLocationId,
                x.DonationRequestSources.Where(s => s.IsActive != false)
                    .Select(s => s.DonationRequest.RequestCode).Distinct().OrderBy(code => code).ToList()))
            .ToListAsync();
    }

    public async Task<ClassificationAreaLayoutDto> GetClassificationAreaLayoutAsync(Guid staffId, DateTime? date)
    {
        var staff = await context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == staffId && x.IsActive != false)
            ?? throw new InvalidOperationException("Classification staff not found.");
        if (!staff.WarehouseId.HasValue)
            throw new InvalidOperationException("Classification staff is not assigned to a warehouse.");
        var warehouse = await context.Warehouses.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == staff.WarehouseId.Value && x.IsActive != false)
            ?? throw new InvalidOperationException("Warehouse not found.");
        var areas = await context.WarehouseAreas.AsNoTracking()
            .Where(x => x.WarehouseId == warehouse.Id && x.AreaType == "Classified" && x.IsActive != false)
            .Include(x => x.Groups.Where(g => g.IsActive != false))
                .ThenInclude(g => g.StorageLocations.Where(l => l.IsActive != false))
            .OrderBy(x => x.AreaName).ToListAsync();
        var areaIds = areas.Select(x => x.Id).ToList();
        var query = context.ClassifiedBatches.AsNoTracking()
            .Where(x => x.WarehouseId == warehouse.Id && x.Status == "Open"
                && x.IsActive != false);
        if (date.HasValue) query = query.Where(x => x.ClassificationDate == date.Value.Date);
        var batches = await query.Include(x => x.DonationRequestSources.Where(s => s.IsActive != false))
            .ThenInclude(x => x.DonationRequest).OrderBy(x => x.BatchCode).ToListAsync();
        GroupedClassifiedBatchDto Map(ClassifiedBatch x) => new(x.Id, x.BatchCode, x.ClassificationDate,
            x.FabricType, x.GarmentGroup, x.ClothingType, x.Gender, x.TargetUser, x.Size,
            Grade(x.ConditionRating), x.ProcessingDirection, x.TotalItem, x.Status, x.TotalWeight,
            x.ClassificationAreaName, x.PlacedInClassificationAreaAt,
            x.StorageLocationId,
            x.DonationRequestSources.Select(s => s.DonationRequest.RequestCode).Distinct().OrderBy(c => c).ToList());
        return new ClassificationAreaLayoutDto(warehouse.Id, warehouse.WarehouseName,
            areas.Select(area => new ClassificationAreaDto(area.Id, area.AreaName, area.Description,
                area.CapacityKg, area.CurrentKg, area.Groups.OrderBy(g => g.GroupName).Select(group =>
                    new ClassificationAreaGroupDto(group.Id, group.GroupName, group.Description,
                        group.CapacityKg, group.CurrentKg,
                        group.StorageLocations.OrderBy(l => l.LocationCode).Select(l =>
                            new ClassificationLocationDto(l.Id, l.LocationCode, l.AisleCode,
                                l.RackCode, l.ShelfCode, l.BinCode, l.CapacityKg,
                                l.CurrentWeightKg, l.Status)).ToList(),
                        batches.Where(b => b.GroupId == group.Id).Select(Map).ToList())).ToList())).ToList(),
            batches.Where(x => !x.AreaId.HasValue || !areaIds.Contains(x.AreaId.Value)
                || !x.GroupId.HasValue).Select(Map).ToList());
    }

    public async Task<GroupedClassifiedBatchDetailDto?> GetGroupedBatchAsync(Guid groupedBatchId)
    {
        var group = await context.ClassifiedBatches.AsNoTracking()
            .Include(x => x.Items.Where(i => i.IsActive != false))
            .Include(x => x.DonationRequestSources.Where(s => s.IsActive != false))
                .ThenInclude(x => x.DonationRequest)
            .FirstOrDefaultAsync(x => x.Id == groupedBatchId && x.IsActive != false);
        return group is null ? null : new GroupedClassifiedBatchDetailDto(group.Id, group.BatchCode,
            group.ClassificationDate, group.FabricType, group.GarmentGroup, group.ClothingType,
            group.Gender, group.TargetUser, group.Size, Grade(group.ConditionRating),
            group.ProcessingDirection, group.TotalItem, group.Status, group.TotalWeight,
            group.ClassificationAreaName, group.PlacedInClassificationAreaAt,
            group.DonationRequestSources.Select(x => x.DonationRequest.RequestCode)
                .Distinct().OrderBy(code => code).ToList(),
            group.Items.OrderBy(x => x.ClassifiedAt).Select(MapItem).ToList());
    }

    public async Task SendGroupedBatchToWarehouseAsync(Guid staffId, Guid groupedBatchId)
    {
        var batch = await context.ClassifiedBatches
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == groupedBatchId && x.IsActive != false)
            ?? throw new InvalidOperationException("Classified batch not found.");
        if (batch.Status != "Open")
            throw new InvalidOperationException("Only an open classified batch can be sent to warehouse.");
        if (!batch.PlacedInClassificationAreaAt.HasValue)
            throw new InvalidOperationException("Complete classification and place the batch in the classified area first.");
        if (!batch.StorageLocationId.HasValue)
            throw new InvalidOperationException("Select a storage location before sending the batch to warehouse.");
        if (!batch.Items.Any(x => x.IsActive != false))
            throw new InvalidOperationException("The classified batch does not contain any item.");

        batch.TotalItem = batch.Items.Count(x => x.IsActive != false);
        batch.Status = "PendingWarehouseReceipt";
        batch.SentToWarehouseAt = DateTime.UtcNow;
        batch.SentToWarehouseByStaffId = staffId;
        batch.RemovedFromClassificationAreaAt = DateTime.UtcNow;
        batch.RemovedFromClassificationAreaByStaffId = staffId;
        batch.UpdateAt = DateTime.UtcNow;
        batch.UpdatedBy = staffId;
        await ReleaseGroupedBatchCapacityAsync(batch);
        var sourceIds = await context.ClassifiedBatchDonationRequests
            .Where(x => x.ClassifiedBatchId == groupedBatchId && x.IsActive != false)
            .Select(x => x.DonationRequestId).ToListAsync();
        await NotificationWriter.NotifyDonorsAsync(context, sourceIds, "SentToWarehouse", "Đã chuyển sang kho",
            _ => $"batch {batch.BatchCode} được chuyển sang kho lúc {NotificationWriter.FormatTime(DateTime.UtcNow)}.", staffId);
        await context.SaveChangesAsync();
    }

    public async Task<SendGroupedBatchesToWarehouseResultDto> SendGroupedBatchesToWarehouseAsync(
        Guid staffId, IReadOnlyList<Guid> groupedBatchIds)
    {
        var ids = groupedBatchIds.Where(x => x != Guid.Empty).Distinct().ToList();
        if (ids.Count == 0)
            throw new InvalidOperationException("Select at least one classified batch.");

        var batches = await context.ClassifiedBatches
            .Include(x => x.Items)
            .Where(x => ids.Contains(x.Id) && x.IsActive != false)
            .ToListAsync();
        if (batches.Count != ids.Count)
            throw new InvalidOperationException("One or more classified batches no longer exist.");

        var now = DateTime.UtcNow;
        var sent = 0;
        var sentBatchIds = new List<Guid>();
        foreach (var batch in batches.Where(x => x.Status == "Open"))
        {
            if (!batch.PlacedInClassificationAreaAt.HasValue || !batch.StorageLocationId.HasValue)
                continue;
            var itemCount = batch.Items.Count(x => x.IsActive != false);
            if (itemCount == 0)
                throw new InvalidOperationException(
                    $"Classified batch {batch.BatchCode} does not contain any item.");

            batch.TotalItem = itemCount;
            batch.Status = "PendingWarehouseReceipt";
            batch.SentToWarehouseAt = now;
            batch.SentToWarehouseByStaffId = staffId;
            batch.RemovedFromClassificationAreaAt = now;
            batch.RemovedFromClassificationAreaByStaffId = staffId;
            batch.UpdateAt = now;
            batch.UpdatedBy = staffId;
            await ReleaseGroupedBatchCapacityAsync(batch);
            sentBatchIds.Add(batch.Id);
            sent++;
        }

        if (sent > 0)
        {
            var provenance = await context.ClassifiedBatchDonationRequests
                .Where(x => sentBatchIds.Contains(x.ClassifiedBatchId) && x.IsActive != false)
                .Select(x => new { x.ClassifiedBatchId, x.DonationRequestId }).ToListAsync();
            foreach (var batch in batches.Where(x => sentBatchIds.Contains(x.Id)))
                await NotificationWriter.NotifyDonorsAsync(context,
                    provenance.Where(x => x.ClassifiedBatchId == batch.Id).Select(x => x.DonationRequestId),
                    "SentToWarehouse", "Đã chuyển sang kho",
                    _ => $"batch {batch.BatchCode} được chuyển sang kho lúc {NotificationWriter.FormatTime(now)}.", staffId);
            await context.SaveChangesAsync();
        }
        return new SendGroupedBatchesToWarehouseResultDto(sent, batches.Count - sent);
    }

    public async Task CompleteBatchAsync(Guid staffId, Guid batchId)
    {
        var batch = await RequireBatch(batchId);
        await RequireActiveClassificationTeamAsync(staffId, batch);
        if (batch.Status != "Classifying") throw new InvalidOperationException("Only a batch being classified can be completed.");
        var itemCount = await context.ClassifiedItems.CountAsync(x => x.BatchId == batchId && x.IsActive != false);
        if (!batch.CountedItemCount.HasValue)
            throw new InvalidOperationException("The batch count has not been recorded.");
        if (itemCount != batch.CountedItemCount.Value)
            throw new InvalidOperationException(
                $"Complete the classification of all counted items first ({itemCount}/{batch.CountedItemCount.Value}).");

        var now = DateTime.UtcNow;
        const string areaName = "Khu đã phân loại";
        await RemoveBatchFromCurrentAreaAsync(batch);
        batch.Status = "InClassifiedArea";
        batch.ClassificationCompletedAt = now;
        batch.ClassificationCompletedByStaffId = staffId;
        batch.ClassificationAreaName = areaName;
        batch.CurrentAreaId = null;
        batch.CurrentAreaGroupId = null;
        batch.CurrentStorageLocationId = null;
        batch.ClassifiedAreaPlacedAt = null;
        batch.ClassifiedAreaPlacedByStaffId = null;
        batch.UpdateAt = now;
        batch.UpdatedBy = staffId;

        var groupedBatches = await context.ClassifiedBatches
            .Where(x => x.Items.Any(i => i.BatchId == batchId && i.IsActive != false) && x.IsActive != false)
            .ToListAsync();
        var itemCountsByGroup = await context.ClassifiedItems
            .Where(x => x.BatchId == batchId && x.IsActive != false && x.ClassifiedBatchId.HasValue)
            .GroupBy(x => x.ClassifiedBatchId!.Value)
            .ToDictionaryAsync(x => x.Key, x => x.Count());
        var remainingWeight = batch.TotalWeight;
        for (var groupedIndex = 0; groupedIndex < groupedBatches.Count; groupedIndex++)
        {
            var groupedBatch = groupedBatches[groupedIndex];
            if (itemCountsByGroup.TryGetValue(groupedBatch.Id, out var groupedItemCount) && itemCount > 0)
            {
                var allocatedWeight = groupedIndex == groupedBatches.Count - 1
                    ? remainingWeight
                    : decimal.Round(batch.TotalWeight * groupedItemCount / itemCount, 2,
                        MidpointRounding.AwayFromZero);
                groupedBatch.TotalWeight += allocatedWeight;
                remainingWeight -= allocatedWeight;
            }
            groupedBatch.AreaId = null;
            groupedBatch.GroupId = null;
            groupedBatch.ClassificationAreaName = null;
            groupedBatch.PlacedInClassificationAreaAt = null;
            groupedBatch.PlacedInClassificationAreaByStaffId = null;
            groupedBatch.UpdateAt = now;
            groupedBatch.UpdatedBy = staffId;
        }
        var sourceIds = await context.IntakeBatchDonationRequests.Where(x => x.IntakeBatchId == batchId)
            .Select(x => x.DonationRequestId).ToListAsync();
        await context.DonationRequests.Where(x => sourceIds.Contains(x.Id))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, DonationRequestStatus.Classified)
                .SetProperty(x => x.UpdateAt, now)
                .SetProperty(x => x.UpdatedBy, staffId));
        await NotificationWriter.NotifyDonorsAsync(context, sourceIds, "ClassificationCompleted",
            "Đã phân loại xong", _ => $"lô {batch.BatchCode} hoàn tất phân loại lúc {NotificationWriter.FormatTime(DateTime.UtcNow)}.", staffId);
        await context.SaveChangesAsync();
    }

    public async Task StartTeamAsync(Guid staffId, Guid teamId)
    {
        var team = await RequireMyClassificationTeamAsync(staffId, teamId);
        if (team.Status != "Scheduled")
            throw new InvalidOperationException("Classification team has already started or completed this shift.");
        var now = VietnamTime.Now;
        var shiftStart = team.Shift.ShiftDate.Date.Add(team.Shift.StartTime);
        var shiftEnd = team.Shift.ShiftDate.Date.Add(team.Shift.EndTime);
        if (now < shiftStart) throw new InvalidOperationException("The classification shift has not started yet.");
        if (now >= shiftEnd) throw new InvalidOperationException("The classification shift has already ended.");
        team.Status = "InProgress";
        team.StartedAt = now;
        team.StartedByStaffId = staffId;
        team.UpdateAt = now;
        await context.SaveChangesAsync();
    }

    public async Task PlaceGroupedBatchAsync(Guid staffId, Guid groupedBatchId, PlaceGroupedClassifiedBatchDto dto)
    {
        var staff = await context.Users.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == staffId && x.IsActive != false)
            ?? throw new InvalidOperationException("Classification staff not found.");
        if (!staff.WarehouseId.HasValue)
            throw new InvalidOperationException("Classification staff is not assigned to a warehouse.");

        var batch = await context.ClassifiedBatches
            .FirstOrDefaultAsync(x => x.Id == groupedBatchId && x.IsActive != false)
            ?? throw new InvalidOperationException("Classified batch not found.");
        if (batch.WarehouseId != staff.WarehouseId.Value)
            throw new InvalidOperationException("Classified batch is not in your warehouse.");
        if (batch.Status != "Open")
            throw new InvalidOperationException("Only an open classified batch can be placed.");
        if (batch.PlacedInClassificationAreaAt.HasValue)
            throw new InvalidOperationException("Classified batch has already been placed.");
        if (dto.ActualWeightKg <= 0)
            throw new InvalidOperationException("Actual weight must be greater than zero.");
        var actualWeightKg = decimal.Round(dto.ActualWeightKg, 2, MidpointRounding.AwayFromZero);

        var area = await context.WarehouseAreas.FirstOrDefaultAsync(x => x.Id == dto.AreaId
            && x.WarehouseId == batch.WarehouseId && x.AreaType == "Classified" && x.IsActive != false)
            ?? throw new InvalidOperationException("Classified area not found.");
        var group = await context.AreaGroups.FirstOrDefaultAsync(x => x.Id == dto.GroupId
            && x.AreaId == area.Id && x.IsActive != false)
            ?? throw new InvalidOperationException("Aisle does not belong to the selected area.");
        var location = await context.StorageLocations.FirstOrDefaultAsync(x =>
            x.Id == dto.StorageLocationId && x.WarehouseId == batch.WarehouseId
            && x.AreaId == area.Id && x.AreaGroupId == group.Id && x.IsActive != false)
            ?? throw new InvalidOperationException("Storage location does not belong to the selected aisle.");
        if (group.CurrentKg + actualWeightKg > group.CapacityKg
            || area.CurrentKg + actualWeightKg > area.CapacityKg
            || location.CurrentWeightKg + actualWeightKg > location.CapacityKg)
            throw new InvalidOperationException("The selected area, aisle, or storage location does not have enough capacity.");

        var placedAt = VietnamTime.Now;
        batch.AreaId = area.Id;
        batch.GroupId = group.Id;
        batch.StorageLocationId = location.Id;
        batch.ClassificationAreaName = area.AreaName;
        batch.PlacedInClassificationAreaAt = placedAt;
        batch.PlacedInClassificationAreaByStaffId = staffId;
        batch.TotalWeight = actualWeightKg;
        batch.UpdateAt = placedAt;
        batch.UpdatedBy = staffId;
        group.CurrentKg += actualWeightKg;
        group.UpdateAt = placedAt;
        area.CurrentKg += actualWeightKg;
        area.UpdateAt = placedAt;
        location.CurrentWeightKg += actualWeightKg;
        location.Status = location.CurrentWeightKg >= location.CapacityKg ? "Full" : "Available";
        location.UpdateAt = placedAt;
        await context.SaveChangesAsync();
    }

    private async Task ReleaseGroupedBatchCapacityAsync(ClassifiedBatch batch)
    {
        if (batch.GroupId.HasValue)
        {
            var group = await context.AreaGroups.FirstOrDefaultAsync(x => x.Id == batch.GroupId.Value);
            if (group is not null)
            {
                group.CurrentKg = Math.Max(0, group.CurrentKg - batch.TotalWeight);
                group.UpdateAt = VietnamTime.Now;
            }
        }
        if (batch.AreaId.HasValue)
        {
            var area = await context.WarehouseAreas.FirstOrDefaultAsync(x => x.Id == batch.AreaId.Value);
            if (area is not null)
            {
                area.CurrentKg = Math.Max(0, area.CurrentKg - batch.TotalWeight);
                area.UpdateAt = VietnamTime.Now;
            }
        }
        if (batch.StorageLocationId.HasValue)
        {
            var location = await context.StorageLocations
                .FirstOrDefaultAsync(x => x.Id == batch.StorageLocationId.Value);
            if (location is not null)
            {
                location.CurrentWeightKg = Math.Max(0, location.CurrentWeightKg - batch.TotalWeight);
                location.Status = "Available";
                location.UpdateAt = VietnamTime.Now;
            }
        }
    }

    public async Task CompleteTeamAsync(Guid staffId, Guid teamId)
    {
        var team = await RequireMyClassificationTeamAsync(staffId, teamId);
        if (team.Status != "InProgress")
            throw new InvalidOperationException("Only an active classification shift can be completed.");
        if (await context.IntakeBatches.AnyAsync(x => x.ClassificationTeamId == teamId
                && x.IsActive != false && x.Status != "InClassifiedArea"))
            throw new InvalidOperationException("Complete all assigned intake batches before ending the shift.");
        var now = VietnamTime.Now;
        team.Status = "Completed";
        team.CompletedAt = now;
        team.CompletedByStaffId = staffId;
        team.UpdateAt = now;
        await context.SaveChangesAsync();
    }

    public async Task<ClassificationManagementBoardDto> GetManagementBoardAsync(
        Guid? warehouseId, DateTime? date)
    {
        await ShiftLifecycle.CompleteEndedShiftsAsync(context);

        var warehouses = await context.Warehouses.AsNoTracking().Where(x => x.IsActive != false)
            .OrderBy(x => x.WarehouseName)
            .Select(x => new ManagerWarehouseOptionDto(x.Id, x.WarehouseName, x.Address)).ToListAsync();
        var staffQuery = context.Users.AsNoTracking().Where(x => x.IsActive != false
            && x.Role.RoleName == "ClassificationStaff");
        if (warehouseId.HasValue) staffQuery = staffQuery.Where(x => x.WarehouseId == warehouseId);
        var staff = await staffQuery.OrderBy(x => x.FullName).Select(x => new ClassificationStaffOptionDto(
            x.Id, x.FullName, x.UserName, x.PhoneNumber, x.WarehouseId)).ToListAsync();

        var teamQuery = context.OperationalTeams.AsNoTracking()
            .Where(x => x.IsActive != false && x.TeamType == "Classification");
        if (warehouseId.HasValue) teamQuery = teamQuery.Where(x => x.Shift.WarehouseId == warehouseId);
        if (date.HasValue) teamQuery = teamQuery.Where(x => x.Shift.ShiftDate.Date == date.Value.Date);
        var teams = await teamQuery.OrderByDescending(x => x.Shift.ShiftDate).ThenBy(x => x.Shift.StartTime)
            .Select(x => new ClassificationTeamDto(x.Id, x.ShiftId, x.TeamName, x.Status,
                x.Shift.ShiftDate, x.Shift.StartTime, x.Shift.EndTime, x.Shift.WarehouseId,
                x.Shift.Warehouse.WarehouseName, x.StartedAt, x.CompletedAt,
                x.Members.Where(m => m.IsActive != false).Select(m =>
                    new ReceivingTeamMemberDto(m.StaffId, m.Staff.FullName, m.Staff.PhoneNumber)).ToList(),
                x.ClassificationBatches.Count(b => b.IsActive != false),
                x.ClassificationBatches.Count(b => b.IsActive != false && b.Status == "InClassifiedArea")))
            .ToListAsync();

        var batchQuery = context.IntakeBatches.AsNoTracking().Where(x => x.IsActive != false
            && (x.Status == "AwaitingClassificationAssignment" || x.ClassificationTeamId != null));
        if (warehouseId.HasValue) batchQuery = batchQuery.Where(x => x.WarehouseId == warehouseId);
        if (date.HasValue)
            batchQuery = batchQuery.Where(x => x.Status == "AwaitingClassificationAssignment"
                || (x.ClassificationTeam != null
                    && x.ClassificationTeam.Shift.ShiftDate.Date == date.Value.Date));
        var batches = await batchQuery.OrderByDescending(x => x.SentToClassificationAt)
            .Select(x => new ClassificationManagementBatchDto(x.Id, x.BatchCode, x.Status,
                x.WarehouseId, x.Warehouse.WarehouseName, x.TotalWeight,
                x.IntakeBatchDonationRequests.Count(r => r.IsActive != false), x.ClassificationTeamId,
                x.ClassificationTeam != null ? x.ClassificationTeam.TeamName : null,
                x.CurrentArea != null ? x.CurrentArea.AreaName : null, x.SentToClassificationAt))
            .ToListAsync();
        return new ClassificationManagementBoardDto(warehouses, staff, teams, batches);
    }

    public async Task AssignBatchAsync(Guid managerId, Guid batchId, Guid teamId)
    {
        var batch = await RequireBatch(batchId);
        if (batch.Status != "AwaitingClassificationAssignment")
            throw new InvalidOperationException("Only a batch waiting for classification assignment can be assigned.");
        var team = await context.OperationalTeams.Include(x => x.Shift)
            .Include(x => x.Members).ThenInclude(x => x.Staff)
            .FirstOrDefaultAsync(x => x.Id == teamId && x.IsActive != false && x.TeamType == "Classification")
            ?? throw new InvalidOperationException("Classification team not found.");
        if (team.Shift.WarehouseId != batch.WarehouseId)
            throw new InvalidOperationException("Batch and classification team must belong to the same warehouse.");
        if (team.Status != "Scheduled")
            throw new InvalidOperationException("Cannot assign a batch after the classification team has started.");
        if (VietnamTime.Now >= team.Shift.ShiftDate.Date.Add(team.Shift.EndTime))
            throw new InvalidOperationException("Cannot assign a batch to an ended shift.");
        if (team.Members.Count(x => x.IsActive != false) is < 1 or > 2)
            throw new InvalidOperationException("Classification team must have one or two members.");
        var now = VietnamTime.Now;
        batch.ClassificationTeamId = team.Id;
        batch.ClassificationAssignedAt = now;
        batch.ClassificationAssignedByManagerId = managerId;
        batch.Status = "AssignedToClassification";
        batch.UpdateAt = now;
        batch.UpdatedBy = managerId;
        foreach (var member in team.Members.Where(x => x.IsActive != false))
            NotificationWriter.NotifyUser(context, member.StaffId, "ClassificationBatchAssigned",
                "Bạn có lô phân loại mới",
                $"Lô {batch.BatchCode} đã được phân công cho {team.TeamName}.",
                $"/classification/batches/{batch.Id}", managerId);
        await context.SaveChangesAsync();
    }

    private async Task RequireActiveClassificationTeamAsync(Guid staffId, IntakeBatch batch)
    {
        if (!batch.ClassificationTeamId.HasValue)
            throw new InvalidOperationException("The batch has not been assigned to a classification team.");
        var team = await RequireMyClassificationTeamAsync(staffId, batch.ClassificationTeamId.Value);
        if (team.Status != "InProgress")
            throw new InvalidOperationException("Start the classification team shift before processing this batch.");
    }

    private async Task<OperationalTeam> RequireMyClassificationTeamAsync(Guid staffId, Guid teamId) =>
        await context.OperationalTeams.Include(x => x.Shift).Include(x => x.Members)
            .FirstOrDefaultAsync(x => x.Id == teamId && x.IsActive != false
                && x.TeamType == "Classification"
                && x.Members.Any(m => m.StaffId == staffId && m.IsActive != false))
        ?? throw new InvalidOperationException("Classification team not found for this staff member.");

    private async Task<WarehouseArea> EnsureAreaAsync(Guid warehouseId, string areaType,
        string areaName, string description)
    {
        var area = await context.WarehouseAreas.FirstOrDefaultAsync(x => x.WarehouseId == warehouseId
            && x.AreaType == areaType && x.IsActive != false);
        if (area is not null) return area;
        area = new WarehouseArea
        {
            Id = Guid.NewGuid(), WarehouseId = warehouseId, AreaType = areaType,
            AreaName = areaName, Description = description, CapacityKg = 5000,
            CurrentKg = 0, CreateAt = VietnamTime.Now, IsActive = true
        };
        context.WarehouseAreas.Add(area);
        return area;
    }

    private async Task MoveBatchToStagingAreaAsync(IntakeBatch batch, WarehouseArea destinationArea)
    {
        var destination = await context.AreaGroups
            .Where(x => x.AreaId == destinationArea.Id && x.IsActive != false
                && x.CapacityKg - x.CurrentKg >= batch.TotalWeight)
            .OrderBy(x => x.CurrentKg).FirstOrDefaultAsync();
        if (destination is null)
            throw new InvalidOperationException($"No staging aisle in {destinationArea.AreaName} has enough capacity.");

        await RemoveBatchFromCurrentAreaAsync(batch);
        destination.CurrentKg += batch.TotalWeight;
        destination.UpdateAt = VietnamTime.Now;
        destinationArea.CurrentKg += batch.TotalWeight;
        destinationArea.UpdateAt = VietnamTime.Now;
        batch.CurrentAreaGroupId = destination.Id;
        batch.CurrentStorageLocationId = null;
    }

    private async Task RemoveBatchFromCurrentAreaAsync(IntakeBatch batch)
    {
        if (batch.CurrentAreaGroupId.HasValue)
        {
            if (batch.CurrentStorageLocationId.HasValue)
            {
                var sourceLocation = await context.StorageLocations
                    .FirstOrDefaultAsync(x => x.Id == batch.CurrentStorageLocationId.Value);
                if (sourceLocation is not null)
                {
                    sourceLocation.CurrentWeightKg = Math.Max(0,
                        sourceLocation.CurrentWeightKg - batch.TotalWeight);
                    sourceLocation.UpdateAt = VietnamTime.Now;
                }
            }
            var source = await context.AreaGroups.FirstOrDefaultAsync(x => x.Id == batch.CurrentAreaGroupId.Value);
            if (source is not null)
            {
                source.CurrentKg = Math.Max(0, source.CurrentKg - batch.TotalWeight);
                source.UpdateAt = VietnamTime.Now;
                var sourceArea = await context.WarehouseAreas.FirstOrDefaultAsync(x => x.Id == source.AreaId);
                if (sourceArea is not null)
                {
                    sourceArea.CurrentKg = Math.Max(0, sourceArea.CurrentKg - batch.TotalWeight);
                    sourceArea.UpdateAt = VietnamTime.Now;
                }
            }
        }
    }

    private async Task<IntakeBatch> RequireBatch(Guid id) => await context.IntakeBatches
        .FirstOrDefaultAsync(x => x.Id == id && x.IsActive != false)
        ?? throw new InvalidOperationException("Intake batch not found.");

    private async Task<ClassifiedBatch> GetOrCreateGroupedBatchAsync(IntakeBatch intakeBatch,
        ClassifiedItem item, CategorySelection selection, Guid staffId)
    {
        var localDate = VietnamTime.Today;
        var isAdult = selection.Target.Code.Equals("TARGET_ADULT", StringComparison.OrdinalIgnoreCase);
        var audienceKey = isAdult ? "ADULT" : "CHILDREN";
        var genderKey = isAdult ? selection.Gender.Id.ToString() : "ALL";
        var key = string.Join('|', intakeBatch.WarehouseId, localDate.ToString("yyyyMMdd"),
            item.ConditionGradeId, selection.Group.Id, audienceKey, genderKey,
            item.ProcessingDirection.ToLowerInvariant());
        var group = await context.ClassifiedBatches.FirstOrDefaultAsync(x => x.GroupKey.StartsWith(key)
            && x.Status == "Open" && x.IsActive != false);
        if (group is not null) return group;
        var groupKey = await context.ClassifiedBatches.AnyAsync(x => x.GroupKey.StartsWith(key))
            ? $"{key}|{Guid.NewGuid():N}"
            : key;
        group = new ClassifiedBatch
        {
            Id = Guid.NewGuid(), WarehouseId = intakeBatch.WarehouseId, ClassificationDate = localDate,
            GroupKey = groupKey, BatchCode = $"CB-{localDate:yyyyMMdd}-{Grade(item.ConditionRating)}-{Guid.NewGuid():N}"[..24].ToUpperInvariant(),
            FabricTypeId = null, GarmentGroupId = selection.Group.Id,
            ClothingTypeId = null, GenderId = isAdult ? selection.Gender.Id : null,
            TargetUserId = isAdult ? selection.Target.Id : null, SizeId = null,
            ConditionGradeId = item.ConditionGradeId,
            FabricType = "Nhiều chất liệu", GarmentGroup = selection.Group.Name,
            ClothingType = selection.Group.Name,
            Gender = isAdult ? selection.Gender.Name : "Không phân biệt",
            TargetUser = isAdult ? selection.Target.Name : "Trẻ em / Em bé",
            Size = "Nhiều kích cỡ",
            ConditionRating = item.ConditionRating, ProcessingDirection = item.ProcessingDirection,
            Status = "Open", TotalItem = 0, TotalWeight = 0, CreateAt = DateTime.UtcNow,
            CreatedBy = staffId
        };
        context.ClassifiedBatches.Add(group);
        return group;
    }

    private async Task LinkBatchProvenanceAsync(Guid classifiedBatchId, Guid intakeBatchId, Guid staffId)
    {
        var requestIds = await context.IntakeBatchDonationRequests.AsNoTracking()
            .Where(x => x.IntakeBatchId == intakeBatchId && x.IsActive != false)
            .Select(x => x.DonationRequestId)
            .Distinct()
            .ToListAsync();
        if (requestIds.Count == 0) return;

        var existingIds = await context.ClassifiedBatchDonationRequests.AsNoTracking()
            .Where(x => x.ClassifiedBatchId == classifiedBatchId
                && x.IntakeBatchId == intakeBatchId
                && requestIds.Contains(x.DonationRequestId))
            .Select(x => x.DonationRequestId)
            .ToListAsync();
        var now = DateTime.UtcNow;
        context.ClassifiedBatchDonationRequests.AddRange(requestIds
            .Where(id => !existingIds.Contains(id))
            .Select(id => new ClassifiedBatchDonationRequest
            {
                Id = Guid.NewGuid(),
                ClassifiedBatchId = classifiedBatchId,
                DonationRequestId = id,
                IntakeBatchId = intakeBatchId,
                LinkedAt = now,
                CreateAt = now,
                CreatedBy = staffId,
                IsActive = true
            }));
    }

    private async Task<CategorySelection> ResolveCategoriesAsync(ClassifyItemDto dto)
    {
        var ids = new[] { dto.FabricTypeId, dto.GarmentGroupId, dto.ClothingTypeId,
            dto.GenderId, dto.TargetUserId, dto.SizeId };
        if (ids.Any(x => x == Guid.Empty) || ids.Distinct().Count() != ids.Length)
            throw new InvalidOperationException("Every classification category must be selected.");
        var values = await context.Categories.Where(x => ids.Contains(x.Id) && x.IsActive != false).ToListAsync();
        Category Require(Guid id, string type) => values.FirstOrDefault(x => x.Id == id && x.Type == type)
            ?? throw new InvalidOperationException($"The selected {type} category is invalid or inactive.");
        var result = new CategorySelection(Require(dto.FabricTypeId, FabricType),
            Require(dto.GarmentGroupId, GarmentGroup), Require(dto.ClothingTypeId, ClothingType),
            Require(dto.GenderId, Gender), Require(dto.TargetUserId, TargetUser), Require(dto.SizeId, Size));
        if (result.Clothing.ParentId != result.Group.Id)
            throw new InvalidOperationException("The clothing type does not belong to the selected garment group.");
        return result;
    }

    private async Task<(int Rating, Category Grade)> ResolveConditionGradeAsync(ClassifyItemDto dto)
    {
        var questions = await context.ConditionQuestions.Include(x => x.Answers)
            .Where(x => x.IsActive != false).OrderBy(x => x.DisplayOrder).ToListAsync();
        if (dto.Answers.Count != questions.Count
            || dto.Answers.Select(x => x.QuestionId).Distinct().Count() != questions.Count)
            throw new InvalidOperationException("Every condition question must be answered exactly once.");
        var ratings = new List<int>();
        foreach (var question in questions)
        {
            var selected = dto.Answers.SingleOrDefault(x => x.QuestionId == question.Id);
            var answer = question.Answers.FirstOrDefault(x => x.Id == selected?.AnswerId && x.IsActive != false)
                ?? throw new InvalidOperationException("An answer does not belong to its condition question.");
            ratings.Add(answer.ConditionRating);
        }
        var rules = await context.Categories.Where(x => x.Type == ConditionGrade && x.IsActive != false)
            .ToListAsync();
        var gradeB = rules.FirstOrDefault(x => x.Code == "GRADE_B")
            ?? throw new InvalidOperationException("Grade B is not configured.");
        var gradeC = rules.FirstOrDefault(x => x.Code == "GRADE_C")
            ?? throw new InvalidOperationException("Grade C is not configured.");
        var rating = ratings.Count(x => x == 3) >= Math.Max(1, gradeC.MinimumMatchCount ?? 1) ? 3
            : ratings.Count(x => x == 2) >= Math.Max(1, gradeB.MinimumMatchCount ?? 2) ? 2 : 1;
        var grade = rules.FirstOrDefault(x => x.Code == $"GRADE_{Grade(rating)}")
            ?? throw new InvalidOperationException("The condition grade category is not configured.");
        return (rating, grade);
    }

    private sealed record CategorySelection(Category Fabric, Category Group, Category Clothing,
        Category Gender, Category Target, Category Size);

    private static string NormalizeStatus(string status) => status switch
    { "SentToClassification" => "PendingConfirmation", _ => status };
    private static string Grade(int rating) => rating == 1 ? "A" : rating == 2 ? "B" : "C";
    private static ClassificationItemDto MapItem(ClassifiedItem x) => new(x.Id, x.ItemCode, x.FabricType,
        x.GarmentGroup, x.ClothingType, x.Gender, x.TargetUser, x.Size, Grade(x.ConditionRating),
        x.ProcessingDirection, x.ImageUrls ?? [], x.Notes, x.ClassifiedAt,
        x.FabricTypeId, x.GarmentGroupId, x.ClothingTypeId, x.GenderId, x.TargetUserId, x.SizeId,
        x.InspectionAnswers.Where(a => a.IsActive != false)
            .Select(a => new ClassificationAnswerDto(a.ConditionQuestionId, a.ConditionAnswerId)).ToList());
    private static ClassificationBatchDetailDto MapBatch(IntakeBatch x) => new(x.Id, x.BatchCode, x.RouteName,
        x.IntakeDate, x.TotalWeight, NormalizeStatus(x.Status), x.IntakeBatchDonationRequests.Count,
        x.CountedItemCount, x.CountedTotalWeight, x.CountingNotes, x.CountedAt,
        x.ClassificationAreaName, x.ClassifiedAreaPlacedAt,
        x.ClassifiedItems.OrderBy(i => i.ClassifiedAt).Select(MapItem).ToList());
}
