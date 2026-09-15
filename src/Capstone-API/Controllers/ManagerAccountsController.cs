using System.Security.Claims;
using BLL.DTOs;
using BLL.Services.Interfaces.ManagerAccounts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Capstone_API.Controllers;

[ApiController]
[Route("api/manager-accounts")]
[Authorize(Roles = "Manager,Admin")]
public class ManagerAccountsController(IManagerAccountService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] Guid? warehouseId, [FromQuery] string? role,
        [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 10) =>
        Ok(await service.SearchAsync(warehouseId, role, search, page, pageSize));

    [HttpPost]
    public async Task<IActionResult> Create(CreateManagerAccountDto dto) =>
        Ok(new { id = await service.CreateAsync(CurrentUserId, dto) });

    [HttpPut("{userId:guid}")]
    public async Task<IActionResult> Update(Guid userId, UpdateManagerAccountDto dto)
    { await service.UpdateAsync(CurrentUserId, userId, dto); return NoContent(); }

    [HttpPatch("{userId:guid}/lock")]
    public async Task<IActionResult> SetLocked(Guid userId, SetManagerAccountStatusDto dto)
    { await service.SetLockedAsync(CurrentUserId, userId, dto.Locked); return NoContent(); }

    [HttpDelete("{userId:guid}")]
    public async Task<IActionResult> Delete(Guid userId)
    { await service.DeleteAsync(CurrentUserId, userId); return NoContent(); }

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
