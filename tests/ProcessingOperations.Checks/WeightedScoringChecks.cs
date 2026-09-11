using BLL.Common;

internal static class WeightedScoringChecks
{
    public static void Run()
    {
        void Check(bool value, string label) { if (!value) throw new Exception("FAIL: " + label); Console.WriteLine("PASS: " + label); }
        var weights = new[] { 35m, 20m, 15m, 30m };
        (decimal Score, int Rating) Score(params int[] ratings) => WeightedClassification.Calculate(weights.Zip(ratings).ToArray(), 85, 50);
        Check(Score(1, 1, 1, 1) == (100m, 1) && Score(2, 2, 2, 2) == (50m, 2) && Score(3, 3, 3, 3) == (0m, 3), "weighted all-A/B/C and inclusive B threshold");
        Check(Score(1, 2, 1, 1) == (90m, 1) && Score(1, 1, 1, 2) == (85m, 1), "weighted examples and inclusive A threshold");
        Check(Score(1, 1, 3, 1) == (85m, 1), "a C answer has no automatic veto");
        Check(WeightedClassification.Calculate([(7, 1), (4, 2), (3, 1), (6, 1)], 85, 50) == (90m, 1), "relative weights normalize independently of their sum");
        Check(WeightedClassification.Calculate([(1, 1), (2, 3)], 85, 50) == (33.33m, 3), "weighted rounding uses two decimals");
        foreach (var invalid in new IReadOnlyList<(decimal Weight, int Rating)>[] { [], [(0, 1)], [(-1, 1)], [(10001, 1)], [(1, 4)] })
        {
            var rejected = false;
            try { WeightedClassification.Calculate(invalid, 85, 50); } catch (InvalidOperationException) { rejected = true; }
            Check(rejected, "invalid weighted evaluation rejected");
        }
        var badThresholds = false;
        try { WeightedClassification.ValidateThresholds(50, 85); } catch (InvalidOperationException) { badThresholds = true; }
        Check(badThresholds, "inverted weighted grade thresholds rejected");
    }
}
