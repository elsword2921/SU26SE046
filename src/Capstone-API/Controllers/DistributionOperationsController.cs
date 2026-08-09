using BLL.DTOs;
using BLL.Services.Implements.DistributionOperations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Capstone_API.Controllers;
[ApiController,Authorize,Route("api/distribution-operations")]
public class DistributionOperationsController(DistributionOperationsService service):ControllerBase
{
    private Guid UserId=>Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    [HttpGet("catalog"),Authorize(Roles="CharityOrganization")]
    public async Task<IActionResult> Catalog([FromQuery]Guid? warehouseId)=>Ok(await service.CatalogAsync(warehouseId));
    [HttpPost,Authorize(Roles="CharityOrganization")]
    public async Task<IActionResult> Create(CreateDistributionRequestDto dto)=>Ok(new{id=await service.CreateAsync(UserId,dto)});
    [HttpPut("{id:guid}"),Authorize(Roles="CharityOrganization")]
    public async Task<IActionResult> Update(Guid id,CreateDistributionRequestDto dto){await service.UpdateAsync(UserId,id,dto);return NoContent();}
    [HttpDelete("{id:guid}"),Authorize(Roles="CharityOrganization")]
    public async Task<IActionResult> Delete(Guid id){await service.DeleteAsync(UserId,id);return NoContent();}
    [HttpGet("mine"),Authorize(Roles="CharityOrganization")]
    public async Task<IActionResult> Mine()=>Ok(await service.MineAsync(UserId));
    [HttpGet("manager"),Authorize(Roles="Manager")]
    public async Task<IActionResult> Manager()=>Ok(await service.ManagerAsync());
    [HttpPost("manager-request"),Authorize(Roles="Manager")]
    public async Task<IActionResult> CreateManagerRequest(CreateManagerRequestDto dto)=>Ok(new{id=await service.CreateManagerRequestAsync(UserId,dto)});
    [HttpPatch("{id:guid}/approval"),Authorize(Roles="Manager")]
    public async Task<IActionResult> Approve(Guid id,ApproveDistributionDto dto){await service.ApproveAsync(UserId,id,dto);return NoContent();}
    [HttpPatch("{id:guid}/organization-response"),Authorize(Roles="CharityOrganization,RecyclingOrganization,DisposalOrganization")]
    public async Task<IActionResult> RespondManagerRequest(Guid id,RespondDistributionRequestDto dto){await service.RespondDistributionRequestAsync(UserId,id,dto);return NoContent();}
    [HttpGet("warehouse"),Authorize(Roles="WarehouseStaff")]
    public async Task<IActionResult> Warehouse()=>Ok(await service.WarehouseAsync(UserId));
    [HttpPost("{id:guid}/issue"),Authorize(Roles="WarehouseStaff")]
    public async Task<IActionResult> Issue(Guid id,IssueDistributionDto dto){await service.IssueAsync(UserId,id,dto);return NoContent();}
    [HttpPost("{id:guid}/ghn"),Authorize(Roles="WarehouseStaff")]
    public async Task<IActionResult> CreateGhn(Guid id,CreateGhnShipmentDto dto){await service.CreateGhnShipmentAsync(UserId,id,dto);return NoContent();}
    [HttpPost("{id:guid}/ghn/refresh"),Authorize(Roles="Manager,WarehouseStaff,CharityOrganization")]
    public async Task<IActionResult> Refresh(Guid id){await service.RefreshGhnAsync(UserId,id);return NoContent();}
}
