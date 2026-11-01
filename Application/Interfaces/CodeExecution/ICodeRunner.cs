namespace Application.Interfaces.CodeExecution
{
    /// <summary>Output of one program run.</summary>
    public record CodeRunOutput(string Output, string? Error);

    /// <summary>
    /// Runs learner code. Today a built-in mini interpreter (Python/JavaScript basics); the Judge0
    /// implementation plugs in behind the same interface (CodeRunner:Provider = "judge0").
    /// </summary>
    public interface ICodeRunner
    {
        /// <param name="language">Monaco language id, e.g. "python", "javascript", "java".</param>
        Task<CodeRunOutput> RunAsync(string language, string code, CancellationToken cancellationToken = default);
    }
}
