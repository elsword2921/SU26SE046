using System.Security.Claims;
using BLL.DTOs;
using BLL.Services.Interfaces.WarehouseOperations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Capstone_API.Controllers;

[ApiController]
[Route("api/warehouse-operations")]
[Authorize(Roles = "WarehouseStaff,Manager")]
public class WarehouseOperationsController(IWarehouseOperationsService service) : ControllerBase
{
    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard([FromQuery] Guid? warehouseId) =>
        Ok(await service.GetDashboardAsync(CurrentUserId, warehouseId));

    [HttpGet("layout")]
    public async Task<IActionResult> Layout([FromQuery] Guid? warehouseId) =>
        Ok(await service.GetLayoutAsync(CurrentUserId, warehouseId));

    [HttpGet("inbound-batches")]
    public async Task<IActionResult> InboundBatches([FromQuery] Guid? warehouseId) =>
        Ok(await service.GetInboundBatchesAsync(CurrentUserId, warehouseId));

    [HttpGet("intake-traces")]
    public async Task<IActionResult> IntakeTraces([FromQuery] Guid? warehouseId) =>
        Ok(await service.GetIntakeTracesAsync(CurrentUserId, warehouseId));

    [HttpPost("warehouses")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> CreateWarehouse(CreateWarehouseDto dto) =>
        Ok(new { id = await service.CreateWarehouseAsync(CurrentUserId, dto) });

    [HttpGet("warehouses/{warehouseId:guid}")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> Warehouse(Guid warehouseId) =>
        Ok(await service.GetWarehouseAsync(CurrentUserId, warehouseId));

    [HttpPut("warehouses/{warehouseId:guid}")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> UpdateWarehouse(Guid warehouseId, CreateWarehouseDto dto)
    { await service.UpdateWarehouseAsync(CurrentUserId, warehouseId, dto); return NoContent(); }

    [HttpDelete("warehouses/{warehouseId:guid}")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> DeleteWarehouse(Guid warehouseId)
    { await service.DeleteWarehouseAsync(CurrentUserId, warehouseId); return NoContent(); }

    [HttpPost("areas")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> CreateArea(SaveWarehouseAreaDto dto) =>
        Ok(new { id = await service.CreateAreaAsync(CurrentUserId, dto) });

    [HttpPut("areas/{areaId:guid}")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> UpdateArea(Guid areaId, SaveWarehouseAreaDto dto)
    { await service.UpdateAreaAsync(CurrentUserId, areaId, dto); return NoContent(); }

    [HttpDelete("areas/{areaId:guid}")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> DeleteArea(Guid areaId)
    { await service.DeleteAreaAsync(CurrentUserId, areaId); return NoContent(); }

    [HttpPost("groups")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> CreateGroup(SaveWarehouseGroupDto dto) =>
        Ok(new { id = await service.CreateGroupAsync(CurrentUserId, dto) });

    [HttpPut("groups/{groupId:guid}")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> UpdateGroup(Guid groupId, SaveWarehouseGroupDto dto)
    { await service.UpdateGroupAsync(CurrentUserId, groupId, dto); return NoContent(); }

    [HttpDelete("groups/{groupId:guid}")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> DeleteGroup(Guid groupId)
    { await service.DeleteGroupAsync(CurrentUserId, groupId); return NoContent(); }

    [HttpPost("locations")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> CreateLocation(SaveStorageLocationDto dto) =>
        Ok(new { id = await service.CreateLocationAsync(CurrentUserId, dto) });

    [HttpPut("locations/{locationId:guid}")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> UpdateLocation(Guid locationId, SaveStorageLocationDto dto)
    { await service.UpdateLocationAsync(CurrentUserId, locationId, dto); return NoContent(); }

    [HttpDelete("locations/{locationId:guid}")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> DeleteLocation(Guid locationId)
    { await service.DeleteLocationAsync(CurrentUserId, locationId); return NoContent(); }

    [HttpGet("batches/{batchId:guid}")]
    public async Task<IActionResult> Batch(Guid batchId)
    {
        var batch = await service.GetBatchAsync(batchId);
        return batch is null ? NotFound() : Ok(batch);
    }

    [HttpPost("batches/{batchId:guid}/confirm-receipt")]
    public async Task<IActionResult> ConfirmReceipt(Guid batchId, ConfirmWarehouseReceiptDto dto)
    { await service.ConfirmReceiptAsync(CurrentUserId, batchId, dto); return NoContent(); }

    [HttpGet("batches/{batchId:guid}/locations")]
    public async Task<IActionResult> Locations(Guid batchId) => Ok(await service.GetLocationsAsync(batchId));

    [HttpPost("batches/{batchId:guid}/putaway")]
    public async Task<IActionResult> Putaway(Guid batchId, PutawayBatchDto dto)
    { await service.PutawayAsync(CurrentUserId, batchId, dto); return NoContent(); }

    [HttpGet("inventory")]
    public async Task<IActionResult> Inventory([FromQuery] Guid? warehouseId, [FromQuery] string? search) =>
        Ok(await service.GetInventoryAsync(CurrentUserId, warehouseId, search));

    [HttpGet("locations/{locationId:guid}/inventory")]
    public async Task<IActionResult> LocationInventory(Guid locationId) =>
        Ok(await service.GetLocationInventoryAsync(CurrentUserId, locationId));

    [HttpGet("transactions")]
    public async Task<IActionResult> Transactions([FromQuery] Guid? warehouseId, [FromQuery] string? type) =>
        Ok(await service.GetTransactionsAsync(CurrentUserId, warehouseId, type));

    [HttpPost("inventory/{inventoryId:guid}/issue")]
    public async Task<IActionResult> Issue(Guid inventoryId, IssueInventoryDto dto)
    { await service.IssueAsync(CurrentUserId, inventoryId, dto); return NoContent(); }

    [HttpPost("inventory/{inventoryId:guid}/move")]
    public async Task<IActionResult> Move(Guid inventoryId, MoveInventoryDto dto)
    { await service.MoveAsync(CurrentUserId, inventoryId, dto); return NoContent(); }

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
