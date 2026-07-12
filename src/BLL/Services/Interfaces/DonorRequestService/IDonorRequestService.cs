using BLL.DTOs;

namespace BLL.Services.Interfaces.DonorRequestService
{
    public interface IDonorRequestService
    {
        Task CreateAsync(Guid donorId, CreateDonorRequestDto dto);

        Task UpdateAsync(Guid donorId, Guid requestId, UpdateDonorRequestDto dto);

        Task CancelAsync(Guid donorId, Guid requestId);

        Task<List<DonorRequestSearchResultDto>> SearchByPhoneNumberAsync(string phoneNumber);

        Task<List<DonorRequestSearchResultDto>> GetByDonorIdAsync(Guid donorId);
    }
}
