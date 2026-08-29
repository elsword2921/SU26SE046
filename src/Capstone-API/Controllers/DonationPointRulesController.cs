using System.Security.Claims;
using DAL;
using DAL.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Capstone_API.Controllers;

[ApiController]
[Route("api/donation-point-rules")]
[Authorize(Roles = "Manager")]
public class DonationPointRulesController(AppDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var rule = await context.DonationPointRules.AsNoTracking()
            .Where(x => x.IsActive != false)
            .OrderByDescending(x => x.UpdateAt ?? x.CreateAt)
            .FirstOrDefaultAsync();

        if (rule is null)
            throw new InvalidOperationException("Donation point rule has not been configured.");

        return Ok(new DonationPointRuleDto(
            rule.PointsPerKg,
            rule.UpdateAt ?? rule.CreateAt));
    }

    [HttpPut]
    public async Task<IActionResult> Update(UpdateDonationPointRuleDto dto)
    {
        if (dto.PointsPerKg is < 1 or > 10000)
            throw new InvalidOperationException("Points per kg must be between 1 and 10,000.");

        var actorId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var rule = await context.DonationPointRules
            .OrderByDescending(x => x.UpdateAt ?? x.CreateAt)
            .FirstOrDefaultAsync();

        if (rule is null)
        {
            rule = new DonationPointRule
            {
                Id = Guid.NewGuid(), CreateAt = DateTime.UtcNow,
                CreatedBy = actorId, IsActive = true
            };
            context.DonationPointRules.Add(rule);
        }

        rule.PointsPerKg = dto.PointsPerKg;
        rule.IsActive = true;
        rule.UpdateAt = DateTime.UtcNow;
        rule.UpdatedBy = actorId;
        await context.SaveChangesAsync();
        return NoContent();
    }
}

public record DonationPointRuleDto(int PointsPerKg, DateTime? UpdatedAt);
public record UpdateDonationPointRuleDto(int PointsPerKg);
