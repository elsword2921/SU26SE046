using DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace DAL
{
    public class AppDbContext : DbContext
    {
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<User> Users => Set<User>();
        public DbSet<Cart> Carts => Set<Cart>();
        public DbSet<CartItem> CartItems => Set<CartItem>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<ClassificationItem> ClassificationItems => Set<ClassificationItem>();
        public DbSet<DistributionRequest> DistributionRequests => Set<DistributionRequest>();
        public DbSet<DistributionItem> DistributionItems => Set<DistributionItem>();
        public DbSet<DonationRequest> DonationRequests => Set<DonationRequest>();
        public DbSet<IntakeBatch> IntakeBatches => Set<IntakeBatch>();
        public DbSet<Inventory> Inventories => Set<Inventory>();
        public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();
        public DbSet<PickupAssignment> PickupAssignments => Set<PickupAssignment>();
        public DbSet<Voucher> Vouchers => Set<Voucher>();
        public DbSet<Warehouse> Warehouses => Set<Warehouse>();

        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>().HasOne(u => u.Role).WithMany(r => r.Users).HasForeignKey(u => u.RoleId);

            modelBuilder.Entity<Role>().HasData(
                new Role
                {
                    Id = RoleSeedData.DonorId,
                    RoleName     = "Donor",
                    Description = "Individual or organization donating clothes",
                    CreateAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Role
                {
                    Id = RoleSeedData.CharityOrganizationId,
                    RoleName = "CharityOrganization",
                    Description = "Organization receiving donated items for charitable purposes",
                    CreateAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Role
                {
                    Id = RoleSeedData.RecyclingOrganizationId,
                    RoleName = "RecyclingOrganization",
                    Description = "Organization responsible for recycling unusable clothes",
                    CreateAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Role
                {
                    Id = RoleSeedData.ManagerId,
                    RoleName = "Manager",
                    Description = "System manager",
                    CreateAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Role
                {
                    Id = RoleSeedData.ReceivingStaffId,
                    RoleName = "ReceivingStaff",
                    Description = "Staff responsible for receiving donations",
                    CreateAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Role
                {
                    Id = RoleSeedData.ClassificationStaffId,
                    RoleName = "ClassificationStaff",
                    Description = "Staff responsible for classifying clothes",
                    CreateAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Role
                {
                    Id = RoleSeedData.WarehouseStaffId,
                    RoleName = "WarehouseStaff",
                    Description = "Staff responsible for warehouse operations",
                    CreateAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                }
            );
            modelBuilder.Entity<DonationRequest>()
                .Property(x => x.Status)
                .HasConversion<string>();

            modelBuilder.Entity<DonationRequest>()
                .HasOne(x => x.Donor)
                .WithMany(x => x.DonationRequests)
                .HasForeignKey(x => x.DonorId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        private static class RoleSeedData
        {
            public static readonly Guid DonorId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            public static readonly Guid CharityOrganizationId = Guid.Parse("22222222-2222-2222-2222-222222222222");
            public static readonly Guid RecyclingOrganizationId = Guid.Parse("33333333-3333-3333-3333-333333333333");
            public static readonly Guid ManagerId = Guid.Parse("44444444-4444-4444-4444-444444444444");
            public static readonly Guid ReceivingStaffId = Guid.Parse("55555555-5555-5555-5555-555555555555");
            public static readonly Guid ClassificationStaffId = Guid.Parse("66666666-6666-6666-6666-666666666666");
            public static readonly Guid WarehouseStaffId = Guid.Parse("77777777-7777-7777-7777-777777777777");
            public static readonly Guid System = Guid.Parse("00000000-0000-0000-0000-000000000000");
        }
    }
}
