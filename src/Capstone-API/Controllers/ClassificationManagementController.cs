using System.Security.Claims;
using BLL.DTOs;
using BLL.Services.Interfaces.ClassificationOperations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Capstone_API.Controllers;

[ApiController]
[Route("api/classification-management")]
[Authorize(Roles = "Manager")]
public class ClassificationManagementController(IClassificationOperationsService service) : ControllerBase
{
    [HttpGet("board")]
    public async Task<IActionResult> Board([FromQuery] Guid? warehouseId, [FromQuery] DateTime? date) =>
        Ok(await service.GetManagementBoardAsync(warehouseId, date));

    [HttpPost("batches/{batchId:guid}/assign")]
    public async Task<IActionResult> Assign(Guid batchId, AssignClassificationBatchDto dto)
    {
        await service.AssignBatchAsync(CurrentUserId, batchId, dto.TeamId);
        return NoContent();
    }

    [HttpPost("shifts/{shiftId:guid}/auto-balance")]
    public async Task<IActionResult> AutoBalance(Guid shiftId) =>
        Ok(await service.AutoBalanceBatchesAsync(CurrentUserId, shiftId));

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
