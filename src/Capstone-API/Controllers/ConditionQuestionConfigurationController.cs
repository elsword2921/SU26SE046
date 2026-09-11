using BLL.DTOs;
using DAL;
using DAL.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Capstone_API.Controllers;

[ApiController]
[Route("api/condition-question-configurations")]
[Authorize(Roles = "Manager")]
public class ConditionQuestionConfigurationController(AppDbContext context) : ControllerBase
{
    [HttpGet("scoring")]
    public async Task<IActionResult> GetScoring() => Ok(await context.Set<ClassificationScoringRule>()
        .Where(x => x.Id == 1).Select(x => new ClassificationScoringRulesDto(x.GradeAMinimum, x.GradeBMinimum)).SingleAsync());

    [HttpPut("scoring")]
    public async Task<IActionResult> UpdateScoring(ClassificationScoringRulesDto dto)
    {
        BLL.Common.WeightedClassification.ValidateThresholds(dto.GradeAMinimum, dto.GradeBMinimum);
        var rule = await context.Set<ClassificationScoringRule>().SingleAsync(x => x.Id == 1);
        rule.GradeAMinimum = dto.GradeAMinimum;
        rule.GradeBMinimum = dto.GradeBMinimum;
        rule.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        return NoContent();
    }
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ConditionQuestionConfigDto>>> GetAll() => Ok(
        await context.ConditionQuestions.AsNoTracking().Where(x => x.IsActive != false)
            .OrderBy(x => x.DisplayOrder)
            .Select(x => new ConditionQuestionConfigDto(x.Id, x.QuestionText, x.DisplayOrder,
                x.Answers.Where(a => a.IsActive != false)
                    .OrderBy(a => a.ConditionRating)
                    .Select(a => new ConditionAnswerConfigDto(a.Id, a.AnswerText,
                        a.ConditionRating == 1 ? "A" : a.ConditionRating == 2 ? "B" : "C"))
                    .ToList(), x.Weight))
            .ToListAsync());

    [HttpPost]
    public async Task<IActionResult> Create(SaveConditionQuestionConfigDto dto)
    {
        Validate(dto);
        await using var transaction = await context.Database.BeginTransactionAsync();
        var activeQuestions = await context.ConditionQuestions
            .Where(x => x.IsActive != false).ToListAsync();
        var maximumOrder = activeQuestions.Count == 0 ? 1 : activeQuestions.Max(x => x.DisplayOrder) + 1;
        if (dto.DisplayOrder > maximumOrder)
            return BadRequest(new { message = $"Display order cannot exceed {maximumOrder}." });
        foreach (var existing in activeQuestions.Where(x => x.DisplayOrder >= dto.DisplayOrder))
        {
            existing.DisplayOrder++;
            existing.UpdateAt = DateTime.UtcNow;
        }
        var question = new ConditionQuestion
        {
            Id = Guid.NewGuid(), QuestionText = dto.QuestionText.Trim(), DisplayOrder = dto.DisplayOrder, Weight = dto.Weight,
            CreateAt = DateTime.UtcNow, IsActive = true
        };
        context.ConditionQuestions.Add(question);
        AddAnswers(question.Id, dto);
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
        return CreatedAtAction(nameof(GetAll), new { id = question.Id }, new { question.Id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, SaveConditionQuestionConfigDto dto)
    {
        Validate(dto);
        await using var transaction = await context.Database.BeginTransactionAsync();
        var question = await context.ConditionQuestions.Include(x => x.Answers)
            .FirstOrDefaultAsync(x => x.Id == id && x.IsActive != false);
        if (question is null) return NotFound();
        var activeQuestions = await context.ConditionQuestions
            .Where(x => x.IsActive != false).ToListAsync();
        var maximumOrder = activeQuestions.Max(x => x.DisplayOrder);
        if (dto.DisplayOrder > maximumOrder)
            return BadRequest(new { message = $"Display order cannot exceed {maximumOrder}." });
        var previousOrder = question.DisplayOrder;
        var questionAtTargetOrder = activeQuestions.FirstOrDefault(x => x.Id != id
            && x.DisplayOrder == dto.DisplayOrder);
        if (questionAtTargetOrder is not null)
        {
            questionAtTargetOrder.DisplayOrder = previousOrder;
            questionAtTargetOrder.UpdateAt = DateTime.UtcNow;
        }
        question.QuestionText = dto.QuestionText.Trim();
        question.DisplayOrder = dto.DisplayOrder;
        question.Weight = dto.Weight;
        question.UpdateAt = DateTime.UtcNow;
        var texts = new[] { dto.AnswerA.Trim(), dto.AnswerB.Trim(), dto.AnswerC.Trim() };
        for (var rating = 1; rating <= 3; rating++)
        {
            var answer = question.Answers.FirstOrDefault(x => x.ConditionRating == rating && x.IsActive != false);
            if (answer is null)
                context.ConditionAnswers.Add(NewAnswer(question.Id, texts[rating - 1], rating));
            else
            {
                answer.AnswerText = texts[rating - 1];
                answer.UpdateAt = DateTime.UtcNow;
            }
        }
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Disable(Guid id)
    {
        var question = await context.ConditionQuestions.Include(x => x.Answers)
            .FirstOrDefaultAsync(x => x.Id == id && x.IsActive != false);
        if (question is null) return NotFound();
        var deletedOrder = question.DisplayOrder;
        question.IsActive = false;
        question.DeleteAt = DateTime.UtcNow;
        foreach (var answer in question.Answers.Where(x => x.IsActive != false))
        {
            answer.IsActive = false;
            answer.DeleteAt = DateTime.UtcNow;
        }
        var followingQuestions = await context.ConditionQuestions.Where(x => x.IsActive != false
            && x.Id != id && x.DisplayOrder > deletedOrder).ToListAsync();
        foreach (var following in followingQuestions)
        {
            following.DisplayOrder--;
            following.UpdateAt = DateTime.UtcNow;
        }
        await context.SaveChangesAsync();
        return NoContent();
    }

    private void AddAnswers(Guid questionId, SaveConditionQuestionConfigDto dto)
    {
        context.ConditionAnswers.AddRange(NewAnswer(questionId, dto.AnswerA.Trim(), 1),
            NewAnswer(questionId, dto.AnswerB.Trim(), 2), NewAnswer(questionId, dto.AnswerC.Trim(), 3));
    }

    private static ConditionAnswer NewAnswer(Guid questionId, string text, int rating) => new()
    {
        Id = Guid.NewGuid(), ConditionQuestionId = questionId, AnswerText = text,
        ConditionRating = rating, CreateAt = DateTime.UtcNow, IsActive = true
    };

    private static void Validate(SaveConditionQuestionConfigDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.QuestionText) || string.IsNullOrWhiteSpace(dto.AnswerA)
            || string.IsNullOrWhiteSpace(dto.AnswerB) || string.IsNullOrWhiteSpace(dto.AnswerC))
            throw new InvalidOperationException("Question and all A/B/C answers are required.");
        if (dto.DisplayOrder < 1) throw new InvalidOperationException("Display order must be at least 1.");
        if (dto.Weight <= 0 || dto.Weight > 10000 || decimal.Round(dto.Weight, 2) != dto.Weight)
            throw new InvalidOperationException("Trọng số phải lớn hơn 0, không quá 10000 và tối đa 2 chữ số thập phân.");
    }
}
