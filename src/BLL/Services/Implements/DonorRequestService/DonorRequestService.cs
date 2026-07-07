using BLL.DTOs;
using BLL.Services.Interfaces.DonorRequestService;
using DAL.Models;
using DAL.Models.Enum;
using DAL.Repository;

namespace BLL.Services.Implements.DonorRequestService
{
    public class DonorRequestService : IDonorRequestService
    {
        private readonly IUnitOfWork _unitOfWork;

        public DonorRequestService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public async Task CreateAsync(
            Guid donorId,
            CreateDonorRequestDto dto)
        {
            var warehouse =
                await _unitOfWork
                .WarehouseRepository
                .GetByIdAsync(dto.WarehouseId);

            if (warehouse == null)
            {
                throw new Exception(
                    "Warehouse not found");
            }

            var request =
                new DonationRequest
                {
                    Id = Guid.NewGuid(),
                    DonorId = donorId,
                    WarehouseId = dto.WarehouseId,
                    PickupDate = DateTime.SpecifyKind(dto.PickupDate, DateTimeKind.Utc),
                    Description = dto.Description,
                    ImageUrls = dto.ImageUrls,
                    EstimateWeight = dto.EstimateWeight,
                    PickupAddress = dto.PickupAddress,
                    CreateAt = DateTime.UtcNow
                };

            await _unitOfWork
                .DonorRequestRepository
                .AddAsync(request);

            await _unitOfWork.SaveChangeAsync();
        }
    }
}
