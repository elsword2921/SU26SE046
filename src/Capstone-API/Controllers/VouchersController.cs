using System.Security.Claims;
using BLL.DTOs;
using BLL.Services.Interfaces.Voucher;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Capstone_API.Controllers;

[ApiController]
[Route("api/vouchers")]
[Authorize]
public class VouchersController(
    IVoucherService service) : ControllerBase
{
    [HttpPost][Authorize(Roles = "Manager")]
    public async Task<IActionResult> CreateVoucher(CreateVoucherDto dto){var id = await service.CreateVoucherAsync(CurrentUserId,dto); return Ok(new { id });}
    [HttpPut("{voucherId:guid}")][Authorize(Roles = "Manager")]
    public async Task<IActionResult> UpdateVoucher(Guid voucherId,UpdateVoucherDto dto){await service.UpdateVoucherAsync(CurrentUserId,voucherId,dto);return NoContent();}
    [HttpPatch("{voucherId:guid}/status")][Authorize(Roles = "Manager")]
    public async Task<IActionResult> UpdateStatus(Guid voucherId,UpdateVoucherStatusDto dto){await service.UpdateVoucherStatusAsync(CurrentUserId,voucherId,dto);return NoContent();}
    [HttpPost("{voucherId:guid}/codes")][Authorize(Roles = "Manager")]
    public async Task<IActionResult> AddCodes(Guid voucherId,AddVoucherCodesDto dto){await service.AddVoucherCodesAsync(CurrentUserId,voucherId,dto);return NoContent();}
    [HttpGet("{voucherId:guid}/codes")][Authorize(Roles = "Manager")]
    public async Task<IActionResult> GetCodes(Guid voucherId){return Ok(await service.GetVoucherCodesAsync(voucherId));}
    [HttpGet][Authorize(Roles = "Donor,Manager")]
    public async Task<IActionResult> GetAvailableVouchers(){return Ok(await service.GetAvailableVouchersAsync());}
    [HttpGet("{voucherId:guid}")][Authorize(Roles = "Donor,Manager")]
    public async Task<IActionResult> GetVoucher(Guid voucherId){var result =await service.GetVoucherAsync(voucherId);return result == null ? NotFound() : Ok(result);}
    [HttpPost("{voucherId:guid}/redeem")][Authorize(Roles = "Donor")]
    public async Task<IActionResult> Redeem(Guid voucherId){return Ok(await service.RedeemVoucherAsync(CurrentUserId,voucherId));}
    [HttpGet("my-vouchers")][Authorize(Roles = "Donor")]
    public async Task<IActionResult> MyVouchers(){return Ok(await service.GetMyVouchersAsync(CurrentUserId));}
    [HttpGet("my-redemptions")][Authorize(Roles = "Donor")]
    public async Task<IActionResult> MyRedemptions(){return Ok(await service.GetMyRedemptionsAsync(CurrentUserId));}
    [HttpGet("my-points")][Authorize(Roles = "Donor")]
    public async Task<IActionResult> MyPoints(){var points =await service.GetDonationPointAsync(CurrentUserId);return Ok(new{donationPoint = points});}
    private Guid CurrentUserId =>Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}