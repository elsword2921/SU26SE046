using System.Security.Claims;
using BLL.DTOs;
using BLL.Services.Interfaces.ClassificationOperations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Capstone_API.Controllers;

[ApiController]
[Route("api/classification-operations")]
[Authorize(Roles = "ClassificationStaff")]
public class ClassificationOperationsController(IClassificationOperationsService service) : ControllerBase
{
    [HttpGet("batches")]
    public async Task<IActionResult> GetBatches() => Ok(await service.GetBatchesAsync(CurrentUserId));

    [HttpGet("batches/{batchId:guid}")]
    public async Task<IActionResult> GetBatch(Guid batchId)
    {
        var batch = await service.GetBatchAsync(CurrentUserId, batchId);
        return batch is null ? NotFound() : Ok(batch);
    }

    [HttpGet("catalog")]
    public async Task<IActionResult> GetCatalog() => Ok(await service.GetCatalogAsync());

    [HttpPost("batches/{batchId:guid}/start")]
    public async Task<IActionResult> Start(Guid batchId)
    { await service.StartBatchAsync(CurrentUserId, batchId); return NoContent(); }

    [HttpPost("batches/{batchId:guid}/confirm-receipt")]
    public async Task<IActionResult> ConfirmReceipt(Guid batchId)
    { await service.ConfirmReceiptAsync(CurrentUserId, batchId); return NoContent(); }

    [HttpPut("batches/{batchId:guid}/count")]
    public async Task<IActionResult> CountBatch(Guid batchId, CountClassificationBatchDto dto)
    { await service.CountBatchAsync(CurrentUserId, batchId, dto); return NoContent(); }

    [HttpPost("batches/{batchId:guid}/items")]
    public async Task<IActionResult> ClassifyItem(Guid batchId, ClassifyItemDto dto) =>
        Ok(await service.ClassifyItemAsync(CurrentUserId, batchId, dto));

    [HttpPut("batches/{batchId:guid}/items/{itemId:guid}")]
    public async Task<IActionResult> UpdateItem(Guid batchId, Guid itemId, ClassifyItemDto dto) =>
        Ok(await service.UpdateItemAsync(CurrentUserId, batchId, itemId, dto));

    [HttpDelete("batches/{batchId:guid}/items/{itemId:guid}")]
    public async Task<IActionResult> DeleteItem(Guid batchId, Guid itemId)
    { await service.DeleteItemAsync(CurrentUserId, batchId, itemId); return NoContent(); }

    [HttpPost("batches/{batchId:guid}/complete")]
    public async Task<IActionResult> Complete(Guid batchId)
    { await service.CompleteBatchAsync(CurrentUserId, batchId); return NoContent(); }

    [HttpPost("teams/{teamId:guid}/start")]
    public async Task<IActionResult> StartTeam(Guid teamId)
    { await service.StartTeamAsync(CurrentUserId, teamId); return NoContent(); }

    [HttpPost("teams/{teamId:guid}/complete")]
    public async Task<IActionResult> CompleteTeam(Guid teamId)
    { await service.CompleteTeamAsync(CurrentUserId, teamId); return NoContent(); }

    [HttpGet("grouped-batches")]
    public async Task<IActionResult> GetGroupedBatches([FromQuery] DateTime? date) =>
        Ok(await service.GetGroupedBatchesAsync(date));

    [HttpGet("grouped-batches/{groupedBatchId:guid}")]
    public async Task<IActionResult> GetGroupedBatch(Guid groupedBatchId)
    {
        var batch = await service.GetGroupedBatchAsync(groupedBatchId);
        return batch is null ? NotFound() : Ok(batch);
    }

    [HttpPost("grouped-batches/{groupedBatchId:guid}/send-to-warehouse")]
    public async Task<IActionResult> SendGroupedBatchToWarehouse(Guid groupedBatchId)
    {
        await service.SendGroupedBatchToWarehouseAsync(CurrentUserId, groupedBatchId);
        return NoContent();
    }

    [HttpPost("grouped-batches/send-to-warehouse")]
    public async Task<IActionResult> SendGroupedBatchesToWarehouse(SendGroupedBatchesToWarehouseDto dto) =>
        Ok(await service.SendGroupedBatchesToWarehouseAsync(CurrentUserId, dto.GroupedBatchIds));

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
