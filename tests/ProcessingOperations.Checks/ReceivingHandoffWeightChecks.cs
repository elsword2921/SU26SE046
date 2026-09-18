using BLL.Services.Implements.ReceivingOperations;
using DAL;
using DAL.Models;
using Microsoft.EntityFrameworkCore;

internal static class ReceivingHandoffWeightChecks
{
    public static async Task Run()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(
            $"Server=(localdb)\\MSSQLLocalDB;Database=ReceivingWeight_{Guid.NewGuid():N};Integrated Security=true;TrustServerCertificate=true").Options;
        await using var db = new AppDbContext(options);
        void Check(bool ok, string label) { if (!ok) throw new Exception(label); Console.WriteLine("PASS: " + label); }
        try
        {
            await db.Database.EnsureCreatedAsync();
            var role = await db.Roles.SingleAsync(r => r.RoleName == "ReceivingStaff");
            var staff = new User { Id = Guid.NewGuid(), RoleId = role.Id, UserName = "handoff", Email = "handoff@example.test" };
            var warehouse = new Warehouse { Id = Guid.NewGuid(), WarehouseName = "Test" };
            var shift = new Shift { Id = Guid.NewGuid(), Warehouse = warehouse, ShiftName = "Test", ShiftDate = DateTime.Today };
            var team = new OperationalTeam { Id = Guid.NewGuid(), Shift = shift, TeamName = "Test", TeamType = "ReceivingPickup" };
            team.Members.Add(new TeamMember { Id = Guid.NewGuid(), Staff = staff });
            var area = new WarehouseArea { Id = Guid.NewGuid(), Warehouse = warehouse, AreaName = "Receiving", AreaType = "Receiving", CurrentKg = 100, CapacityKg = 200 };
            var group = new AreaGroup { Id = Guid.NewGuid(), Area = area, GroupName = "A", CurrentKg = 100, CapacityKg = 200 };
            var location = new StorageLocation { Id = Guid.NewGuid(), Area = area, AreaGroup = group, Warehouse = warehouse, LocationCode = "A1", CurrentWeightKg = 100, CapacityKg = 200 };
            var request = new DonationRequest { Id = Guid.NewGuid(), Donor = staff, Warehouse = warehouse, RequestCode = "WEIGHT", EstimateWeight = 50 };
            var batch = new IntakeBatch { Id = Guid.NewGuid(), BatchCode = "WEIGHT", Warehouse = warehouse, Shift = shift, ReceivingTeam = team, IntakeDate = DateTime.Today, Status = "ReceivedAtWarehouse", CurrentArea = area, CurrentAreaGroup = group, CurrentStorageLocation = location };
            batch.IntakeBatchDonationRequests.Add(new IntakeBatchDonationRequest { Id = Guid.NewGuid(), DonationRequest = request, AddedByStaff = staff, AddedAt = DateTime.UtcNow });
            db.Add(batch);
            await db.SaveChangesAsync();
            var service = new ReceivingOperationsService(db);
            foreach (var weight in new[] { -1m, 0m, 0.01m, 3m, 9.99m, 10m, 10.01m })
            {
                batch.TotalWeight = weight;
                batch.Status = "ReceivedAtWarehouse";
                batch.SentToClassificationAt = null;
                batch.CurrentArea = area; batch.CurrentAreaGroup = group; batch.CurrentStorageLocation = location;
                area.CurrentKg = group.CurrentKg = location.CurrentWeightKg = 100;
                await db.SaveChangesAsync();
                var notificationsBefore = await db.Notifications.CountAsync();
                if (weight <= 0)
                {
                    try { await service.SendToClassificationAsync(staff.Id, batch.Id); throw new Exception("Underweight handoff accepted"); }
                    catch (InvalidOperationException e) { Check(e.Message.Contains("0 kg"), $"handoff rejects non-positive {weight} kg"); }
                    Check(batch.Status == "ReceivedAtWarehouse" && batch.CurrentStorageLocationId == location.Id
                        && batch.SentToClassificationAt == null && area.CurrentKg == 100 && group.CurrentKg == 100 && location.CurrentWeightKg == 100
                        && await db.Notifications.CountAsync() == notificationsBefore, "rejected handoff leaves status, location, capacity and notifications unchanged");
                }
                else
                {
                    await service.SendToClassificationAsync(staff.Id, batch.Id);
                    Check(batch.Status == "AwaitingClassificationAssignment" && batch.CurrentStorageLocationId == null
                        && batch.SentToClassificationAt.HasValue && location.CurrentWeightKg == 100 - weight,
                        $"handoff accepts {weight} kg and releases receiving capacity");
                }
            }
            foreach (var invalidWeight in new[] { -1m, 0m, 50.01m, 100000m, 1.234m })
            {
                var dto = new BLL.DTOs.ConfirmPickupDto(invalidWeight, null, null);
                foreach (var action in new Func<Task>[] {
                    () => service.ConfirmPickupAsync(staff.Id, batch.Id, request.Id, dto),
                    () => service.ConfirmWarehouseDropOffAsync(staff.Id, request.Id, dto) })
                {
                    try { await action(); throw new Exception("Invalid received weight accepted"); }
                    catch (InvalidOperationException e) { Check(e.Message.Contains("50 kg"), $"receipt API rejects {invalidWeight} kg before processing"); }
                }
            }
            batch.Status = "Receiving"; batch.TotalWeight = 0;
            // Actual receipt must stay truthful even above the planning limit.
            team.MaxReceivingWeightKg = 1;
            db.DonationPointRules.Add(new DonationPointRule { Id = Guid.NewGuid(), PointsPerKg = 1 });
            team.Status = shift.Status = "InProgress";
            var receiptRequests = new List<DonationRequest>();
            foreach (var value in new[] { 0.01m, 50m })
            {
                var receiptRequest = new DonationRequest { Id = Guid.NewGuid(), Donor = staff, Warehouse = warehouse, RequestCode = $"RECEIPT-{value}", EstimateWeight = value };
                db.Add(new PickupAssignment { Id = Guid.NewGuid(), IntakeBatch = batch, DonorRequest = receiptRequest, Team = team, Shift = shift, Status = "Pending" });
                receiptRequests.Add(receiptRequest);
            }
            await db.SaveChangesAsync();
            foreach (var receiptRequest in receiptRequests)
            {
                await service.ConfirmPickupAsync(staff.Id, batch.Id, receiptRequest.Id, new BLL.DTOs.ConfirmPickupDto(receiptRequest.EstimateWeight, "Verified receipt", null));
                Check(receiptRequest.ActualWeight == receiptRequest.EstimateWeight, $"receipt accepts {receiptRequest.EstimateWeight} kg exactly");
            }
            Check(batch.TotalWeight == 50.01m, "50 kg limit applies per donation, not per intake batch");
        }
        finally { await db.Database.EnsureDeletedAsync(); }
    }
}
