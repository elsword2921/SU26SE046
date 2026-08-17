using BLL.DTOs;
using BLL.Services.Interfaces.DonorRequestService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Capstone_API.Controllers
{
    [ApiController]
    [Route("api/donor-requests")]
    public class DonorRequestController
    : ControllerBase
    {
        private readonly IDonorRequestService _service;

        public DonorRequestController(IDonorRequestService service)
        {
            _service = service;
        }

        [HttpPost]
        [Authorize(Roles = "Donor")]
        public async Task<IActionResult> Create(CreateDonorRequestDto dto)
        {
            Guid donorId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var requestId = await _service.CreateAsync(donorId, dto);

            return Ok(new
            {
                Message = "Donation request created successfully.",
                RequestId = requestId
            });
        }

        [HttpGet("pickup-windows")]
        [Authorize(Roles = "Donor")]
        public async Task<IActionResult> PickupWindows(
            [FromQuery] DateTime date,
            [FromQuery] double? latitude,
            [FromQuery] double? longitude,
            [FromQuery] Guid? warehouseId) =>
            Ok(await _service.GetPickupAvailabilityAsync(date, latitude, longitude, warehouseId));

        [HttpGet("pickup-dates")]
        [Authorize(Roles = "Donor")]
        public async Task<IActionResult> PickupDates(
            [FromQuery] DateTime month,
            [FromQuery] double? latitude,
            [FromQuery] double? longitude,
            [FromQuery] Guid? warehouseId) =>
            Ok(await _service.GetPickupDatesAsync(month, latitude, longitude, warehouseId));



        [HttpPut("{id}")]
        [Authorize(Roles = "Donor")]
        public async Task<IActionResult> Update(Guid id, UpdateDonorRequestDto dto)
        {
            Guid donorId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            await _service.UpdateAsync(donorId, id, dto);

            return Ok(new
            {
                Message = "Donation request updated successfully."
            });
        }

        [HttpPatch("{id}/cancel")]
        [Authorize(Roles = "Donor")]
        public async Task<IActionResult> Cancel(Guid id)
        {
            Guid donorId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            await _service.CancelAsync(donorId, id);

            return Ok(new
            {
                Message = "Donation request cancelled successfully."
            });
        }

        [HttpPatch("{id}/shipping-info")]
        [Authorize(Roles = "Donor")]
        public async Task<IActionResult> UpdateShippingInfo(Guid id, UpdateShippingInfoDto dto)
        {
            Guid donorId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            await _service.UpdateShippingInfoAsync(donorId, id, dto);
            return Ok(new { Message = "Shipping information updated successfully." });
        }
        [HttpGet("my")]
        [Authorize(Roles = "Donor")]
        public async Task<IActionResult> GetMyRequests()
        {
            Guid donorId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var result =
                await _service.GetByDonorIdAsync(donorId);

            return Ok(result);
        }
        [HttpGet("search")]
        [Authorize(Roles = "Donor")]
        public async Task<IActionResult> SearchByPhoneNumber()
        {
            Guid donorId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _service.GetByDonorIdAsync(donorId);

            return Ok(result);
        }
    }
}
