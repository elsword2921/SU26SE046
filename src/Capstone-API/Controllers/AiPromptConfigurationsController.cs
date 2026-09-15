using System.Security.Claims;
using BLL.DTOs;
using DAL;
using DAL.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Capstone_API.Controllers;

[ApiController]
[Route("api/ai-prompt-configurations")]
[Authorize(Roles = "Manager")]
public class AiPromptConfigurationsController(AppDbContext context) : ControllerBase
{
    public const string ClassificationFeature = "ClothingClassification";
    public const string DefaultClassificationPrompt =
        "Phân tích thận trọng dựa trên đặc điểm nhìn thấy trong ảnh. Ưu tiên đúng nhóm áo/quần, đối tượng sử dụng và giới tính; không suy đoán quá mức khi ảnh không rõ. Phần giải thích phải ngắn gọn bằng tiếng Việt.";

    [HttpGet("classification")]
    public async Task<IActionResult> GetClassificationPrompt()
    {
        var value = await context.AiPromptConfigurations.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Feature == ClassificationFeature && x.IsActive != false);
        return Ok(value is null
            ? new AiPromptConfigurationDto(null, ClassificationFeature, "Prompt phân loại mặc định", DefaultClassificationPrompt, true, true, null)
            : new AiPromptConfigurationDto(value.Id, value.Feature, value.Name, value.PromptText,
                value.Enabled, false, value.UpdateAt ?? value.CreateAt));
    }

    [HttpPut("classification")]
    public async Task<IActionResult> SaveClassificationPrompt(SaveAiPromptConfigurationDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name)) throw new InvalidOperationException("Prompt name is required.");
        if (string.IsNullOrWhiteSpace(dto.PromptText)) throw new InvalidOperationException("Prompt content is required.");
        if (dto.PromptText.Length > 12000) throw new InvalidOperationException("Prompt cannot exceed 12,000 characters.");
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var value = await context.AiPromptConfigurations
            .FirstOrDefaultAsync(x => x.Feature == ClassificationFeature);
        if (value is null)
        {
            value = new AiPromptConfiguration { Id = Guid.NewGuid(), Feature = ClassificationFeature,
                CreateAt = DateTime.UtcNow, CreatedBy = userId, IsActive = true };
            context.AiPromptConfigurations.Add(value);
        }
        value.Name = dto.Name.Trim(); value.PromptText = dto.PromptText.Trim(); value.Enabled = dto.Enabled;
        value.IsActive = true; value.UpdateAt = DateTime.UtcNow; value.UpdatedBy = userId;
        await context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("classification")]
    public async Task<IActionResult> ResetClassificationPrompt()
    {
        var value = await context.AiPromptConfigurations
            .FirstOrDefaultAsync(x => x.Feature == ClassificationFeature);
        if (value is not null) context.AiPromptConfigurations.Remove(value);
        await context.SaveChangesAsync();
        return NoContent();
    }
}
