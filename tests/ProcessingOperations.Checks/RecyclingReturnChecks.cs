using BLL.Common;
using BLL.DTOs;
using BLL.Services.Implements.ProcessingOperations;
using BLL.Services.Implements.ClassificationOperations;
using BLL.Services.Implements.WarehouseOperations;
using DAL;
using DAL.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Authentication;

internal static class RecyclingReturnChecks
{
    public static async Task Run(Func<AppDbContext> dbFactory,
        Func<Func<ProcessingOperationsService, Task>, Task> action,
        Guid operationId, Guid warehouseId, Guid recyclerId, Guid strangerId, Guid staffId, Guid wrongStaffId, Guid managerId)
    {
        void Check(bool value, string label)
        {
            if (!value) throw new Exception("FAIL: " + label);
            Console.WriteLine("PASS: " + label);
        }
        async Task Denied(Func<ProcessingOperationsService, Task> work, string label)
        {
            try { await action(work); }
            catch (Exception e) when (e is InvalidOperationException or AuthenticationException) { Check(true, label); return; }
            throw new Exception("FAIL: expected rejection: " + label);
        }
        var schedule = new ScheduleRecyclingReturnDto(VietnamTime.Today.AddDays(1), "Appointment");
        var dispatch = new DispatchRecyclingReturnDto("Organization truck", "RETURN-001", "Recycled clothing", [new("Clean clothing", 1, 2)]);
        await Denied(s => s.DispatchReturnAsync(recyclerId, operationId, dispatch), "return dispatch requires scheduled date");
        await Denied(s => s.ScheduleReturnAsync(strangerId, operationId, schedule), "return schedule checks organization ownership");
        await Denied(s => s.ScheduleReturnAsync(recyclerId, operationId, schedule with { ExpectedReturnDate = VietnamTime.Today.AddDays(-1) }), "past return date rejected");
        await action(s => s.ScheduleReturnAsync(recyclerId, operationId, schedule));
        await action(s => s.ScheduleReturnAsync(recyclerId, operationId, schedule with { ExpectedReturnDate = VietnamTime.Today.AddDays(2) }));
        await action(s => s.DispatchReturnAsync(recyclerId, operationId, dispatch));
        await Denied(s => s.DispatchReturnAsync(recyclerId, operationId, dispatch), "duplicate return dispatch rejected");
        var shift = new Shift { Id = Guid.NewGuid(), WarehouseId = warehouseId, ShiftName = "Return shift", ShiftDate = VietnamTime.Today, StartTime = TimeSpan.Zero, EndTime = new TimeSpan(23, 59, 59), Status = "InProgress" };
        var recycledArea = new WarehouseArea { Id = Guid.NewGuid(), WarehouseId = warehouseId, AreaName = "Recycled staging", AreaType = "Recycled", CapacityKg = 100 };
        var recycledGroup = new AreaGroup { Id = Guid.NewGuid(), Area = recycledArea, GroupName = "RC", CapacityKg = 100 };
        var recycledLocation = new StorageLocation { Id = Guid.NewGuid(), WarehouseId = warehouseId, Area = recycledArea, AreaGroup = recycledGroup, LocationCode = "RC-01", CapacityKg = 1 };
        var unclassifiedArea = new WarehouseArea { Id = Guid.NewGuid(), WarehouseId = warehouseId, AreaName = "Unclassified", AreaType = "Unclassified", CapacityKg = 100 };
        var unclassifiedGroup = new AreaGroup { Id = Guid.NewGuid(), Area = unclassifiedArea, GroupName = "UC", CapacityKg = 100 };
        var classificationStaff = new User { Id = Guid.NewGuid(), UserName = "Classifier", FullName = "Classifier", WarehouseId = warehouseId, Role = new Role { Id = Guid.NewGuid(), RoleName = "ClassificationStaff" } };
        var team = new OperationalTeam { Id = Guid.NewGuid(), Shift = shift, TeamName = "Reclassification team", TeamType = "Classification", Status = "InProgress" };
        team.Members.Add(new TeamMember { Id = Guid.NewGuid(), Staff = classificationStaff });
        Guid outputId;
        await using (var db = dbFactory())
        {
            db.AddRange(recycledLocation, unclassifiedGroup, team);
            await db.SaveChangesAsync();
            outputId = await db.ProcessingOperationOutputs.Where(x => x.ProcessingOperationId == operationId).Select(x => x.Id).SingleAsync();
        }
        var receipt = new ReceiveRecyclingReturnDto(shift.Id, [new(outputId, recycledLocation.Id, 1, 2, null)]);
        await Denied(s => s.ReceiveReturnAsync(wrongStaffId, operationId, receipt), "return receipt checks warehouse ownership");
        await Denied(s => s.ReceiveReturnAsync(staffId, operationId, receipt), "return receipt enforces location capacity");
        await using (var db = dbFactory())
        {
            Check(!await db.IntakeBatches.AnyAsync(), "failed return receipt leaves no partial batch");
            (await db.StorageLocations.SingleAsync(x => x.Id == recycledLocation.Id)).CapacityKg = 100;
            await db.SaveChangesAsync();
        }
        await Denied(s => s.ReceiveReturnAsync(staffId, operationId, receipt with { Batches = [new(outputId, recycledLocation.Id, 2, 2, null)] }), "return discrepancy requires reason");
        await action(async s => { var options = await s.ReturnReceiptOptionsAsync(staffId, operationId); Check(options.Locations.Count == 1 && options.Shifts.Count == 1, "receipt options only recycled locations and current shifts"); });
        await action(s => s.ReceiveReturnAsync(staffId, operationId, receipt));
        await Denied(s => s.ReceiveReturnAsync(staffId, operationId, receipt), "duplicate return receipt rejected");
        Guid intakeId;
        await using (var db = dbFactory())
        {
            var intake = await db.IntakeBatches.SingleAsync(); intakeId = intake.Id;
            Check(intake.ProcessingOperationOutputId == outputId && intake.Status == "AwaitingClassificationAssignment", "return creates intake with traceable source and assignment status");
            Check(await db.Inventories.CountAsync() == 4, "return does not create available inventory before classification");
            Check((await db.StorageLocations.SingleAsync(x => x.Id == recycledLocation.Id)).CurrentWeightKg == 2, "return occupies recycled staging exactly once");
            var service = new ClassificationOperationsService(db);
            var board = await service.GetManagementBoardAsync(warehouseId, null);
            Check(board.Batches.Any(x => x.Id == intakeId), "returned batch appears on existing manager board");
            await service.AssignBatchAsync(managerId, intakeId, team.Id);
        }
        await using (var db = dbFactory())
        {
            var service = new ClassificationOperationsService(db);
            Check((await service.GetBatchesAsync(classificationStaff.Id)).Any(x => x.Id == intakeId), "assigned return appears for classification staff");
            await service.ConfirmReceiptAsync(classificationStaff.Id, intakeId);
            Check((await db.StorageLocations.SingleAsync(x => x.Id == recycledLocation.Id)).CurrentWeightKg == 0
                && (await db.WarehouseAreas.SingleAsync(x => x.Id == recycledArea.Id)).CurrentKg == 0, "classification receipt releases recycled staging capacity");
            foreach (var invalidWeight in new[] { 0m, -1m, 2.001m, 3m })
            {
                var countRejected = false;
                try { await service.CountBatchAsync(classificationStaff.Id, intakeId, new() { ItemCount = 1, TotalWeightKg = invalidWeight }); }
                catch (InvalidOperationException) { countRejected = true; }
                Check(countRejected, $"classification rejects invalid counted weight {invalidWeight}");
                var unchanged = await db.IntakeBatches.AsNoTracking().SingleAsync(x => x.Id == intakeId);
                Check(unchanged.Status == "AwaitingClassificationCount" && unchanged.CountedTotalWeight == null,
                    "invalid count does not update the batch");
            }
            await service.CountBatchAsync(classificationStaff.Id, intakeId, new() { ItemCount = 1, TotalWeightKg = 2 });
            await service.StartBatchAsync(classificationStaff.Id, intakeId);
            Category Cat(string type, string code) => new() { Id = Guid.NewGuid(), Type = type, Code = code, Name = code };
            var fabric = Cat("FabricType", "COTTON"); var group = Cat("GarmentGroup", "TOP");
            var clothing = Cat("ClothingType", "SHIRT"); clothing.ParentId = group.Id;
            var gender = Cat("Gender", "MALE"); var target = Cat("TargetUser", "TARGET_ADULT"); var size = Cat("Size", "M");
            var grade = Cat("ConditionGrade", "GRADE_A");
            db.AddRange(fabric, group, clothing, gender, target, size, grade, Cat("ConditionGrade", "GRADE_B"), Cat("ConditionGrade", "GRADE_C"));
            var question = new ConditionQuestion { Id = Guid.NewGuid(), QuestionText = "Fabric", Weight = 35 };
            var answer = new ConditionAnswer { Id = Guid.NewGuid(), ConditionQuestion = question, AnswerText = "Intact", ConditionRating = 1 };
            db.Add(answer);
            await db.SaveChangesAsync();
            var item = await service.ClassifyItemAsync(classificationStaff.Id, intakeId, new() { FabricTypeId = fabric.Id, GarmentGroupId = group.Id, ClothingTypeId = clothing.Id, GenderId = gender.Id, TargetUserId = target.Id, SizeId = size.Id, Answers = [new(question.Id, answer.Id)] });
            Check(item.WeightedScore == 100 && item.ScoringSnapshot != null, "classification persists weighted score and evidence");
            var snapshot = item.ScoringSnapshot;
            question.Weight = 70;
            await db.SaveChangesAsync();
            var persisted = await db.ClassifiedItems.AsNoTracking().SingleAsync(x => x.Id == item.Id);
            Check(persisted.WeightedScore == 100 && persisted.ScoringSnapshot == snapshot && snapshot!.Contains("\"Weight\":35"), "criteria edits preserve historical scoring evidence");
            Check(item.ConditionGrade == "A" && item.ProcessingDirection == "Charity", "recycled clothing receives fresh grade and existing routing");
            var oldShift = new Shift { Id = Guid.NewGuid(), WarehouseId = warehouseId, ShiftName = "Previous day", ShiftDate = VietnamTime.Today.AddDays(-1), EndTime = new TimeSpan(23,59,59), Status = "Completed" };
            var oldTeam = await db.OperationalTeams.SingleAsync(x => x.Id == team.Id);
            db.Shifts.Add(oldShift);
            oldTeam.Shift = oldShift;
            oldTeam.Status = "Completed";
            var nextTeam = new OperationalTeam { Id = Guid.NewGuid(), ShiftId = shift.Id, TeamName = "Current team", TeamType = "Classification", Status = "InProgress" };
            nextTeam.Members.Add(new TeamMember { Id = Guid.NewGuid(), StaffId = classificationStaff.Id });
            db.Add(nextTeam);
            await db.SaveChangesAsync();
            Check((await service.GetCurrentTeamsAsync(classificationStaff.Id)).Any(x => x.Id == nextTeam.Id), "current shift listed even without newly assigned batches");
            var oldShiftBlocked = false;
            try { await service.CompleteBatchAsync(classificationStaff.Id, intakeId); }
            catch (InvalidOperationException) { oldShiftBlocked = true; }
            Check(oldShiftBlocked, "expired shift cannot mutate classification before carryover");
            var foreignResumeBlocked = false;
            try { await service.ResumeBatchAsync(staffId, intakeId, nextTeam.Id); }
            catch (InvalidOperationException) { foreignResumeBlocked = true; }
            Check(foreignResumeBlocked, "carryover requires membership in source and destination teams");
            await service.ResumeBatchAsync(classificationStaff.Id, intakeId, nextTeam.Id);
            var resumed = await db.IntakeBatches.SingleAsync(x => x.Id == intakeId);
            Check(resumed.Status == "Classifying" && resumed.CountedItemCount == 1 && resumed.ClassificationTeamId == nextTeam.Id
                && await db.ClassifiedItems.CountAsync(x => x.BatchId == intakeId) == 1,
                "carryover preserves counted quantity and classified items");
            Check(await db.ClassificationBatchTransfers.AnyAsync(x => x.IntakeBatchId == intakeId && x.FromTeamId == team.Id && x.ToTeamId == nextTeam.Id), "carryover records previous team, new team and actor");
            await action(async processing => Check((await processing.GetByIdAsync(managerId, operationId)).Status == "ReturnReceived",
                "assigned return remains in progress until classification is complete"));
            await service.CompleteBatchAsync(classificationStaff.Id, intakeId);
            await action(async processing =>
            {
                Check((await processing.GetByIdAsync(managerId, operationId)).Status == "Completed",
                    "reclassified return is completed in detail, including historical ReturnReceived records");
                Check((await processing.GetListAsync(managerId, "Completed")).Any(x => x.Id == operationId)
                    && !(await processing.GetListAsync(managerId, "ReturnReceived")).Any(x => x.Id == operationId),
                    "processing list and status filters agree with reclassification completion");
            });
            var additionalOutput = new ProcessingOperationOutput { Id = Guid.NewGuid(), ProcessingOperationId = operationId,
                OutputType = "RecycledClothing", Quantity = 1, Weight = 1, IsActive = true };
            db.ProcessingOperationOutputs.Add(additionalOutput);
            await db.SaveChangesAsync();
            await action(async processing => Check((await processing.GetByIdAsync(managerId, operationId)).Status == "ReturnReceived",
                "an output without a classified intake prevents premature completion"));
            additionalOutput.IsActive = false;
            await db.SaveChangesAsync();
            await action(async processing => Check((await processing.GetByIdAsync(managerId, operationId)).Status == "Completed",
                "inactive outputs do not block completion"));
            var scratch = await service.CreateManualBatchAsync(classificationStaff.Id, new(group.Id, gender.Id, target.Id, grade.Id));
            var scratchSummary = (await service.GetGroupedBatchesAsync(classificationStaff.Id, null)).Single(x => x.Id == scratch.Id);
            Check(scratchSummary.GarmentGroupId == group.Id && scratchSummary.GenderId == gender.Id
                && scratchSummary.TargetUserId == target.Id && scratchSummary.ConditionGradeId == grade.Id,
                "batch summaries expose exact category IDs for matching suggestions");
            var gradeB = await db.Categories.SingleAsync(x => x.Code == "GRADE_B");
            await service.UpdateManualBatchAsync(classificationStaff.Id, scratch.Id, new(group.Id, gender.Id, target.Id, gradeB.Id));
            Check((await service.GetGroupedBatchAsync(classificationStaff.Id, scratch.Id))!.ProcessingDirection == "Recycling", "editing empty manual batch updates grade and processing direction");
            await service.UpdateManualBatchAsync(classificationStaff.Id, scratch.Id, new(group.Id, gender.Id, target.Id, grade.Id));
            await service.AssignItemsAsync(classificationStaff.Id, scratch.Id, [item.Id]);
            Check(!(await service.GetBatchesAsync(classificationStaff.Id)).Any(x => x.Id == intakeId),
                "fully grouped intake disappears from classified queue even while group is draft");
            Check(await service.GetBatchAsync(classificationStaff.Id, intakeId) != null,
                "fully grouped intake remains accessible for traceability");
            await service.RemoveItemAsync(classificationStaff.Id, scratch.Id, item.Id);
            Check((await service.GetBatchesAsync(classificationStaff.Id)).Any(x => x.Id == intakeId),
                "intake returns to classified queue when an item is removed from its group");
            await service.AssignItemsAsync(classificationStaff.Id, scratch.Id, [item.Id]);
            var rejected = false;
            try { await service.UpdateManualBatchAsync(classificationStaff.Id, scratch.Id, new(group.Id, gender.Id, target.Id, gradeB.Id)); }
            catch (InvalidOperationException) { rejected = true; }
            Check(rejected, "cannot edit batch attributes to mismatch existing items");
            foreach (var invalidWeight in new[] { -1m, 0m, 9.99m, 10.001m })
            {
                var weightRejected = false;
                try { await service.FinalizeManualBatchAsync(classificationStaff.Id, scratch.Id, new(invalidWeight)); }
                catch (InvalidOperationException) { weightRejected = true; }
                var draft = await db.ClassifiedBatches.AsNoTracking().SingleAsync(x => x.Id == scratch.Id);
                Check(weightRejected && draft.Status == "Draft" && draft.TotalWeight == 0,
                    $"finalizing rejects {invalidWeight} kg without changing the draft");
            }
            await service.FinalizeManualBatchAsync(classificationStaff.Id, scratch.Id, new(10.01m));
            Check((await db.ClassifiedBatches.AsNoTracking().SingleAsync(x => x.Id == scratch.Id)).TotalWeight == 10.01m,
                "finalizing persists fractional weight above minimum");
            await service.DeleteManualBatchAsync(classificationStaff.Id, scratch.Id);
            Check((await service.GetBatchesAsync(classificationStaff.Id)).Any(x => x.Id == intakeId),
                "deleting a grouped batch restores its intake to the classified queue");
            Check((await db.ClassifiedBatches.SingleAsync(x => x.Id == scratch.Id)).IsActive == false
                && (await db.ClassifiedItems.SingleAsync(x => x.Id == item.Id)).ClassifiedBatchId == null,
                "delete unplaced batch soft deletes grouping and releases classified items");
            var grouped = await service.CreateManualBatchAsync(classificationStaff.Id, new(group.Id, gender.Id, target.Id, grade.Id));
            await service.AssignItemsAsync(classificationStaff.Id, grouped.Id, [item.Id]);
            await service.FinalizeManualBatchAsync(classificationStaff.Id, grouped.Id, new(10));
            Check((await db.ClassifiedBatches.SingleAsync(x => x.Id == grouped.Id)).Status == "ReadyForPlacement", "reclassified output enters existing grouped batch putaway flow");
            Check(await db.ClassifiedItems.AnyAsync(x => x.ClassifiedBatchId == grouped.Id && x.Batch.ProcessingOperationOutputId == outputId), "new classified batch retains recycling provenance through intake items");
            var classifiedArea = new WarehouseArea { Id = Guid.NewGuid(), WarehouseId = warehouseId, AreaType = "Classified", AreaName = "Classified staging", CapacityKg = 100 };
            var classifiedGroup = new AreaGroup { Id = Guid.NewGuid(), Area = classifiedArea, GroupName = "Classified aisle", CapacityKg = 100 };
            var classifiedLocation = new StorageLocation { Id = Guid.NewGuid(), WarehouseId = warehouseId, Area = classifiedArea, AreaGroup = classifiedGroup, LocationCode = "CLASSIFIED-01", CapacityKg = 100 };
            var storageArea = new WarehouseArea { Id = Guid.NewGuid(), WarehouseId = warehouseId, AreaType = "Storage", AreaName = "Charity", CapacityKg = 100 };
            var storageLocation = new StorageLocation { Id = Guid.NewGuid(), WarehouseId = warehouseId, Area = storageArea, LocationCode = "CHARITY-01", CapacityKg = 100, PreferredProcessingDirection = "Charity" };
            db.AddRange(classifiedLocation, storageLocation);
            await db.SaveChangesAsync();
            var changedWeightRejected = false;
            try { await service.PlaceGroupedBatchAsync(classificationStaff.Id, grouped.Id, new(classifiedArea.Id, classifiedGroup.Id, classifiedLocation.Id, 2)); }
            catch (InvalidOperationException) { changedWeightRejected = true; }
            Check(changedWeightRejected && classifiedLocation.CurrentWeightKg == 0, "placement cannot overwrite confirmed batch weight");
            await service.PlaceGroupedBatchAsync(classificationStaff.Id, grouped.Id, new(classifiedArea.Id, classifiedGroup.Id, classifiedLocation.Id, 10));
            Check(classifiedLocation.CurrentWeightKg == 10, "placement uses finalized weight for capacity");
            (await db.ClassifiedBatches.SingleAsync(x => x.Id == grouped.Id)).ClassificationDate = VietnamTime.Today.AddDays(-10);
            await db.SaveChangesAsync();
            var occupiedLayout = await service.GetClassificationAreaLayoutAsync(classificationStaff.Id, VietnamTime.Today);
            Check(occupiedLayout.Areas.SelectMany(x => x.Groups).SelectMany(x => x.Batches).Any(x => x.Id == grouped.Id),
                "physical layout includes older placed batches even with today's date filter");
            var deleteBlocked = false;
            try { await service.DeleteManualBatchAsync(classificationStaff.Id, grouped.Id); }
            catch (InvalidOperationException) { deleteBlocked = true; }
            Check(deleteBlocked, "cannot delete a batch already placed in classified area");
            await service.SendGroupedBatchToWarehouseAsync(classificationStaff.Id, grouped.Id);
            var warehouse = new WarehouseOperationsService(db);
            await warehouse.ConfirmReceiptAsync(staffId, grouped.Id, new(10, 1, true, null));
            await warehouse.PutawayAsync(staffId, grouped.Id, new(storageLocation.Id, null));
            var fullList = await warehouse.GetInboundBatchesAsync(staffId, warehouseId);
            var summaries = await warehouse.GetInboundBatchesAsync(staffId, warehouseId, false);
            Check(fullList.Single(x => x.Id == grouped.Id).Items.Count == 1
                && summaries.All(x => x.Items.Count == 0)
                && fullList.Select(x => x with { Items = Array.Empty<ClassificationItemDto>() })
                    .Select(x => System.Text.Json.JsonSerializer.Serialize(x))
                    .SequenceEqual(summaries.Select(x => System.Text.Json.JsonSerializer.Serialize(x))),
                "warehouse summary omits item payload while preserving batch metadata and order");
            var dashboard = await warehouse.GetDashboardAsync(staffId, warehouseId);
            var stockRows = await db.Inventories.AsNoTracking().Where(x => x.WarehouseId == warehouseId && x.IsActive != false).ToListAsync();
            Check(dashboard.StoredBatches == fullList.Count(x => x.Status == "Stored")
                && dashboard.AvailableQuantity == stockRows.Sum(x => Math.Max(0, x.Quantity - x.ReservedQuantity))
                && dashboard.AvailableWeightKg == stockRows.Sum(x => Math.Max(0, x.TotalWeight - x.ReservedWeight))
                && dashboard.InventorySkuCount == stockRows.Count(x => x.TotalWeight > x.ReservedWeight),
                "SQL dashboard aggregates preserve stored, available and reserved stock counts");
            var inventory = await db.Inventories.SingleAsync(x => x.ClassifiedBatchId == grouped.Id);
            Check(inventory.Status == "Available" && inventory.ConditionRating == 1 && inventory.Quantity == 1
                && inventory.TotalWeight == 10 && inventory.ProcessingDirection == "Charity", "recycled return finishes old classification/warehouse flow as fresh available inventory");
        }
    }
}
