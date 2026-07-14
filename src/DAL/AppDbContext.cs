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
        public DbSet<ClassifiedItem> ClassifiedItems => Set<ClassifiedItem>();
        public DbSet<ClassifiedBatch> ClassifiedBatches => Set<ClassifiedBatch>();
        public DbSet<ClassificationCriteria> ClassificationCriteria => Set<ClassificationCriteria>();
        public DbSet<ClassificationCriteriaOption> ClassificationCriteriaOptions => Set<ClassificationCriteriaOption>();
        public DbSet<ClassificationResult> ClassificationResults => Set<ClassificationResult>();
        public DbSet<ConditionQuestion> ConditionQuestions => Set<ConditionQuestion>();
        public DbSet<ConditionAnswer> ConditionAnswers => Set<ConditionAnswer>();
        public DbSet<InspectionAnswer> InspectionAnswers => Set<InspectionAnswer>();
        public DbSet<DistributionRequest> DistributionRequests => Set<DistributionRequest>();
        public DbSet<DistributionItem> DistributionItems => Set<DistributionItem>();
        public DbSet<DonationRequest> DonationRequests => Set<DonationRequest>();
        public DbSet<IntakeBatch> IntakeBatches => Set<IntakeBatch>();
        public DbSet<IntakeBatchDonationRequest> IntakeBatchDonationRequests => Set<IntakeBatchDonationRequest>();
        public DbSet<Inventory> Inventories => Set<Inventory>();
        public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();
        public DbSet<TransactionItem> TransactionItems => Set<TransactionItem>();
        public DbSet<PickupAssignment> PickupAssignments => Set<PickupAssignment>();
        public DbSet<Voucher> Vouchers => Set<Voucher>();
        public DbSet<Warehouse> Warehouses => Set<Warehouse>();
        public DbSet<WarehouseArea> WarehouseAreas => Set<WarehouseArea>();
        public DbSet<AreaGroup> AreaGroups => Set<AreaGroup>();
        public DbSet<Shift> Shifts => Set<Shift>();
        public DbSet<OperationalTeam> OperationalTeams => Set<OperationalTeam>();
        public DbSet<TeamMember> TeamMembers => Set<TeamMember>();
        public DbSet<TransferRequest> TransferRequests => Set<TransferRequest>();
        public DbSet<TransferItem> TransferItems => Set<TransferItem>();
        public DbSet<Profile> Profiles => Set<Profile>();
        public DbSet<ProfileDetail> ProfileDetails => Set<ProfileDetail>();

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

            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = Guid.Parse("85555555-5555-5555-5555-555555555555"),
                    FullName = "Receiving Staff Demo",
                    UserName = "receiving.staff",
                    Email = "receiving.staff@rethreads.local",
                    PhoneNumber = "0900000001",
                    Address = "Ho Chi Minh City",
                    PasswordHash = "$2a$11$TCC0aSnsg3xBXrySfOn18OsY5Bme6jTvPnd6kVhAfR/XJIFODASVa",
                    RoleId = RoleSeedData.ReceivingStaffId,
                    UserStatus = "Active",
                    IsActive = true,
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

            modelBuilder.Entity<IntakeBatchDonationRequest>()
                .HasKey(x => new { x.IntakeBatchId, x.DonationRequestId });

            modelBuilder.Entity<IntakeBatchDonationRequest>()
                .HasOne(x => x.AddedByStaff)
                .WithMany()
                .HasForeignKey(x => x.AddedByStaffId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<IntakeBatch>()
                .HasOne(x => x.Shift)
                .WithOne(x => x.IntakeBatch)
                .HasForeignKey<IntakeBatch>(x => x.ShiftId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<IntakeBatch>()
                .HasOne(x => x.ReceivingTeam)
                .WithMany(x => x.IntakeBatches)
                .HasForeignKey(x => x.ReceivingTeamId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<TeamMember>()
                .HasIndex(x => new { x.TeamId, x.StaffId })
                .IsUnique();

            modelBuilder.Entity<PickupAssignment>()
                .HasIndex(x => x.DonorRequestId)
                .IsUnique(false);

            modelBuilder.Entity<ClassifiedBatch>()
                .HasOne(x => x.Area)
                .WithMany()
                .HasForeignKey(x => x.AreaId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ClassifiedBatch>()
                .HasOne(x => x.Group)
                .WithMany()
                .HasForeignKey(x => x.GroupId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ClassificationResult>()
                .HasOne(x => x.Criteria)
                .WithMany(x => x.Results)
                .HasForeignKey(x => x.CriteriaId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ClassificationResult>()
                .HasOne(x => x.Option)
                .WithMany(x => x.Results)
                .HasForeignKey(x => x.OptionId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<InspectionAnswer>()
                .HasOne(x => x.ConditionQuestion)
                .WithMany(x => x.InspectionAnswers)
                .HasForeignKey(x => x.ConditionQuestionId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<InspectionAnswer>()
                .HasOne(x => x.ConditionAnswer)
                .WithMany(x => x.InspectionAnswers)
                .HasForeignKey(x => x.ConditionAnswerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TransferRequest>()
                .HasOne(x => x.FromArea)
                .WithMany()
                .HasForeignKey(x => x.FromAreaId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TransferRequest>()
                .HasOne(x => x.ToArea)
                .WithMany()
                .HasForeignKey(x => x.ToAreaId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TransferItem>()
                .HasOne(x => x.ToArea)
                .WithMany()
                .HasForeignKey(x => x.ToAreaId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TransferItem>()
                .HasOne(x => x.RequestStaff)
                .WithMany()
                .HasForeignKey(x => x.RequestStaffId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TransferItem>()
                .HasOne(x => x.ApproveStaff)
                .WithMany()
                .HasForeignKey(x => x.ApproveStaffId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DistributionRequest>()
                .HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            foreach (var property in modelBuilder.Model.GetEntityTypes()
                         .SelectMany(entity => entity.GetProperties())
                         .Where(property => property.ClrType == typeof(decimal)
                                            || property.ClrType == typeof(decimal?)))
            {
                property.SetPrecision(18);
                property.SetScale(2);
            }

            // Operational records are soft-deleted. Database cascades could erase
            // inventory and audit history and also create multiple cascade paths.
            foreach (var foreignKey in modelBuilder.Model.GetEntityTypes()
                         .SelectMany(entity => entity.GetForeignKeys()))
            {
                foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
            }
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
