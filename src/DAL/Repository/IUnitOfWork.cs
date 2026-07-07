namespace DAL.Repository
{
    public interface IUnitOfWork
    {
        Task SaveChangeAsync();
        IUserRepository UserRepository { get; }
        IRoleRepository RoleRepository { get; }
        IDonorRequestRepository DonorRequestRepository { get; }
        IPickupAssignmentRepository PickupAssignmentRepository { get; }
        IWarehouseRepository WarehouseRepository { get; }
    }
}
