using System.Security.Claims;
using System.Text.Json;
using BLL.Services.Implements.OperatingFund;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Capstone_API.Controllers;

[ApiController]
[Route("api/operating-fund")]
[Authorize(Roles = OperatingFundService.Readers)]
[ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
public class OperatingFundController(OperatingFundService service) : ControllerBase
{
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    [HttpGet("summary")] public async Task<IActionResult> Summary() => Ok(await service.Summary(UserId));
    [HttpGet("mine")] public async Task<IActionResult> Mine(int page = 1) => Ok(await service.Mine(UserId, page));
    [HttpGet("contributions")][Authorize(Roles = "Manager")]
    public async Task<IActionResult> Contributions(int page = 1) => Ok(await service.AllContributions(UserId, page));
    [HttpPost("checkout")][Authorize(Roles = OperatingFundService.Contributors)]
    public async Task<IActionResult> Checkout(CreateContribution input) => Ok(await service.Checkout(UserId, input));
    [HttpPost("contributions/{id:guid}/refresh")]
    public async Task<IActionResult> Refresh(Guid id) => Ok(await service.Refresh(UserId, id));
    [AllowAnonymous][HttpPost("payos/webhook")][RequestSizeLimit(65536)]
    public async Task<IActionResult> Webhook([FromBody] JsonElement body) { await service.Webhook(body); return Ok(new { success = true }); }
    [HttpGet("expenses")] public async Task<IActionResult> Expenses(int page = 1) => Ok(await service.ExpenseList(UserId, page));
    [HttpPost("expenses")][Authorize(Roles = "Manager")][RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> Publish([FromForm] ExpenseForm input)
    {
        if (input.Receipt is null || input.Receipt.Length > 5 * 1024 * 1024) return BadRequest(new { message = "Chọn chứng từ, tối đa 5 MB." });
        await using var stream = new MemoryStream(); await input.Receipt.CopyToAsync(stream);
        return Ok(new { id = await service.Publish(UserId, new(input.RequestKey, input.Amount, input.Title, input.Description, input.SpentOn), stream.ToArray()) });
    }
    [HttpPost("expenses/{id:guid}/void")][Authorize(Roles = "Manager")]
    public async Task<IActionResult> Void(Guid id, VoidForm input) { await service.Void(UserId, id, input.Reason); return NoContent(); }
    [HttpGet("expenses/{id:guid}/receipt")]
    public async Task<IActionResult> Receipt(Guid id)
    {
        var file = await service.Receipt(UserId, id);
        Response.Headers["Cache-Control"] = "private, no-store";
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        var extension = file.ContentType == "application/pdf" ? "pdf" : file.ContentType == "image/png" ? "png" : "jpg";
        return File(file.Data, file.ContentType, $"chung-tu-{id}.{extension}");
    }
    public sealed class ExpenseForm
    {
        public Guid RequestKey { get; set; }
        public decimal Amount { get; set; }
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public DateOnly SpentOn { get; set; }
        public IFormFile? Receipt { get; set; }
    }
    public record VoidForm(string Reason);
}
