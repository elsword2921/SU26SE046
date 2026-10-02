using System.Security.Claims;
using BLL.Services.Implements.OperatingFund;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Capstone_API.Controllers;

[ApiController]
[Route("api/operating-fund/statements")]
[Authorize(Roles = OperatingFundService.Readers)]
[ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
public sealed class FundStatementsController(FundStatementService service) : ControllerBase
{
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    [HttpGet] public async Task<IActionResult> List(int page = 1) => Ok(await service.List(UserId, page));
    [HttpGet("status")][Authorize(Roles = "Manager")]
    public async Task<IActionResult> Status() => Ok(await service.Status(UserId));
    [HttpPost][Authorize(Roles = "Manager")][RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> Publish([FromForm] StatementForm input)
    {
        if (input.Document == null || input.Document.Length > 5 * 1024 * 1024) return BadRequest(new { message = "Chọn file sao kê JPG, PNG hoặc PDF, tối đa 5 MB." });
        await using var stream = new MemoryStream(); await input.Document.CopyToAsync(stream);
        return Ok(new { id = await service.Publish(UserId, new(input.RequestKey, input.Month, input.ClosingBalance, input.Note), stream.ToArray()) });
    }
    [HttpGet("{id:guid}/document")]
    public async Task<IActionResult> Document(Guid id)
    {
        var file = await service.Document(UserId, id);
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        var ext = file.ContentType == "application/pdf" ? "pdf" : file.ContentType == "image/png" ? "png" : "jpg";
        return File(file.Data, file.ContentType, $"sao-ke-{id}.{ext}");
    }
    public sealed class StatementForm
    {
        public Guid RequestKey { get; set; }
        public string Month { get; set; } = "";
        public decimal ClosingBalance { get; set; }
        public string? Note { get; set; }
        public IFormFile? Document { get; set; }
    }
}
