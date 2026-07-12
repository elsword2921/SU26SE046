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

            await _service.CreateAsync(donorId, dto);

            return Ok(new
            {
                Message =
                "Donation request created successfully."
            });
        }

        [HttpGet("search")]
        [AllowAnonymous]
        public async Task<IActionResult> SearchByPhoneNumber([FromQuery] string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                return BadRequest(new
                {
                    Message = "Phone number is required."
                });
            }

            var result =
                await _service.SearchByPhoneNumberAsync(phoneNumber);

            return Ok(result);
        }
    }
}
