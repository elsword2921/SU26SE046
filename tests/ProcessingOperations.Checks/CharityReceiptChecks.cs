using BLL.Services.Implements.DistributionOperations;
using DAL;
using DAL.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

internal static class CharityReceiptChecks
{
    public static async Task Run(Func<AppDbContext> dbFactory, Guid warehouseId, Guid managerId)
    {
        var charityId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        await using (var db = dbFactory())
        {
            var role = await db.Roles.SingleAsync(x => x.RoleName == "CharityOrganization");
            foreach (var id in new[] { charityId, otherId })
                db.Users.Add(new User { Id = id, RoleId = role.Id, UserName = id.ToString(), Email = $"{id}@example.invalid",
                    FullName = "Receipt test", PasswordHash = "unused", UserStatus = "Active", EmailConfirmed = true });
            db.DistributionRequests.Add(new DistributionRequest { Id = requestId, UserId = charityId,
                WarehouseId = warehouseId, RequestCode = "DIST-RECEIPT-TEST", Status = "GhnBooked",
                RequestedAt = DateTime.UtcNow, GhnOrderCode = "TEST-RECEIPT", GhnStatus = "ready_to_pick" });
            await db.SaveChangesAsync();
        }
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Ghn:Token"] = "test-token" }).Build();
        using var client = new HttpClient(new FakeGhnHandler { Status = "picking" }) { BaseAddress = new Uri("https://ghn.invalid/") };
        client.DefaultRequestHeaders.Add("ShopId", "123");
        async Task<bool> Receive(Guid actor)
        {
            await using var db = dbFactory();
            try { await new DistributionOperationsService(db, client, config).ConfirmReceiptAsync(actor, requestId); return true; }
            catch (InvalidOperationException) { return false; }
            catch (UnauthorizedAccessException) { return false; }
        }
        void Check(bool condition, string label) { if (!condition) throw new Exception("FAIL: " + label); Console.WriteLine("PASS: " + label); }
        Check(!await Receive(charityId), "charity cannot receive before warehouse issue");
        await using (var db = dbFactory())
        {
            var request = await db.DistributionRequests.SingleAsync(x => x.Id == requestId);
            request.WarehouseIssuedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }
        Check(!await Receive(otherId) && !await Receive(managerId), "charity receipt enforces ownership and role");
        var results = await Task.WhenAll(Receive(charityId), Receive(charityId));
        Check(results.Count(x => x) == 1, "simultaneous charity confirmations succeed exactly once");
        await using (var db = dbFactory())
        {
            var service = new DistributionOperationsService(db, client, config);
            await service.RefreshGhnAsync(charityId, requestId);
            var request = await db.DistributionRequests.AsNoTracking().SingleAsync(x => x.Id == requestId);
            Check(request.Status == "OrganizationReceived" && request.ActualDeliveryTime != null && request.UpdatedBy == charityId,
                "GHN refresh preserves charity manual receipt and audit");
            Check(await db.ShipmentStatusHistories.CountAsync(x => x.DistributionRequestId == requestId && x.Source == "Organization") == 1,
                "charity receipt records a single organization history event");
        }
    }
}
