using BLL.Services.Implements.ReceivingOperations;
using DAL;
using DAL.Models;
using Microsoft.EntityFrameworkCore;

internal static class ReceivingStageChecks
{
    public static async Task Run()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(
            $"Server=(localdb)\\MSSQLLocalDB;Database=ReceivingStages_{Guid.NewGuid():N};Integrated Security=true;TrustServerCertificate=true").Options;
        await using var db = new AppDbContext(options);
        void Check(bool ok, string label) { if (!ok) throw new Exception(label); Console.WriteLine("PASS: " + label); }
        try
        {
            await db.Database.EnsureCreatedAsync();
            var roleId = await db.Roles.Where(x => x.RoleName == "ReceivingStaff").Select(x => x.Id).SingleAsync();
            var staff = new User { Id = Guid.NewGuid(), RoleId = roleId, UserName = "stage-test", Email = "stage@example.test", FullName = "Stage staff" };
            var warehouse = new Warehouse { Id = Guid.NewGuid(), WarehouseName = "Stage warehouse", Address = "Stage address" };
            var shift = new Shift { Id = Guid.NewGuid(), Warehouse = warehouse, ShiftDate = DateTime.Today, ShiftName = "Stage shift" };
            var statuses = new[] { "Planned", "Receiving", "Completed", "ReceivedAtWarehouse", "AwaitingClassificationAssignment", "AssignedToClassification", "SentToClassification" };
            foreach (var status in statuses)
            {
                var team = new OperationalTeam { Id = Guid.NewGuid(), Shift = shift, TeamName = status, TeamType = "ReceivingPickup" };
                team.Members.Add(new TeamMember { Id = Guid.NewGuid(), Staff = staff });
                var batch = new IntakeBatch { Id = Guid.NewGuid(), BatchCode = status, Status = status, Warehouse = warehouse, Shift = shift, ReceivingTeam = team, IntakeDate = DateTime.Today };
                var request = new DonationRequest { Id = Guid.NewGuid(), Donor = staff, Warehouse = warehouse, RequestCode = status, ContactName = "Contact", ContactPhoneNumber = "0900000001", PickupAddress = "Test", ActualWeight = 5 };
                batch.PickupAssignments.Add(new PickupAssignment { Id = Guid.NewGuid(), DonorRequest = request, Team = team, Shift = shift, RouteOrder = 1, Status = status == "Completed" ? "Received" : "Pending" });
                db.Add(batch);
            }
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
            var service = new ReceivingOperationsService(db);
            Check((await service.GetMyBatchesAsync(staff.Id)).Count == 7, "unfiltered batch API remains compatible");
            foreach (var (stage, expected) in new[] {
                ("receiving", new[] { "Planned", "Receiving" }),
                ("completed", new[] { "Completed" }),
                ("transferring", new[] { "AwaitingClassificationAssignment", "AssignedToClassification", "SentToClassification" }) })
            {
                var batches = await service.GetMyBatchesAsync(staff.Id, stage);
                Check(batches.Select(x => x.Status).Order().SequenceEqual(expected.Order()), $"{stage} only returns matching batches");
                Check(batches.All(x => x.Requests.Count == 1), $"{stage} preserves order details");
            }
            var overview = await service.GetMyOverviewAsync(staff.Id);
            Check(overview.TotalCount == 7 && overview.ProcessedCount == 1 && overview.TotalWeight == 5, "overview preserves aggregate totals across stages");
            Check(overview.Batches.Count == 7 && overview.Batches.All(x => x.Requests.Count == 0 && x.ReceivingGroups.Count == 0 && x.TeamMembers.Count == 1), "overview includes teams without donor details or storage trees");
            var outsider = Guid.NewGuid();
            Check((await service.GetMyBatchesAsync(outsider, "receiving")).Count == 0 && (await service.GetMyOverviewAsync(outsider)).TotalCount == 0, "overview and stage API respect staff membership");
            try { await service.GetMyBatchesAsync(staff.Id, "unknown"); throw new Exception("Invalid stage accepted"); }
            catch (ArgumentException) { Console.WriteLine("PASS: invalid stage rejected"); }
        }
        finally { await db.Database.EnsureDeletedAsync(); }
    }
}
