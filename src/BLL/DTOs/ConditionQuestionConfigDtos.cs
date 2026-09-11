namespace BLL.DTOs;

public record ConditionAnswerConfigDto(Guid Id, string Text, string Grade);
public record ConditionQuestionConfigDto(Guid Id, string QuestionText, int DisplayOrder,
    IReadOnlyList<ConditionAnswerConfigDto> Answers, decimal Weight = 1);

public record ClassificationScoringRulesDto(decimal GradeAMinimum = 85, decimal GradeBMinimum = 50);

public class SaveConditionQuestionConfigDto
{
    public string QuestionText { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public decimal Weight { get; set; } = 1;
    public string AnswerA { get; set; } = string.Empty;
    public string AnswerB { get; set; } = string.Empty;
    public string AnswerC { get; set; } = string.Empty;
}
