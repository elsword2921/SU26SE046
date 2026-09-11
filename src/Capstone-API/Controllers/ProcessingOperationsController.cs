using System.Security.Claims;
using BLL.DTOs;
using BLL.Services.Interfaces.ProcessingOperations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Capstone_API.Controllers;

[ApiController]
[Route("api/processing-operations")]
[Authorize]
[ProcessingOperationErrors]
public class ProcessingOperationsController(
    IProcessingOperationsService service) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> Create(CreateProcessingOperationDto dto) =>Ok(await service.CreateAsync(CurrentUserId, dto));

    [HttpGet]
    [Authorize(Roles = "Manager,RecyclingOrganization,DisposalOrganization,WarehouseStaff")]
    public async Task<IActionResult> GetList(
        [FromQuery] string? status)
    {
        return Ok(await service.GetListAsync(CurrentUserId, status));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Manager,RecyclingOrganization,DisposalOrganization,WarehouseStaff")]
    public async Task<IActionResult> GetById(Guid id)
    {
        return Ok(await service.GetByIdAsync(CurrentUserId, id));
    }

    [HttpPost("{id:guid}/organization/approve")]
    [Authorize(Roles = "RecyclingOrganization,DisposalOrganization")]
    public async Task<IActionResult> ApproveByOrganization(Guid id)
    {
        await service.ApproveByOrganizationAsync(CurrentUserId,id);
        return Ok();
    }

    [HttpPost("{id:guid}/organization/reject")]
    [Authorize(Roles = "RecyclingOrganization,DisposalOrganization")]
    public async Task<IActionResult> RejectByOrganization(Guid id,ProcessingOperationDecisionDto dto)
    {
        await service.RejectByOrganizationAsync(CurrentUserId,id,dto);
        return Ok();
    }

    [HttpPost("{id:guid}/manager/approve")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> ApproveByManager(Guid id)
    {
        await service.ApproveByManagerAsync(CurrentUserId,id);
        return Ok();
    }

    [HttpPost("{id:guid}/manager/reject")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> RejectByManager(Guid id,ProcessingOperationDecisionDto dto)
    {
        await service.RejectByManagerAsync(CurrentUserId,id,dto);
        return Ok();
    }

    [HttpPost("{id:guid}/issue")]
    [Authorize(Roles = "WarehouseStaff")]
    public async Task<IActionResult> Issue(Guid id,IssueProcessingOperationDto dto)
    {
        await service.IssueAsync(CurrentUserId,id,dto);
        return Ok();
    }

    [HttpGet("catalog")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> Catalog([FromQuery] Guid? warehouseId, [FromQuery] string? operationType)
        => Ok(await service.CatalogAsync(CurrentUserId, warehouseId, operationType));

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> Cancel(Guid id, ProcessingOperationDecisionDto dto)
    {
        await service.CancelAsync(CurrentUserId, id, dto);
        return Ok();
    }

    [HttpPost("{id:guid}/organization/receive")]
    [Authorize(Roles = "RecyclingOrganization,DisposalOrganization")]
    public async Task<IActionResult> Receive(Guid id)
    {
        await service.ReceiveAsync(CurrentUserId, id);
        return Ok();
    }

    [HttpPost("{id:guid}/organization/complete")]
    [Authorize(Roles = "RecyclingOrganization,DisposalOrganization")]
    public async Task<IActionResult> Complete(Guid id, CompleteProcessingOperationDto dto)
    {
        await service.CompleteAsync(CurrentUserId, id, dto);
        return Ok();
    }

    [HttpPost("{id:guid}/ghn")]
    [Authorize(Roles = "WarehouseStaff")]
    public async Task<IActionResult> CreateGhn(Guid id, CreateProcessingGhnShipmentDto dto)
    {
        await service.CreateGhnShipmentAsync(CurrentUserId, id, dto);
        return NoContent();
    }

    [HttpPost("{id:guid}/ghn/refresh")]
    [Authorize(Roles = "Manager,WarehouseStaff,RecyclingOrganization,DisposalOrganization")]
    public async Task<IActionResult> RefreshGhn(Guid id)
    {
        await service.RefreshGhnAsync(CurrentUserId, id);
        return NoContent();
    }

    private Guid CurrentUserId =>Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost("{id:guid}/organization/return-schedule")]
    [Authorize(Roles = "RecyclingOrganization")]
    public async Task<IActionResult> ScheduleReturn(Guid id, ScheduleRecyclingReturnDto dto)
    {
        await service.ScheduleReturnAsync(CurrentUserId, id, dto);
        return NoContent();
    }

    [HttpPost("{id:guid}/organization/return-dispatch")]
    [Authorize(Roles = "RecyclingOrganization")]
    public async Task<IActionResult> DispatchReturn(Guid id, DispatchRecyclingReturnDto dto)
    {
        await service.DispatchReturnAsync(CurrentUserId, id, dto);
        return NoContent();
    }

    [HttpGet("{id:guid}/return-receipt-options")]
    [Authorize(Roles = "WarehouseStaff")]
    public async Task<IActionResult> ReturnReceiptOptions(Guid id)
        => Ok(await service.ReturnReceiptOptionsAsync(CurrentUserId, id));

    [HttpPost("{id:guid}/return-receive")]
    [Authorize(Roles = "WarehouseStaff")]
    public async Task<IActionResult> ReceiveReturn(Guid id, ReceiveRecyclingReturnDto dto)
    {
        await service.ReceiveReturnAsync(CurrentUserId, id, dto);
        return NoContent();
    }
}
