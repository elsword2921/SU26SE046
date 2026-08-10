using BLL.DTOs;

namespace BLL.Services.Interfaces.Voucher
{
    public interface IVoucherService
    {
        Task<Guid> CreateVoucherAsync(Guid managerId, CreateVoucherDto dto);
        Task UpdateVoucherAsync(Guid managerId, Guid voucherId, UpdateVoucherDto dto);
        Task UpdateVoucherStatusAsync(Guid managerId, Guid voucherId, UpdateVoucherStatusDto dto);
        Task AddVoucherCodesAsync(Guid managerId, Guid voucherId, AddVoucherCodesDto dto);
        Task<List<VoucherCodeDto>> GetVoucherCodesAsync(Guid voucherId);
        Task<List<VoucherDto>> GetAvailableVouchersAsync();
        Task<VoucherDto?> GetVoucherAsync(Guid voucherId);
        Task<RedeemVoucherResultDto> RedeemVoucherAsync(Guid userId, Guid voucherId);
        Task<List<MyVoucherDto>> GetMyVouchersAsync(Guid userId);
        Task<List<VoucherRedemptionDto>> GetMyRedemptionsAsync(Guid userId);
        Task<int> GetDonationPointAsync(Guid userId);
    }
}