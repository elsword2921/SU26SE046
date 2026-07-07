using BLL.Services.Interfaces.WarehouseService;
using Microsoft.AspNetCore.Mvc;

namespace Capstone_API.Controllers
{
    [ApiController]
    [Route("api/warehouses")]
    public class WarehouseController
        : ControllerBase
    {
        private readonly IWarehouseService _service;

        public WarehouseController(
            IWarehouseService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result =
                await _service.GetAllAsync();

            return Ok(result);
        }
    }
}

