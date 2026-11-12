namespace MathematicsLessons.Services;

/// <summary>
/// The single place that decides whether a student's answer is correct.
/// Both quiz entry points (the lesson form and the standalone practice page)
/// use this so grading rules can never drift apart.
/// </summary>
public static class AnswerChecker
{
    // Exercises whose solution is this sentinel are "show me" items and are not graded.
    private const string PracticeSentinel = "practice";

    /// <summary>
    /// True when <paramref name="given"/> matches the exercise's <paramref name="solution"/>.
    /// </summary>
    public static bool IsCorrect(string? solution, string? given)
    {
        var expected = Normalize(solution);
        var actual = Normalize(given);

        // A blank key must never behave like "any answer is correct".
        if (expected.Length == 0) return false;

        // Non-graded filler exercise: always passes.
        if (expected == PracticeSentinel) return true;

        return actual == expected || actual.Contains(expected, StringComparison.Ordinal);
    }

    // Case-insensitive, whitespace-insensitive comparison ("x = 5" == "X=5").
    private static string Normalize(string? value) =>
        (value ?? string.Empty).Replace(" ", string.Empty).ToLowerInvariant();
}
