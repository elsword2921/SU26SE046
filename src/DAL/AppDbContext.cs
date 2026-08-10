using DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace DAL
{
    public class AppDbContext : DbContext
    {
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<User> Users => Set<User>();
        public DbSet<UserVerificationCode> UserVerificationCodes => Set<UserVerificationCode>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<ClassifiedItem> ClassifiedItems => Set<ClassifiedItem>();
        public DbSet<ClassifiedBatch> ClassifiedBatches => Set<ClassifiedBatch>();
        public DbSet<ClassifiedBatchDonationRequest> ClassifiedBatchDonationRequests => Set<ClassifiedBatchDonationRequest>();
        public DbSet<ConditionQuestion> ConditionQuestions => Set<ConditionQuestion>();
        public DbSet<ConditionAnswer> ConditionAnswers => Set<ConditionAnswer>();
        public DbSet<InspectionAnswer> InspectionAnswers => Set<InspectionAnswer>();
        public DbSet<DistributionRequest> DistributionRequests => Set<DistributionRequest>();
        public DbSet<DistributionItem> DistributionItems => Set<DistributionItem>();
        public DbSet<ShipmentStatusHistory> ShipmentStatusHistories => Set<ShipmentStatusHistory>();
        public DbSet<DonationRequest> DonationRequests => Set<DonationRequest>();
        public DbSet<IntakeBatch> IntakeBatches => Set<IntakeBatch>();
        public DbSet<IntakeBatchDonationRequest> IntakeBatchDonationRequests => Set<IntakeBatchDonationRequest>();
        public DbSet<Inventory> Inventories => Set<Inventory>();
        public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();
        public DbSet<TransactionItem> TransactionItems => Set<TransactionItem>();
        public DbSet<PickupAssignment> PickupAssignments => Set<PickupAssignment>();
        public DbSet<Voucher> Vouchers => Set<Voucher>();
        public DbSet<VoucherCode> VoucherCodes => Set<VoucherCode>();
        public DbSet<VoucherRedemption> VoucherRedemptions => Set<VoucherRedemption>();
        public DbSet<Warehouse> Warehouses => Set<Warehouse>();
        public DbSet<WarehouseArea> WarehouseAreas => Set<WarehouseArea>();
        public DbSet<AreaGroup> AreaGroups => Set<AreaGroup>();
        public DbSet<StorageLocation> StorageLocations => Set<StorageLocation>();
        public DbSet<Shift> Shifts => Set<Shift>();
        public DbSet<WorkScheduleTemplate> WorkScheduleTemplates => Set<WorkScheduleTemplate>();
        public DbSet<OperationalTeam> OperationalTeams => Set<OperationalTeam>();
        public DbSet<TeamMember> TeamMembers => Set<TeamMember>();
        public DbSet<TransferRequest> TransferRequests => Set<TransferRequest>();
        public DbSet<TransferItem> TransferItems => Set<TransferItem>();
        public DbSet<Notification> Notifications => Set<Notification>();

        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>().HasOne(u => u.Role).WithMany(r => r.Users).HasForeignKey(u => u.RoleId);
            modelBuilder.Entity<WorkScheduleTemplate>()
                .HasIndex(x => new { x.WarehouseId, x.Year })
                .IsUnique();
            modelBuilder.Entity<WorkScheduleTemplate>()
                .HasOne(x => x.Warehouse).WithMany()
                .HasForeignKey(x => x.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<User>().HasIndex(x => x.UserName);
            modelBuilder.Entity<User>().HasIndex(x => x.Email);
            modelBuilder.Entity<User>().HasIndex(x => x.PhoneNumber);
            modelBuilder.Entity<UserVerificationCode>()
                .HasIndex(x => new { x.UserId, x.IsActive });
            modelBuilder.Entity<UserVerificationCode>()
                .HasOne(x => x.User)
                .WithMany(x => x.VerificationCodes)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Category>().HasIndex(x => x.Code).IsUnique();
            modelBuilder.Entity<Category>().HasIndex(x => new { x.Type, x.ParentId, x.Name }).IsUnique();
            modelBuilder.Entity<Category>().Property(x => x.Code).HasMaxLength(80);
            modelBuilder.Entity<Category>().Property(x => x.Type).HasMaxLength(40);

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
                    EmailConfirmed = true,
                    IsActive = true,
                    CreateAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                }
            );
            modelBuilder.Entity<DonationRequest>()
                .Property(x => x.Status)
                .HasConversion<string>();

            modelBuilder.Entity<DonationRequest>()
                .Property(x => x.RequestCode)
                .HasMaxLength(32);

            modelBuilder.Entity<DonationRequest>()
                .HasIndex(x => x.RequestCode)
                .IsUnique();

            modelBuilder.Entity<Notification>().Property(x => x.Type).HasMaxLength(60);
            modelBuilder.Entity<Notification>().Property(x => x.Title).HasMaxLength(200);
            modelBuilder.Entity<Notification>().Property(x => x.TargetUrl).HasMaxLength(500);
            modelBuilder.Entity<Notification>().HasIndex(x => new { x.UserId, x.IsRead, x.CreateAt });
            modelBuilder.Entity<Notification>()
                .HasOne(x => x.User).WithMany(x => x.Notifications)
                .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Notification>()
                .HasOne(x => x.DonationRequest).WithMany(x => x.Notifications)
                .HasForeignKey(x => x.DonationRequestId).OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<DistributionItem>()
                .HasOne(x => x.Inventory).WithMany()
                .HasForeignKey(x => x.InventoryId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<DistributionRequest>()
                .Property(x => x.RequestCode).HasMaxLength(32);
            modelBuilder.Entity<DistributionRequest>()
                .HasIndex(x => x.RequestCode).IsUnique();
            modelBuilder.Entity<DistributionRequest>()
                .HasIndex(x => x.IssueSlipCode).IsUnique().HasFilter("[IssueSlipCode] IS NOT NULL");
            modelBuilder.Entity<DistributionRequest>()
                .HasIndex(x => x.GhnOrderCode).IsUnique().HasFilter("[GhnOrderCode] IS NOT NULL");
            modelBuilder.Entity<DistributionRequest>().HasOne(x => x.ApprovedByManager).WithMany()
                .HasForeignKey(x => x.ApprovedByManagerId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<DistributionRequest>().HasOne(x => x.WarehouseIssuedByStaff).WithMany()
                .HasForeignKey(x => x.WarehouseIssuedByStaffId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ShipmentStatusHistory>()
                .HasOne(x => x.DistributionRequest).WithMany(x => x.ShipmentHistory)
                .HasForeignKey(x => x.DistributionRequestId).OnDelete(DeleteBehavior.Cascade);

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
                .WithMany(x => x.IntakeBatches)
                .HasForeignKey(x => x.ShiftId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<IntakeBatch>()
                .HasIndex(x => new { x.ShiftId, x.ReceivingTeamId })
                .IsUnique()
                .HasFilter("[ReceivingTeamId] IS NOT NULL AND [IsActive] = 1");

            modelBuilder.Entity<IntakeBatch>()
                .HasOne(x => x.ReceivingTeam)
                .WithMany(x => x.IntakeBatches)
                .HasForeignKey(x => x.ReceivingTeamId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<IntakeBatch>()
                .HasOne(x => x.ClassificationReceivedByStaff)
                .WithMany()
                .HasForeignKey(x => x.ClassificationReceivedByStaffId)
                .OnDelete(DeleteBehavior.Restrict);

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

            modelBuilder.Entity<ClassifiedBatch>()
                .Property(x => x.GroupKey)
                .HasMaxLength(450);

            modelBuilder.Entity<ClassifiedBatch>()
                .HasIndex(x => x.GroupKey)
                .IsUnique();

            modelBuilder.Entity<ClassifiedBatchDonationRequest>()
                .HasKey(x => new { x.ClassifiedBatchId, x.DonationRequestId, x.IntakeBatchId });

            modelBuilder.Entity<ClassifiedBatchDonationRequest>()
                .HasOne(x => x.ClassifiedBatch)
                .WithMany(x => x.DonationRequestSources)
                .HasForeignKey(x => x.ClassifiedBatchId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ClassifiedBatchDonationRequest>()
                .HasOne(x => x.DonationRequest)
                .WithMany(x => x.ClassifiedBatchDonationRequests)
                .HasForeignKey(x => x.DonationRequestId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ClassifiedBatchDonationRequest>()
                .HasOne(x => x.IntakeBatch)
                .WithMany(x => x.ClassifiedBatchSources)
                .HasForeignKey(x => x.IntakeBatchId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StorageLocation>()
                .HasIndex(x => new { x.WarehouseId, x.LocationCode })
                .IsUnique();

            modelBuilder.Entity<Inventory>()
                .HasIndex(x => x.Sku)
                .IsUnique();

            modelBuilder.Entity<Inventory>()
                .HasOne(x => x.StorageLocation)
                .WithMany(x => x.Inventories)
                .HasForeignKey(x => x.StorageLocationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Inventory>()
                .HasOne(x => x.ClassifiedBatch)
                .WithMany()
                .HasForeignKey(x => x.ClassifiedBatchId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<InventoryTransaction>()
                .HasOne(x => x.PerformedByStaff)
                .WithMany()
                .HasForeignKey(x => x.PerformedByStaffId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TransactionItem>()
                .HasOne(x => x.SourceLocation)
                .WithMany()
                .HasForeignKey(x => x.SourceLocationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TransactionItem>()
                .HasOne(x => x.DestinationLocation)
                .WithMany()
                .HasForeignKey(x => x.DestinationLocationId)
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

            modelBuilder.Entity<InspectionAnswer>()
                .HasIndex(x => new { x.ClassifiedItemId, x.ConditionQuestionId })
                .IsUnique();

            modelBuilder.Entity<ClassifiedItem>()
                .HasOne(x => x.ClassifiedByStaff)
                .WithMany()
                .HasForeignKey(x => x.ClassifiedByStaffId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ClassifiedItem>()
                .HasIndex(x => x.ItemCode)
                .IsUnique();

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

            modelBuilder.Entity<Voucher>()
                .Property(x => x.Name)
                .HasMaxLength(200);

            modelBuilder.Entity<Voucher>()
                .Property(x => x.PartnerName)
                .HasMaxLength(100);

            modelBuilder.Entity<Voucher>()
                .Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(30);

            modelBuilder.Entity<Voucher>()
                .HasIndex(x => new
                {
                    x.PartnerName,
                    x.Name
                });

            modelBuilder.Entity<VoucherCode>()
                .Property(x => x.Code)
                .HasMaxLength(200);

            modelBuilder.Entity<VoucherCode>()
                .Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(30);

            modelBuilder.Entity<VoucherCode>()
                .HasIndex(x => x.Code)
                .IsUnique();

            modelBuilder.Entity<VoucherCode>()
                .HasOne(x => x.Voucher)
                .WithMany(x => x.VoucherCodes)
                .HasForeignKey(x => x.VoucherId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<VoucherCode>()
                .HasOne(x => x.RedeemedByUser)
                .WithMany(x => x.RedeemedVoucherCodes)
                .HasForeignKey(x => x.RedeemedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<VoucherRedemption>()
                .HasOne(x => x.User)
                .WithMany(x => x.VoucherRedemptions)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<VoucherRedemption>()
                .HasOne(x => x.Voucher)
                .WithMany(x => x.VoucherRedemptions)
                .HasForeignKey(x => x.VoucherId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<VoucherRedemption>()
                .HasOne(x => x.VoucherCode)
                .WithOne(x => x.Redemption)
                .HasForeignKey<VoucherRedemption>(x => x.VoucherCodeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<VoucherRedemption>()
                .HasIndex(x => x.VoucherCodeId)
                .IsUnique();

            modelBuilder.Entity<VoucherRedemption>()
                .HasIndex(x => new
                {
                    x.UserId,
                    x.RedeemedAt
                });

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
