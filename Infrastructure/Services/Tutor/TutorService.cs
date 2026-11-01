using System.Text;
using System.Text.Json;
using Anthropic;
using Anthropic.Exceptions;
using Anthropic.Models.Beta.Messages;
using Application.Interfaces.Gamification;
using Application.Interfaces.Tutor;
using Application.VMs;
using Application.VMs.Tutor;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Tutor
{
    /// <summary>
    /// Answers with Claude when AI:ApiKey is configured, otherwise with built-in rule-based hints
    /// (the same replies the frontend mock gave), so the tutor always works in development.
    /// The prompt carries the lesson or problem, the learner's code and their last run; the model
    /// solution and hidden tests are never sent.
    /// </summary>
    public class TutorService : ITutorService
    {
        private const string DefaultModel = "claude-opus-5-5";

        private readonly AppDbContext _db;
        private readonly IStoreService _store;
        private readonly IConfiguration _config;
        private readonly ILogger<TutorService> _logger;
        private readonly AnthropicClient? _client;

        public TutorService(AppDbContext db, IStoreService store, IConfiguration config, ILogger<TutorService> logger)
        {
            _db = db;
            _store = store;
            _config = config;
            _logger = logger;
            var apiKey = config["AI:ApiKey"];
            _client = string.IsNullOrWhiteSpace(apiKey) ? null : new AnthropicClient { ApiKey = apiKey };
        }

        public async Task<ResponseVM> AskAsync(long userId, TutorRequestVM request, CancellationToken cancellationToken = default)
        {
            var context = await LoadContextAsync(request);
            if (context == null)
            {
                return ResponseVM.NotFound("That lesson or problem does not exist.");
            }

            var tutorName = await TutorNameAsync(userId);
            if (_client == null)
            {
                return ResponseVM.Ok(new TutorReplyVM { Text = RuleBasedReply(context, request, tutorName) });
            }

            try
            {
                var text = await AskClaudeAsync(context, request, tutorName, cancellationToken);
                return ResponseVM.Ok(new TutorReplyVM { Text = text });
            }
            catch (AnthropicRateLimitException ex)
            {
                _logger.LogWarning(ex, "Tutor rate limited by the AI provider.");
                return ResponseVM.Fail(429, "rate_limited", $"{tutorName} is helping a lot of learners right now. Try again in a minute.");
            }
            catch (AnthropicApiException ex)
            {
                // Fall back to the built-in hints rather than leaving the learner without help.
                _logger.LogError(ex, "Tutor AI request failed.");
                return ResponseVM.Ok(new TutorReplyVM { Text = RuleBasedReply(context, request, tutorName) });
            }
        }

        // ---- Claude ----

        private async Task<string> AskClaudeAsync(TutorContext context, TutorRequestVM request, string tutorName, CancellationToken cancellationToken)
        {
            var model = _config["AI:Model"] is { Length: > 0 } configured ? configured : DefaultModel;

            var response = await _client!.Beta.Messages.Create(new MessageCreateParams
            {
                Model = model,
                MaxTokens = 16000,
                // A chat reply, not deep reasoning: low effort keeps answers quick and cheap.
                OutputConfig = new BetaOutputConfig { Effort = Effort.Low },
                // If a safety classifier declines, the API picks a fallback model inside the same call.
                Betas = ["server-side-fallback-2026-07-01"],
                Fallbacks = new Default(),
                System = SystemPrompt(tutorName),
                Messages = [new BetaUserMessageParam { Content = UserPrompt(context, request) }],
            }, cancellationToken);

            if (response.StopReason == "refusal")
            {
                return "I can only help with this lesson's code. Try asking about the challenge or your error.";
            }

            var text = new StringBuilder();
            foreach (var block in response.Content)
            {
                if (block.TryPickText(out var part))
                {
                    text.Append(part.Text);
                }
            }
            return text.Length > 0 ? text.ToString().Trim() : RuleBasedReply(context, request, tutorName);
        }

        private static string SystemPrompt(string tutorName) =>
            $"""
            You are {tutorName}, the friendly AI coding tutor inside Coddify, a free app where beginners
            in Pakistan learn to code through short lessons and practice problems.

            How you help:
            - Guide the learner to the answer with hints, questions and small examples. Do not write the
              full solution to the challenge, even if asked; offer the next step instead.
            - When their code has an error, explain in plain words what the message means and where to look.
            - Keep replies short: 2 to 6 sentences, or a few short numbered steps.
            - Formatting: only **bold** and `inline code` are rendered. No headings, tables or code fences;
              put any code on its own line inside `backticks`.
            - If the question is not about programming or this exercise, steer back to the lesson kindly.
            """;

        private static string UserPrompt(TutorContext context, TutorRequestVM request)
        {
            var prompt = new StringBuilder();
            prompt.AppendLine($"<exercise kind=\"{context.Kind}\" language=\"{context.Language}\">");
            prompt.AppendLine($"Title: {context.Title}");
            prompt.AppendLine(context.Description);
            if (context.Expected.Count > 0)
            {
                prompt.AppendLine("Expected output from the tests:");
                context.Expected.ForEach(expected => prompt.AppendLine($"- {expected}"));
            }
            prompt.AppendLine("</exercise>");

            prompt.AppendLine("<learner_code>");
            prompt.AppendLine(string.IsNullOrWhiteSpace(request.Code) ? "(empty)" : request.Code);
            prompt.AppendLine("</learner_code>");

            if (request.LastRun is { } run)
            {
                prompt.AppendLine($"<last_run passed=\"{run.Passed.ToString().ToLowerInvariant()}\">");
                prompt.AppendLine($"Output: {(string.IsNullOrEmpty(run.Output) ? "(nothing)" : run.Output)}");
                if (!string.IsNullOrEmpty(run.Error))
                {
                    prompt.AppendLine($"Error: {run.Error}");
                }
                prompt.AppendLine("</last_run>");
            }

            prompt.AppendLine("<question>");
            prompt.AppendLine(request.Message);
            prompt.AppendLine("</question>");
            return prompt.ToString();
        }

        // ---- context ----

        private sealed record TutorContext(
            string Kind, string Title, string Language, string Description, string Intro,
            List<string> Steps, List<string> Expected);

        private async Task<TutorContext?> LoadContextAsync(TutorRequestVM request)
        {
            if (!string.IsNullOrWhiteSpace(request.LessonId))
            {
                var node = await _db.JourneyNodes.AsNoTracking()
                    .Include(x => x.Lesson).ThenInclude(x => x!.Tests)
                    .FirstOrDefaultAsync(x => x.Key == request.LessonId && x.IsPublished && !x.IsArchived);
                if (node?.Lesson == null)
                {
                    return null;
                }

                var challenge = JsonDocument.Parse(node.Lesson.ChallengeJson).RootElement;
                var intro = challenge.TryGetProperty("intro", out var i) ? i.GetString() ?? "" : "";
                var steps = challenge.TryGetProperty("steps", out var s)
                    ? s.EnumerateArray().Select(step => step.GetString() ?? "").ToList()
                    : new List<string>();
                return new TutorContext(
                    "lesson", node.Title, node.Lesson.EditorLanguage,
                    $"{node.Lesson.Summary}\nChallenge: {intro}\nSteps:\n{string.Join("\n", steps.Select((x, n) => $"{n + 1}. {x}"))}",
                    intro, steps,
                    node.Lesson.Tests.Where(t => !t.IsHidden && t.ExpectedOutput != null).Select(t => t.ExpectedOutput!).ToList());
            }

            if (!string.IsNullOrWhiteSpace(request.ProblemId))
            {
                var problem = await _db.Problems.AsNoTracking()
                    .Include(x => x.Tests)
                    .FirstOrDefaultAsync(x => x.Key == request.ProblemId && x.IsPublished && !x.IsArchived);
                if (problem == null)
                {
                    return null;
                }

                var detail = JsonDocument.Parse(problem.DetailJson).RootElement;
                var statement = detail.TryGetProperty("statement", out var st)
                    ? string.Join("\n", st.EnumerateArray().Select(x => x.GetString()))
                    : "";
                var language = detail.TryGetProperty("defaultLanguage", out var dl) ? dl.GetString() ?? "python" : "python";
                var hints = detail.TryGetProperty("hints", out var h) && h.ValueKind == JsonValueKind.Array
                    ? h.EnumerateArray().Select(x => x.GetString() ?? "").Where(x => x.Length > 0).ToList()
                    : new List<string>();
                return new TutorContext(
                    "problem", problem.Title, language, statement, statement, hints,
                    problem.Tests.Where(t => !t.IsHidden).OrderBy(t => t.Order).Take(3).Select(t => $"{t.Args} -> {t.Expected}").ToList());
            }

            return null;
        }

        private async Task<string> TutorNameAsync(long userId)
        {
            var items = await _store.GetItemsAsync(userId);
            var tutor = items.FirstOrDefault(x => x.Kind == "tutor" && x.State == "equipped");
            return tutor?.Name ?? "Bit";
        }

        // ---- built-in hints (no AI key) ----

        private static string RuleBasedReply(TutorContext context, TutorRequestVM request, string tutorName)
        {
            var question = request.Message.ToLowerInvariant();
            var run = request.LastRun;

            if (context.Kind == "problem" && question.Contains("hint") && context.Steps.Count > 0)
            {
                return $"Here's a nudge: {context.Steps[0]}\n\nStill stuck? Open the **Hints** tab for the next one.";
            }
            if (question.Contains("explain") && context.Steps.Count > 0)
            {
                var steps = string.Join("\n", context.Steps.Select((step, i) => $"{i + 1}. {step}"));
                return $"Sure! {context.Intro}\n\nHere is the plan:\n{steps}\n\nTry the first step, then press Run Code. I'll be right here.";
            }
            if (!string.IsNullOrEmpty(run?.Error))
            {
                var error = run.Error.Split('\n').Last();
                if (error.StartsWith("NameError") || error.StartsWith("ReferenceError"))
                {
                    return $"One of your names isn't defined yet:\n`{error}`\n\nCheck the spelling, and make sure the variable gets a value (with `=`) on a line *above* where you use it.";
                }
                if (error.StartsWith("SyntaxError"))
                {
                    return $"That's a syntax error:\n`{error}`\n\nLook for a missing quote or bracket, or a space inside a name. Names can't contain spaces, use `snake_case` instead.";
                }
                if (error.StartsWith("TypeError"))
                {
                    return $"Mixing types there:\n`{error}`\n\nText and numbers can't be added with `+`. Try an f-string like `f\"{{value}}\"` instead.";
                }
                return $"Your code stopped with:\n`{error}`\n\nRead the line number in the message and check that line first.";
            }
            if (run is { Passed: false })
            {
                var expected = context.Expected.FirstOrDefault();
                return expected != null
                    ? $"Close! Your output was:\n`{(string.IsNullOrEmpty(run.Output) ? "(nothing)" : run.Output)}`\n\nThe test expects:\n`{expected}`\n\nCompare them character by character: spacing and capital letters count."
                    : "Run the code once more and check the output panel.";
            }
            if (run is { Passed: true })
            {
                return "All tests pass. Nice work! Want a challenge? Change the code and predict the output before you run it.";
            }
            if (context.Steps.Count > 0)
            {
                return $"Let's start small. {context.Steps[0].Replace("**", "")}\n\nWhen you're ready, press **Run Code** and tell me what you see.";
            }
            return $"I'm {tutorName}, your AI tutor. Ask me about this exercise, an error, or for a hint.";
        }
    }
}
