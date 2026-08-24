using BLL.Services.Interfaces.ManagerDashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Capstone_API.Controllers;

[ApiController]
[Route("api/manager-dashboard")]
[Authorize(Roles = "Manager,Admin")]
public class ManagerDashboardController(IManagerDashboardService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] Guid? warehouseId, [FromQuery] int? year,
        [FromQuery] int? month, [FromQuery] DateTime? date) =>
        Ok(await service.GetAsync(warehouseId, year, month, date));
}
