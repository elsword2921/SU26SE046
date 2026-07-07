namespace DAL.Repository
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;
        private readonly IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IDonorRequestRepository _donorRequestRepository;
        private readonly IPickupAssignmentRepository _pickupAssignmentRepository;
        private readonly IWarehouseRepository _warehouseRepository;
        public IUserRepository UserRepository => _userRepository;
        public IRoleRepository RoleRepository => _roleRepository;
        public IDonorRequestRepository DonorRequestRepository => _donorRequestRepository;
        public IPickupAssignmentRepository PickupAssignmentRepository => _pickupAssignmentRepository;
        public IWarehouseRepository WarehouseRepository => _warehouseRepository;

        public Task SaveChangeAsync()
        {
            return _context.SaveChangesAsync();
        }

        public UnitOfWork(AppDbContext context)
        {
            _context = context;
            _userRepository = new UserRepository(context);
            _roleRepository = new RoleRepository(context);
            _donorRequestRepository = new DonorRequestRepository(context);
            _pickupAssignmentRepository = new PickupAssignmentRepository(context);
            _warehouseRepository = new WarehouseRepository(context);
        }
    }
}
