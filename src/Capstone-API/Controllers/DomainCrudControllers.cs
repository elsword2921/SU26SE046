using BLL.Services.Interfaces.Common;
using DAL;
using DAL.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Capstone_API.Controllers;

[Route("api/warehouses")]
public class WarehouseController(ICrudService<Warehouse> service) : CrudControllerBase<Warehouse>(service)
{
    [AllowAnonymous]
    public override Task<ActionResult<List<Warehouse>>> GetAll() => base.GetAll();
}

[Route("api/categories")]
[ApiController]
[Authorize(Roles = "Manager")]
public class CategoryController(AppDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<Category>>> GetAll() => Ok(await context.Categories.AsNoTracking()
        .Where(x => x.IsActive != false).OrderBy(x => x.Type).ThenBy(x => x.SortOrder).ToListAsync());

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Category>> GetById(Guid id)
    {
        var category = await context.Categories.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.IsActive != false);
        return category is null ? NotFound() : Ok(category);
    }

    [HttpPost]
    public async Task<ActionResult<Category>> Create(Category category)
    {
        if (string.IsNullOrWhiteSpace(category.Type)) return BadRequest(new { message = "Category type is required." });
        await using var transaction = await context.Database.BeginTransactionAsync();
        var siblings = await context.Categories.Where(x => x.IsActive != false && x.Type == category.Type).ToListAsync();
        var maximumOrder = siblings.Count + 1;
        if (category.SortOrder < 1 || category.SortOrder > maximumOrder)
            return BadRequest(new { message = $"Sort order must be between 1 and {maximumOrder}." });
        foreach (var sibling in siblings.Where(x => x.SortOrder >= category.SortOrder))
        {
            sibling.SortOrder++;
            sibling.UpdateAt = DateTime.UtcNow;
        }
        category.Id = Guid.NewGuid();
        category.Code = category.Code.Trim().ToUpperInvariant();
        category.Name = category.Name.Trim();
        category.CreateAt = DateTime.UtcNow;
        category.UpdateAt = null;
        category.DeleteAt = null;
        category.IsActive = true;
        context.Categories.Add(category);
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
        return CreatedAtAction(nameof(GetById), new { id = category.Id }, category);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<Category>> Update(Guid id, Category input)
    {
        await using var transaction = await context.Database.BeginTransactionAsync();
        var category = await context.Categories.FirstOrDefaultAsync(x => x.Id == id && x.IsActive != false);
        if (category is null) return NotFound();
        var oldType = category.Type;
        var oldOrder = category.SortOrder;
        var typeChanged = oldType != input.Type;
        if (typeChanged)
        {
            var oldSiblings = await context.Categories.Where(x => x.IsActive != false && x.Id != id
                && x.Type == oldType && x.SortOrder > oldOrder).ToListAsync();
            foreach (var sibling in oldSiblings) sibling.SortOrder--;
            var newSiblings = await context.Categories.Where(x => x.IsActive != false && x.Id != id
                && x.Type == input.Type).ToListAsync();
            var maximumOrder = newSiblings.Count + 1;
            if (input.SortOrder < 1 || input.SortOrder > maximumOrder)
                return BadRequest(new { message = $"Sort order must be between 1 and {maximumOrder}." });
            foreach (var sibling in newSiblings.Where(x => x.SortOrder >= input.SortOrder)) sibling.SortOrder++;
        }
        else
        {
            var siblings = await context.Categories.Where(x => x.IsActive != false && x.Type == oldType).ToListAsync();
            if (input.SortOrder < 1 || input.SortOrder > siblings.Count)
                return BadRequest(new { message = $"Sort order must be between 1 and {siblings.Count}." });
            var target = siblings.FirstOrDefault(x => x.Id != id && x.SortOrder == input.SortOrder);
            if (target is not null)
            {
                target.SortOrder = oldOrder;
                target.UpdateAt = DateTime.UtcNow;
            }
        }
        category.Code = input.Code.Trim().ToUpperInvariant();
        category.Name = input.Name.Trim();
        category.Type = input.Type;
        category.ParentId = input.ParentId;
        category.SortOrder = input.SortOrder;
        category.Description = input.Description;
        category.MinimumMatchCount = input.MinimumMatchCount;
        category.UpdateAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
        return Ok(category);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await using var transaction = await context.Database.BeginTransactionAsync();
        var category = await context.Categories.FirstOrDefaultAsync(x => x.Id == id && x.IsActive != false);
        if (category is null) return NotFound();
        category.IsActive = false;
        category.DeleteAt = DateTime.UtcNow;
        var following = await context.Categories.Where(x => x.IsActive != false && x.Id != id
            && x.Type == category.Type && x.SortOrder > category.SortOrder).ToListAsync();
        foreach (var sibling in following)
        {
            sibling.SortOrder--;
            sibling.UpdateAt = DateTime.UtcNow;
        }
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
        return NoContent();
    }
}

[Route("api/voucher")]
public class VoucherController(ICrudService<Voucher> service) : CrudControllerBase<Voucher>(service);

[Route("api/pickup-assignments")]
[Authorize(Roles = "Manager,ReceivingStaff")]
public class PickupAssignmentController(ICrudService<PickupAssignment> service) : CrudControllerBase<PickupAssignment>(service);

[Route("api/intake-batches")]
[Authorize(Roles = "Manager,ReceivingStaff,ClassificationStaff,WarehouseStaff")]
public class IntakeBatchController(ICrudService<IntakeBatch> service) : CrudControllerBase<IntakeBatch>(service);

[Route("api/shifts")]
[Authorize(Roles = "Manager")]
public class ShiftController(ICrudService<Shift> service) : CrudControllerBase<Shift>(service);

[Route("api/operational-teams")]
[Authorize(Roles = "Manager")]
public class OperationalTeamController(ICrudService<OperationalTeam> service) : CrudControllerBase<OperationalTeam>(service);

[Route("api/team-members")]
[Authorize(Roles = "Manager")]
public class TeamMemberController(ICrudService<TeamMember> service) : CrudControllerBase<TeamMember>(service);

[Route("api/condition-questions")]
[Authorize(Roles = "Manager,ClassificationStaff")]
public class ConditionQuestionController(ICrudService<ConditionQuestion> service) : CrudControllerBase<ConditionQuestion>(service);

[Route("api/condition-answers")]
[Authorize(Roles = "Manager,ClassificationStaff")]
public class ConditionAnswerController(ICrudService<ConditionAnswer> service) : CrudControllerBase<ConditionAnswer>(service);

[Route("api/classified-items")]
[Authorize(Roles = "Manager,ClassificationStaff,WarehouseStaff")]
public class ClassifiedItemController(ICrudService<ClassifiedItem> service) : CrudControllerBase<ClassifiedItem>(service);

[Route("api/classified-batches")]
[Authorize(Roles = "Manager,ClassificationStaff,WarehouseStaff")]
public class ClassifiedBatchController(ICrudService<ClassifiedBatch> service) : CrudControllerBase<ClassifiedBatch>(service);

[Route("api/inspection-answers")]
[Authorize(Roles = "Manager,ClassificationStaff")]
public class InspectionAnswerController(ICrudService<InspectionAnswer> service) : CrudControllerBase<InspectionAnswer>(service);

[Route("api/warehouse-areas")]
[Authorize(Roles = "Manager,WarehouseStaff")]
public class WarehouseAreaController(ICrudService<WarehouseArea> service) : CrudControllerBase<WarehouseArea>(service);

[Route("api/area-groups")]
[Authorize(Roles = "Manager,WarehouseStaff")]
public class AreaGroupController(ICrudService<AreaGroup> service) : CrudControllerBase<AreaGroup>(service);

[Route("api/inventories")]
[Authorize(Roles = "Manager,WarehouseStaff")]
public class InventoryController(ICrudService<Inventory> service) : CrudControllerBase<Inventory>(service);

[Route("api/inventory-transactions")]
[Authorize(Roles = "Manager,WarehouseStaff")]
public class InventoryTransactionController(ICrudService<InventoryTransaction> service) : CrudControllerBase<InventoryTransaction>(service);

[Route("api/transaction-items")]
[Authorize(Roles = "Manager,WarehouseStaff")]
public class TransactionItemController(ICrudService<TransactionItem> service) : CrudControllerBase<TransactionItem>(service);

[Route("api/transfer-requests")]
[Authorize(Roles = "Manager,WarehouseStaff")]
public class TransferRequestController(ICrudService<TransferRequest> service) : CrudControllerBase<TransferRequest>(service);

[Route("api/transfer-items")]
[Authorize(Roles = "Manager,WarehouseStaff")]
public class TransferItemController(ICrudService<TransferItem> service) : CrudControllerBase<TransferItem>(service);

[Route("api/distribution-requests")]
[Authorize(Roles = "Manager,WarehouseStaff,CharityOrganization,RecyclingOrganization")]
public class DistributionRequestController(ICrudService<DistributionRequest> service) : CrudControllerBase<DistributionRequest>(service);

[Route("api/distribution-items")]
[Authorize(Roles = "Manager,WarehouseStaff,CharityOrganization,RecyclingOrganization")]
public class DistributionItemController(ICrudService<DistributionItem> service) : CrudControllerBase<DistributionItem>(service);
