namespace DAL.Models;

public class ClassificationScoringRule
{
    public int Id { get; set; } = 1;
    public decimal GradeAMinimum { get; set; } = 85;
    public decimal GradeBMinimum { get; set; } = 50;
    public DateTime? UpdatedAt { get; set; }
}
