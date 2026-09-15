using BLL.Common;
using BLL.DTOs;
using BLL.Services.Implements.DonorRequestService;
using DAL;
using DAL.Models;
using DAL.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

internal static class DonorRoutingChecks
{
    public static async Task Run()
    {
        var database = "ReThreadsRoutingChecks_" + Guid.NewGuid().ToString("N");
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(
            $"Server=(localdb)\\MSSQLLocalDB;Database={database};Integrated Security=true;TrustServerCertificate=true").Options;
        await using var db = new AppDbContext(options);
        using var http = new HttpClient();
        var service = new DonorRequestService(new UnitOfWork(db), db, http, new ConfigurationBuilder().Build());
        void Check(bool condition, string label) { if (!condition) throw new Exception("FAIL: " + label); Console.WriteLine("PASS: " + label); }
        async Task Reject(Func<Task> action, string label)
        {
            try { await action(); } catch (InvalidOperationException) { Check(true, label); return; }
            throw new Exception("FAIL: expected rejection: " + label);
        }
        try
        {
            await db.Database.EnsureCreatedAsync();
            var near = new Warehouse { Id = Guid.NewGuid(), WarehouseName = "Nearest test warehouse", Address = "123 Nearest address", Latitude = 10.8, Longitude = 106.6, ServiceRadiusKm = 20 };
            var far = new Warehouse { Id = Guid.NewGuid(), WarehouseName = "Further warehouse", Address = "456 Further address", Latitude = 10.9, Longitude = 106.6, ServiceRadiusKm = 20 };
            var inactive = new Warehouse { Id = Guid.NewGuid(), WarehouseName = "Inactive warehouse", Latitude = 10.801, Longitude = 106.6, ServiceRadiusKm = 20, IsActive = false };
            var donorRoleId = await db.Roles.Where(x => x.RoleName == "Donor").Select(x => x.Id).SingleAsync();
            var donor = new User { Id = Guid.NewGuid(), FullName = "Routing donor", UserName = "routing_donor", Email = "routing@example.test", PhoneNumber = "0987654321", RoleId = donorRoleId, EmailConfirmed = true, UserStatus = "Active" };
            db.AddRange(near, far, inactive, donor);
            await db.SaveChangesAsync();
            var preview = await service.GetNearestWarehouseAsync(10.801, 106.6);
            Check(preview.Id == near.Id && preview.WarehouseName == near.WarehouseName && preview.Address == near.Address, "nearest warehouse returns identity and address and excludes inactive warehouses");
            var tomorrow = VietnamTime.Today.AddDays(1);
            Check((await service.GetPickupAvailabilityAsync(tomorrow, 10.801, 106.6, null)).Windows.Count == 0, "warehouse preview remains available without scheduled shifts");
            await Reject(async () => { await service.GetNearestWarehouseAsync(0, 0); }, "out-of-radius address rejected");
            await Reject(async () => { await service.GetNearestWarehouseAsync(double.NaN, 106); }, "non-finite coordinate rejected");
            db.Shifts.Add(new Shift { Id = Guid.NewGuid(), WarehouseId = near.Id, ShiftName = "Routing check", ShiftDate = tomorrow, StartTime = TimeSpan.FromHours(8), EndTime = TimeSpan.FromHours(12), Status = "Scheduled" });
            await db.SaveChangesAsync();
            var request = new CreateDonorRequestDto { ContactName = donor.FullName, ContactPhoneNumber = donor.PhoneNumber, PickupAddress = "Pickup test address", PickupLatitude = 10.801, PickupLongitude = 106.6, PickupDate = tomorrow.AddHours(9), EstimateWeight = 12.35m, Description = "Manual weight check" };
            var id = await service.CreateAsync(donor.Id, request);
            var saved = await db.DonationRequests.SingleAsync(x => x.Id == id);
            Check(saved.WarehouseId == preview.Id && saved.EstimateWeight == 12.35m, "created donation uses previewed warehouse and exact manually entered weight");
            request.EstimateWeight = 50m;
            var maximumId = await service.CreateAsync(donor.Id, request);
            Check((await db.DonationRequests.SingleAsync(x => x.Id == maximumId)).EstimateWeight == 50m, "donation accepts exactly 50 kg");
            foreach (var weight in new[] { 0m, -1m, 1.234m, 50.01m, 100000m })
            {
                request.EstimateWeight = weight;
                await Reject(async () => { await service.CreateAsync(donor.Id, request); }, "invalid donation weight rejected");
                await Reject(() => service.UpdateAsync(donor.Id, id, new UpdateDonorRequestDto { EstimateWeight = weight }), "invalid weight cannot bypass limit through update API");
            }
        }
        finally { await db.Database.EnsureDeletedAsync(); }
    }
}
