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

    [HttpPost("year-shifts")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> GenerateYearShifts(GenerateYearShiftsDto dto)
        => Ok(await service.GenerateYearShiftsAsync(dto));

    [HttpPost("month-shifts")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> GenerateMonthShifts(GenerateMonthShiftsDto dto)
        => Ok(await service.GenerateMonthShiftsAsync(dto));

    [HttpDelete("year-shifts")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> DeleteYearShifts([FromQuery] Guid warehouseId, [FromQuery] int year)
        => Ok(await service.DeleteYearShiftsAsync(new DeleteYearShiftsDto(warehouseId, year)));
    
    [HttpPost("shifts/generate")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> GenerateShifts(
        GenerateShiftsV2Dto dto)
        => Ok(await service.GenerateShiftsAsync(dto));

    [HttpPut("manager-shifts/{shiftId:guid}")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> UpdateShift(Guid shiftId, UpdateManagerShiftDto dto)
    { await service.UpdateShiftAsync(shiftId, dto); return NoContent(); }

    [HttpDelete("manager-shifts/{shiftId:guid}")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> DeleteShift(Guid shiftId)
    { await service.DeleteShiftAsync(shiftId); return NoContent(); }

    [HttpPost("teams")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> CreateTeam(CreateReceivingTeamDto dto)
    { var id = await service.CreateTeamAsync(dto); return Ok(new { id }); }

    [HttpPut("teams/{teamId:guid}")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> UpdateTeam(Guid teamId, UpdateReceivingTeamDto dto)
    { await service.UpdateTeamAsync(teamId, dto); return NoContent(); }

    [HttpDelete("teams/{teamId:guid}")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> DeleteTeam(Guid teamId)
    { await service.DeleteTeamAsync(teamId); return NoContent(); }

    [HttpPost("plan")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> Plan(PlanReceivingShiftDto dto)
    { var count = await service.PlanShiftAsync(dto); return Ok(new { plannedRequests = count }); }

    [HttpPost("auto-balance/{shiftId:guid}")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> AutoBalance(Guid shiftId)
        => Ok(await service.AutoBalanceShiftAsync(shiftId));

    [HttpGet("dispatch-board")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> DispatchBoard() => Ok(await service.GetDispatchBoardAsync());

    [HttpGet("manager-setup")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> ManagerSetup() => Ok(await service.GetManagerSetupAsync());

    [HttpPost("assign-request")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> AssignRequest(AssignDonationRequestDto dto)
    { await service.AssignRequestAsync(dto); return NoContent(); }

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

    [HttpGet("my-warehouse-dropoffs")]
    [Authorize(Roles = "ReceivingStaff")]
    public async Task<IActionResult> MyWarehouseDropOffs()
        => Ok(await service.GetMyWarehouseDropOffsAsync(CurrentUserId));

    [HttpPost("my-warehouse-dropoffs/{requestId:guid}/confirm")]
    [Authorize(Roles = "ReceivingStaff")]
    public async Task<IActionResult> ConfirmWarehouseDropOff(Guid requestId, ConfirmPickupDto dto)
    {
        await service.ConfirmWarehouseDropOffAsync(CurrentUserId, requestId, dto);
        return NoContent();
    }

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

    [HttpPost("my-batches/{batchId:guid}/send-to-classification")]
    [Authorize(Roles = "ReceivingStaff")]
    public async Task<IActionResult> SendToClassification(Guid batchId)
    { await service.SendToClassificationAsync(CurrentUserId, batchId); return NoContent(); }

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
