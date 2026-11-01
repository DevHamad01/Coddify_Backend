using System.Text.RegularExpressions;
using Application.Interfaces.CodeExecution;

namespace Infrastructure.Services.CodeExecution
{
    /// <summary>
    /// TEMPORARY until Judge0 is hosted (CodeRunner:Provider = "judge0"): it cannot execute functions,
    /// so it treats unchanged starter code as a wrong answer (the starter echoes its input) and edited
    /// code as correct. Same behaviour as the frontend mock, so the workspace flow can be built and tested.
    /// </summary>
    public class TemporaryProblemJudge : IProblemJudge
    {
        public Task<(IReadOnlyList<JudgeTestResult> Results, string? Error)> JudgeAsync(
            string language,
            string functionName,
            string code,
            string starterCode,
            IReadOnlyList<JudgeTest> tests,
            CancellationToken cancellationToken = default)
        {
            var untouched = Compact(code) == Compact(starterCode) || code.Contains("Write code here", StringComparison.Ordinal);
            IReadOnlyList<JudgeTestResult> results = tests.Select(test =>
            {
                var output = untouched ? test.Args.Trim('"') : test.Expected;
                return new JudgeTestResult(test.Key, output, output == test.Expected);
            }).ToList();
            return Task.FromResult<(IReadOnlyList<JudgeTestResult>, string?)>((results, null));
        }

        private static string Compact(string text) => Regex.Replace(text, @"\s+", "");
    }
}
