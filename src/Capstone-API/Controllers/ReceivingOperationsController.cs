using System.Security.Claims;
using BLL.DTOs;
using BLL.Services.Interfaces.ReceivingOperations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Capstone_API.Controllers;

[ApiController]
[Route("api/receiving-operations")]
[Authorize]
public class ReceivingOperationsController(IReceivingOperationsService service) : ControllerBase
{
    [HttpPost("standard-shifts")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> GenerateShifts(GenerateShiftsDto dto)
    { await service.GenerateStandardShiftsAsync(dto); return NoContent(); }

    [HttpPost("teams")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> CreateTeam(CreateReceivingTeamDto dto)
    { var id = await service.CreateTeamAsync(dto); return Ok(new { id }); }

    [HttpPost("plan")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> Plan(PlanReceivingShiftDto dto)
    { var count = await service.PlanShiftAsync(dto); return Ok(new { plannedRequests = count }); }

    [HttpGet("my-batches")]
    [Authorize(Roles = "ReceivingStaff")]
    public async Task<IActionResult> MyBatches() => Ok(await service.GetMyBatchesAsync(CurrentUserId));

    [HttpGet("my-batches/{batchId:guid}")]
    [Authorize(Roles = "ReceivingStaff")]
    public async Task<IActionResult> MyBatch(Guid batchId)
    { var batch = await service.GetMyBatchAsync(CurrentUserId, batchId); return batch is null ? NotFound() : Ok(batch); }

    [HttpPost("my-batches/{batchId:guid}/start")]
    [Authorize(Roles = "ReceivingStaff")]
    public async Task<IActionResult> Start(Guid batchId)
    { await service.StartBatchAsync(CurrentUserId, batchId); return NoContent(); }

    [HttpPost("my-shifts/{shiftId:guid}/complete")]
    [Authorize(Roles = "ReceivingStaff")]
    public async Task<IActionResult> CompleteShift(Guid shiftId)
    { await service.CompleteShiftAsync(CurrentUserId, shiftId); return NoContent(); }

    [HttpPost("my-batches/{batchId:guid}/requests/{requestId:guid}/confirm")]
    [Authorize(Roles = "ReceivingStaff")]
    public async Task<IActionResult> Confirm(Guid batchId, Guid requestId, ConfirmPickupDto dto)
    { await service.ConfirmPickupAsync(CurrentUserId, batchId, requestId, dto); return NoContent(); }

    [HttpPost("my-batches/{batchId:guid}/requests/{requestId:guid}/reschedule")]
    [Authorize(Roles = "ReceivingStaff")]
    public async Task<IActionResult> Reschedule(Guid batchId, Guid requestId, ReschedulePickupDto dto)
    { await service.RescheduleAsync(CurrentUserId, batchId, requestId, dto); return NoContent(); }

    [HttpPost("my-batches/{batchId:guid}/requests/{requestId:guid}/reject")]
    [Authorize(Roles = "ReceivingStaff")]
    public async Task<IActionResult> Reject(Guid batchId, Guid requestId, RejectPickupDto dto)
    { await service.RejectAsync(CurrentUserId, batchId, requestId, dto); return NoContent(); }

    [HttpPost("my-batches/{batchId:guid}/complete")]
    [Authorize(Roles = "ReceivingStaff")]
    public async Task<IActionResult> Complete(Guid batchId)
    { await service.CompleteBatchAsync(CurrentUserId, batchId); return NoContent(); }

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
