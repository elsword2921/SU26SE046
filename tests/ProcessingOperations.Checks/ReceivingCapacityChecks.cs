using BLL.Common;
using BLL.DTOs;
using BLL.Services.Implements.ReceivingOperations;
using DAL;
using DAL.Models;
using DAL.Models.Enum;
using Microsoft.EntityFrameworkCore;

internal static class ReceivingCapacityChecks
{
    public static async Task Run()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(
            $"Server=(localdb)\\MSSQLLocalDB;Database=ReceivingCapacity_{Guid.NewGuid():N};Integrated Security=true;TrustServerCertificate=true").Options;
        await using var db = new AppDbContext(options);
        void Check(bool value, string label) { if (!value) throw new Exception(label); Console.WriteLine("PASS: " + label); }
        async Task Reject(Func<Task> action, string label)
        {
            try { await action(); } catch (InvalidOperationException) { Console.WriteLine("PASS: " + label); db.ChangeTracker.Clear(); return; }
            throw new Exception("Expected rejection: " + label);
        }
        try
        {
            await db.Database.EnsureCreatedAsync();
            var warehouse = new Warehouse { Id = Guid.NewGuid(), WarehouseName = "Capacity test" };
            var staffRole = await db.Roles.SingleAsync(r => r.RoleName == "ReceivingStaff");
            var donorRole = await db.Roles.SingleAsync(r => r.RoleName == "Donor");
            var donor = new User { Id = Guid.NewGuid(), RoleId = donorRole.Id, UserName = "capacity-donor", Email = "capacity@example.test" };
            var shift = new Shift { Id = Guid.NewGuid(), Warehouse = warehouse, ShiftName = "Tomorrow", ShiftDate = VietnamTime.Now.Date.AddDays(1), StartTime = TimeSpan.FromHours(8), EndTime = TimeSpan.FromHours(12) };
            OperationalTeam Team(string name) {
                var t = new OperationalTeam { Id = Guid.NewGuid(), Shift = shift, TeamName = name, TeamType = "ReceivingPickup" };
                t.Members.Add(new TeamMember { Id = Guid.NewGuid(), Staff = new User { Id = Guid.NewGuid(), RoleId = staffRole.Id, Warehouse = warehouse, UserName = name, Email = name + "@example.test" } });
                db.Add(t); return t;
            }
            var a = Team("A"); var b = Team("B");
            DonationRequest Request(string code, decimal kg) => new() { Id = Guid.NewGuid(), WarehouseId = warehouse.Id, DonorId = donor.Id, RequestCode = code, EstimateWeight = kg,
                PickupDate = shift.ShiftDate.AddHours(9), DeliveryMethod = "StaffPickup", Status = DonationRequestStatus.WaitingReceivingStaff,
                PickupAddress = "1 Test, District 1", ContactName = "Test", ContactPhoneNumber = "0900000001" };
            db.Add(donor); await db.SaveChangesAsync();
            var requests = Enumerable.Range(1, 6).Select(i => Request($"CAP-{i}", 5)).ToList();
            db.AddRange(requests); await db.SaveChangesAsync();
            var service = new ReceivingOperationsService(db);
            var defaults = await service.GetCapacityBoardAsync(warehouse.Id, shift.ShiftDate);
            Check(defaults.Teams.All(t => t.MaxRequests == 8 && t.MaxWeightKg == 80), "default 8 orders / 80 kg per team shift");
            await Reject(() => service.SetTeamLimitsAsync(a.Id, new(0, 10)), "invalid limits rejected");
            await service.SetTeamLimitsAsync(a.Id, new(2, 10)); await service.SetTeamLimitsAsync(b.Id, new(2, 10));
            var before = await db.Notifications.CountAsync();
            var plan = await service.PreviewPlanAsync(shift.Id);
            Check(plan.Assignments.Count == 4 && plan.Unassigned.Count == 2 && plan.Assignments.GroupBy(x => x.TeamId).All(g => g.Count() == 2), "preview balances orders/kg and reports overflow");
            Check(!await db.PickupAssignments.AnyAsync() && await db.Notifications.CountAsync() == before, "preview has no assignment/notification side effects");
            await service.ApplyPlanAsync(new(shift.Id, plan.Assignments.Select(x => new AssignDonationRequestDto(x.RequestId, x.TeamId)).ToList()));
            var loads = (await service.GetCapacityBoardAsync(warehouse.Id, shift.ShiftDate)).Teams;
            Check(loads.All(t => t.AssignedRequests == 2 && t.EstimatedWeightKg == 10), "apply reaches exact limits and includes existing load");
            await Reject(() => service.ApplyPlanAsync(new(shift.Id, plan.Assignments.Select(x => new AssignDonationRequestDto(x.RequestId, x.TeamId)).ToList())), "stale preview cannot duplicate assignments");
            await Reject(() => service.AssignRequestAsync(new(plan.Unassigned[0].RequestId, a.Id)), "manual assignment cannot bypass capacity");
            var assignedA = plan.Assignments.First(x => x.TeamId == a.Id);
            await Reject(() => service.AssignRequestAsync(new(assignedA.RequestId, b.Id)), "moving to a full team is rejected");
            Check(await db.PickupAssignments.CountAsync(x => x.TeamId == a.Id) == 2, "rejected move preserves source load");
            await Reject(() => service.SetTeamLimitsAsync(a.Id, new(1, 9)), "cannot lower limits below assigned load");
            Check(await service.PlanShiftAsync(new(shift.Id, a.Id)) == 0, "legacy plan cannot exceed capacity");
            Check((await service.AutoBalanceShiftAsync(shift.Id)).RequestCount == 0, "legacy auto balance cannot exceed capacity or move existing requests");
            await service.SetWarehouseLimitsAsync(warehouse.Id, new(1, 4));
            await Reject(() => service.SetTeamLimitsAsync(a.Id, null), "reset to lower warehouse default is rejected for loaded team");
            await service.SetWarehouseLimitsAsync(warehouse.Id, new(8, 80));
            await service.SetTeamLimitsAsync(a.Id, null);
            await Reject(() => service.SetWarehouseLimitsAsync(warehouse.Id, new(1, 4)), "warehouse limits cannot invalidate inheriting team load");
            shift = await db.Shifts.Include(s => s.Warehouse).SingleAsync(s => s.Id == shift.Id);
            warehouse = shift.Warehouse;
            var c = Team("C");
            await db.SaveChangesAsync(); await service.SetTeamLimitsAsync(c.Id, new(1, 10));
            async Task<bool> Concurrent(Guid requestId) {
                await using var other = new AppDbContext(options);
                try { await new ReceivingOperationsService(other).AssignRequestAsync(new(requestId, c.Id)); return true; }
                catch (InvalidOperationException) { return false; }
            }
            var results = await Task.WhenAll(plan.Unassigned.Select(r => Concurrent(r.RequestId)));
            Check(results.Count(x => x) == 1 && await db.PickupAssignments.CountAsync(x => x.TeamId == c.Id) == 1, "concurrent assignments never oversubscribe the team");
            var remaining = await db.DonationRequests.Where(r => requests.Select(x => x.Id).Contains(r.Id)
                && !db.PickupAssignments.Any(p => p.DonorRequestId == r.Id)).SingleAsync();
            var another = Request("CAP-ATOMIC", 5); db.Add(another); await db.SaveChangesAsync();
            before = await db.PickupAssignments.CountAsync();
            await Reject(() => service.ApplyPlanAsync(new(shift.Id, [new(remaining.Id, a.Id), new(another.Id, c.Id)])), "over-capacity preview edit rolls back the whole plan");
            Check(await db.PickupAssignments.CountAsync() == before, "no partial assignment after failed apply");
            warehouse = new Warehouse { Id = Guid.NewGuid(), WarehouseName = "Mixed weights" };
            shift = new Shift { Id = Guid.NewGuid(), Warehouse = warehouse, ShiftName = "Mixed", ShiftDate = VietnamTime.Now.Date.AddDays(1), StartTime = TimeSpan.FromHours(8), EndTime = TimeSpan.FromHours(12) };
            var d = Team("D"); var e = Team("E"); await db.SaveChangesAsync();
            var mixed = new[] { 35m, 35m, 5m, 5m, 5m, 5m }.Select((kg, i) => Request($"MIX-{i}", kg)).ToList();
            db.AddRange(mixed); await db.SaveChangesAsync();
            var mixedPlan = await service.PreviewPlanAsync(shift.Id);
            Check(mixedPlan.Assignments.Count == 6 && mixedPlan.Assignments.GroupBy(r => r.TeamId).Count() == 2
                && mixedPlan.Assignments.GroupBy(r => r.TeamId).All(g => g.Count() == 3 && g.Sum(r => r.EstimateWeight) == 45),
                "mixed heavy/light requests balance to 3 orders and 45 kg per team");
            d.Status = "InProgress"; await db.SaveChangesAsync();
            var startedPlan = await service.PreviewPlanAsync(shift.Id);
            Check(startedPlan.Assignments.All(r => r.TeamId == e.Id) && startedPlan.Unassigned.Count > 0,
                "started team is excluded and remaining capacity is enforced");
            await Reject(() => service.AssignRequestAsync(new(mixed[0].Id, d.Id)), "manual assignment cannot target started team");
            var closed = await db.Shifts.SingleAsync(s => s.Id == shift.Id); closed.Status = "Completed"; await db.SaveChangesAsync();
            await Reject(() => service.AssignRequestAsync(new(mixed[0].Id, e.Id)), "closed shift cannot receive assignments even before scheduled end");
            await Reject(() => service.SetTeamLimitsAsync(e.Id, new(8, 80)), "closed shift cannot change receiving limits");
        }
        finally { await db.Database.EnsureDeletedAsync(); }
    }
}
