using BLL.DTOs;
using BLL.Services.Implements.DistributionOperations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Net.Http.Json;
using System.Text.Json;

namespace Capstone_API.Controllers;
[ApiController,Authorize,Route("api/distribution-operations")]
public class DistributionOperationsController(DistributionOperationsService service,
    IHttpClientFactory httpClientFactory, IConfiguration configuration):ControllerBase
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
    [HttpPatch("{id:guid}/approval"),Authorize(Roles="Manager")]
    public async Task<IActionResult> Approve(Guid id,ApproveDistributionDto dto){await service.ApproveAsync(UserId,id,dto);return NoContent();}
    [HttpGet("warehouse"),Authorize(Roles="WarehouseStaff")]
    public async Task<IActionResult> Warehouse()=>Ok(await service.WarehouseAsync(UserId));
    [HttpPost("{id:guid}/issue"),Authorize(Roles="WarehouseStaff")]
    public async Task<IActionResult> Issue(Guid id,IssueDistributionDto dto){await service.IssueAsync(UserId,id,dto);return NoContent();}
    [HttpPost("{id:guid}/ghn"),Authorize(Roles="WarehouseStaff")]
    public async Task<IActionResult> CreateGhn(Guid id,CreateGhnShipmentDto dto){await service.CreateGhnShipmentAsync(UserId,id,dto);return NoContent();}
    [HttpPost("{id:guid}/ghn/refresh"),Authorize(Roles="Manager,WarehouseStaff,CharityOrganization")]
    public async Task<IActionResult> Refresh(Guid id){await service.RefreshGhnAsync(UserId,id);return NoContent();}
    [HttpGet("ghn/provinces"),Authorize(Roles="WarehouseStaff")]
    public async Task<IActionResult> GhnProvinces() => Ok(await GhnMasterAsync("province", null));
    [HttpGet("ghn/districts"),Authorize(Roles="WarehouseStaff")]
    public async Task<IActionResult> GhnDistricts([FromQuery]int provinceId) =>
        Ok(await GhnMasterAsync("district", new { province_id = provinceId }));
    [HttpGet("ghn/wards"),Authorize(Roles="WarehouseStaff")]
    public async Task<IActionResult> GhnWards([FromQuery]int districtId) =>
        Ok(await GhnMasterAsync("ward", new { district_id = districtId }));

    private async Task<JsonElement> GhnMasterAsync(string path, object? body)
    {
        var token = configuration["Ghn:Token"] ?? configuration["GHN:Key"]
            ?? throw new InvalidOperationException("GHN Token is not configured.");
        var configuredEndpoint = configuration["Ghn:Endpoint"] ?? configuration["GHN:Endpoint"]
            ?? "https://dev-online-gateway.ghn.vn/shiip/public-api/";
        var apiRoot = configuredEndpoint.TrimEnd('/');
        if (apiRoot.EndsWith("/v2", StringComparison.OrdinalIgnoreCase))
            apiRoot = apiRoot[..^3];
        var client = httpClientFactory.CreateClient();
        client.BaseAddress = new Uri($"{apiRoot}/master-data/");
        client.DefaultRequestHeaders.Add("Token", token);
        using var response = body is null
            ? await client.GetAsync(path)
            : await client.PostAsJsonAsync(path, body);
        var json = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Cannot load GHN administrative data: {json}");
        using var document = JsonDocument.Parse(json);
        return document.RootElement.TryGetProperty("data", out var data)
            ? data.Clone()
            : document.RootElement.Clone();
    }
}
