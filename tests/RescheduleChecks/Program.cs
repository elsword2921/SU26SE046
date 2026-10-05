using BLL.Common;
using BLL.DTOs;
using BLL.Services.Implements.ReceivingOperations;
using DAL;
using DAL.Models;
using DAL.Models.Enum;
using Microsoft.EntityFrameworkCore;

await RescheduleChecks.Run();

internal static class RescheduleChecks
{
    public static async Task Run()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(
                $"Server=(localdb)\\MSSQLLocalDB;Database=ReceivingCapacity_{Guid.NewGuid():N};Integrated Security=true;TrustServerCertificate=true"
            )
            .Options;
        await using var db = new AppDbContext(options);
        void Check(bool value, string label)
        {
            if (!value)
                throw new Exception(label);
            Console.WriteLine("PASS: " + label);
        }
        async Task Reject(Func<Task> action, string label)
        {
            try
            {
                await action();
            }
            catch (InvalidOperationException)
            {
                Console.WriteLine("PASS: " + label);
                db.ChangeTracker.Clear();
                return;
            }
            throw new Exception("Expected rejection: " + label);
        }
        try
        {
            await db.Database.EnsureCreatedAsync();
            var warehouse = new Warehouse { Id = Guid.NewGuid(), WarehouseName = "Capacity test" };
            var staffRole = await db.Roles.SingleAsync(r => r.RoleName == "ReceivingStaff");
            var donorRole = await db.Roles.SingleAsync(r => r.RoleName == "Donor");
            var donor = new User
            {
                Id = Guid.NewGuid(),
                RoleId = donorRole.Id,
                UserName = "capacity-donor",
                Email = "capacity@example.test",
            };
            var shift = new Shift
            {
                Id = Guid.NewGuid(),
                Warehouse = warehouse,
                ShiftName = "Tomorrow",
                ShiftDate = VietnamTime.Now.Date.AddDays(1),
                StartTime = TimeSpan.FromHours(8),
                EndTime = TimeSpan.FromHours(12),
            };
            OperationalTeam Team(string name)
            {
                var t = new OperationalTeam
                {
                    Id = Guid.NewGuid(),
                    Shift = shift,
                    TeamName = name,
                    TeamType = "ReceivingPickup",
                };
                t.Members.Add(
                    new TeamMember
                    {
                        Id = Guid.NewGuid(),
                        Staff = new User
                        {
                            Id = Guid.NewGuid(),
                            RoleId = staffRole.Id,
                            Warehouse = warehouse,
                            UserName = name,
                            Email = name + "@example.test",
                        },
                    }
                );
                db.Add(t);
                return t;
            }
            var a = Team("A");
            var b = Team("B");
            DonationRequest Request(string code, decimal kg) =>
                new()
                {
                    Id = Guid.NewGuid(),
                    WarehouseId = warehouse.Id,
                    DonorId = donor.Id,
                    RequestCode = code,
                    EstimateWeight = kg,
                    PickupDate = shift.ShiftDate.AddHours(9),
                    DeliveryMethod = "StaffPickup",
                    Status = DonationRequestStatus.WaitingReceivingStaff,
                    PickupAddress = "1 Test, District 1",
                    ContactName = "Test",
                    ContactPhoneNumber = "0900000001",
                };
            db.Add(donor);
            await db.SaveChangesAsync();
            var request = Request("OVERDUE", 8);
            request.PickupDate = VietnamTime.Today.AddDays(-2).AddHours(16);
            db.Add(request);
            await db.SaveChangesAsync();
            var managerRole = await db.Roles.SingleAsync(r => r.RoleName == "Manager");
            var manager = new User
            {
                Id = Guid.NewGuid(),
                RoleId = managerRole.Id,
                UserName = "manager-test",
            };
            db.Add(manager);
            await db.SaveChangesAsync();
            var oldDate = request.PickupDate;
            var appointment = shift.ShiftDate.AddHours(9);
            var service = new ReceivingOperationsService(db);
            await Reject(
                () => service.AssignRequestAsync(new(request.Id, a.Id)),
                "original assignment still rejects mismatched date"
            );
            await Reject(
                () =>
                    service.RescheduleAndAssignAsync(
                        manager.Id,
                        new(request.Id, a.Id, appointment, false)
                    ),
                "donor confirmation required"
            );
            await Reject(
                () =>
                    service.RescheduleAndAssignAsync(
                        manager.Id,
                        new(request.Id, a.Id, VietnamTime.Today.AddDays(-1), true)
                    ),
                "past appointment rejected"
            );
            await service.SetTeamLimitsAsync(a.Id, new(1, 5));
            await Reject(
                () =>
                    service.RescheduleAndAssignAsync(
                        manager.Id,
                        new(request.Id, a.Id, appointment, true)
                    ),
                "over-capacity reschedule rejected"
            );
            Check(
                (
                    await db.DonationRequests.AsNoTracking().SingleAsync(x => x.Id == request.Id)
                ).PickupDate == oldDate
                    && !await db.PickupAssignments.AnyAsync()
                    && !await db.Notifications.AnyAsync(),
                "failed assignment rolls back appointment and notifications"
            );
            await Reject(
                () =>
                    service.RescheduleAndAssignAsync(
                        manager.Id,
                        new(request.Id, b.Id, shift.ShiftDate.AddHours(14), true)
                    ),
                "wrong shift time rejected"
            );
            await service.RescheduleAndAssignAsync(
                manager.Id,
                new(request.Id, b.Id, appointment, true)
            );
            db.ChangeTracker.Clear();
            Check(
                (await db.DonationRequests.SingleAsync(x => x.Id == request.Id)).PickupDate
                    == appointment,
                "new appointment saved"
            );
            Check(
                (await db.PickupAssignments.SingleAsync()).TeamId == b.Id,
                "assigned to chosen team"
            );
            Check(
                await db.Notifications.CountAsync() == 2,
                "assignment and reschedule notifications recorded"
            );
            await Reject(
                () =>
                    service.RescheduleAndAssignAsync(
                        manager.Id,
                        new(request.Id, b.Id, appointment, true)
                    ),
                "duplicate reschedule rejected"
            );
            Check(
                await db.PickupAssignments.CountAsync() == 1
                    && await db.Notifications.CountAsync() == 2,
                "duplicate has no side effects"
            );
        }
        finally
        {
            await db.Database.EnsureDeletedAsync();
        }
    }
}
