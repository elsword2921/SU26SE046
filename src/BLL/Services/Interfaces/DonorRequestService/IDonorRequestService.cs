using BLL.DTOs;

namespace BLL.Services.Interfaces.DonorRequestService
{
    public interface IDonorRequestService
    {
        Task CreateAsync(Guid donorId, CreateDonorRequestDto dto);
    }
}
