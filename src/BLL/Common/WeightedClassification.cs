namespace BLL.Common;

public static class WeightedClassification
{
    public static void ValidateThresholds(decimal a, decimal b)
    {
        if (b < 0 || a > 100 || a <= b || decimal.Round(a, 2) != a || decimal.Round(b, 2) != b)
            throw new InvalidOperationException("Ngưỡng phải thỏa 0 ≤ B < A ≤ 100, tối đa 2 chữ số thập phân.");
    }

    public static (decimal Score, int Rating) Calculate(IReadOnlyList<(decimal Weight, int Rating)> answers, decimal a, decimal b)
    {
        ValidateThresholds(a, b);
        if (answers.Count == 0 || answers.Any(x => x.Weight <= 0 || x.Weight > 10000 || x.Rating is < 1 or > 3))
            throw new InvalidOperationException("Cần ít nhất một tiêu chí có trọng số hợp lệ và đủ đáp án.");
        var score = decimal.Round(answers.Sum(x => x.Weight * (x.Rating == 1 ? 100m : x.Rating == 2 ? 50m : 0m))
            / answers.Sum(x => x.Weight), 2, MidpointRounding.AwayFromZero);
        return (score, score >= a ? 1 : score >= b ? 2 : 3);
    }
}
