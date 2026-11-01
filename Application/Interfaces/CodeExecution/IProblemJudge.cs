namespace Application.Interfaces.CodeExecution
{
    public record JudgeTest(string Key, string Args, string Expected);

    public record JudgeTestResult(string Key, string Output, bool Passed);

    /// <summary>Runs a function-style solution against test cases.</summary>
    public interface IProblemJudge
    {
        /// <param name="starterCode">The untouched template for this language (lets the temporary judge spot unedited code).</param>
        Task<(IReadOnlyList<JudgeTestResult> Results, string? Error)> JudgeAsync(
            string language,
            string functionName,
            string code,
            string starterCode,
            IReadOnlyList<JudgeTest> tests,
            CancellationToken cancellationToken = default);
    }
}
