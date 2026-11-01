using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Application.Interfaces.CodeExecution;

namespace Infrastructure.Services.CodeExecution
{
    /// <summary>
    /// Temporary code runner until Judge0 is hosted. A tiny, safe interpreter (no eval, no I/O) for the
    /// statements the beginner lessons use: print/console.log, variables, f-strings/template strings and
    /// arithmetic. Port of the frontend's mini-runner, so results match the demo exactly.
    /// Other languages fall back to echoing their string literals.
    /// </summary>
    public class MiniCodeRunner : ICodeRunner
    {
        private enum Lang { Python, JavaScript }

        private sealed class RunError : Exception
        {
            public RunError(string message) : base(message) { }
        }

        public Task<CodeRunOutput> RunAsync(string language, string code, CancellationToken cancellationToken = default)
        {
            if (language is not ("python" or "javascript" or "typescript"))
            {
                var strings = Regex.Matches(code, "\"([^\"\\\\]*)\"").Select(m => m.Groups[1].Value);
                return Task.FromResult(new CodeRunOutput(string.Join("\n", strings), null));
            }

            var lang = language == "python" ? Lang.Python : Lang.JavaScript;
            var vars = new Dictionary<string, object?>();
            var output = new List<string>();
            var lines = code.Replace("\r\n", "\n").Split('\n');

            for (var i = 0; i < lines.Length; i++)
            {
                var line = StripComment(lines[i], lang).Trim();
                if (line.EndsWith(';'))
                {
                    line = line[..^1];
                }
                if (line.Length == 0)
                {
                    continue;
                }
                try
                {
                    RunLine(line, lang, vars, output);
                }
                catch (RunError error)
                {
                    var where = lang == Lang.Python ? $"  File \"main.py\", line {i + 1}\n" : $"main.js:{i + 1}\n";
                    return Task.FromResult(new CodeRunOutput(string.Join("\n", output), where + error.Message));
                }
            }
            return Task.FromResult(new CodeRunOutput(string.Join("\n", output), null));
        }

        private static void RunLine(string line, Lang lang, Dictionary<string, object?> vars, List<string> output)
        {
            var print = lang == Lang.Python
                ? Regex.Match(line, @"^print\s*\((.*)\)$", RegexOptions.Singleline)
                : Regex.Match(line, @"^console\.log\s*\((.*)\)$", RegexOptions.Singleline);
            if (print.Success)
            {
                var values = SplitArgs(print.Groups[1].Value).Select(arg => Evaluate(arg, lang, vars));
                output.Add(string.Join(" ", values.Select(value => Display(value, lang))));
                return;
            }

            var assign = lang == Lang.Python
                ? Regex.Match(line, @"^([A-Za-z_]\w*)\s*=(?!=)\s*(.+)$")
                : Regex.Match(line, @"^(?:let|const|var)?\s*([A-Za-z_$][\w$]*)\s*=(?!=)\s*(.+)$");
            if (assign.Success)
            {
                vars[assign.Groups[1].Value] = Evaluate(assign.Groups[2].Value, lang, vars);
                return;
            }

            throw new RunError($"SyntaxError: the practice runner only understands simple statements: {line}");
        }

        private static string StripComment(string line, Lang lang)
        {
            var marker = lang == Lang.Python ? "#" : "//";
            char? quote = null;
            for (var i = 0; i < line.Length; i++)
            {
                var ch = line[i];
                if (quote != null)
                {
                    if (ch == '\\') i++;
                    else if (ch == quote) quote = null;
                }
                else if (ch is '"' or '\'' or '`')
                {
                    quote = ch;
                }
                else if (string.CompareOrdinal(line, i, marker, 0, marker.Length) == 0)
                {
                    return line[..i];
                }
            }
            return line;
        }

        private static List<string> SplitArgs(string text)
        {
            var args = new List<string>();
            var depth = 0;
            char? quote = null;
            var start = 0;
            for (var i = 0; i < text.Length; i++)
            {
                var ch = text[i];
                if (quote != null)
                {
                    if (ch == '\\') i++;
                    else if (ch == quote) quote = null;
                }
                else if (ch is '"' or '\'' or '`') quote = ch;
                else if (ch == '(') depth++;
                else if (ch == ')') depth--;
                else if (ch == ',' && depth == 0)
                {
                    args.Add(text[start..i]);
                    start = i + 1;
                }
            }
            var last = text[start..];
            if (last.Trim().Length > 0) args.Add(last);
            return args;
        }

        private static string Display(object? value, Lang lang) => value switch
        {
            null => lang == Lang.Python ? "None" : "null",
            bool b => lang == Lang.Python ? (b ? "True" : "False") : (b ? "true" : "false"),
            double d => FormatNumber(d),
            _ => value.ToString() ?? ""
        };

        private static string FormatNumber(double d)
        {
            if (double.IsPositiveInfinity(d)) return "Infinity";
            if (double.IsNegativeInfinity(d)) return "-Infinity";
            if (double.IsNaN(d)) return "NaN";
            return d == Math.Floor(d) && Math.Abs(d) < 1e15
                ? ((long)d).ToString(CultureInfo.InvariantCulture)
                : d.ToString("R", CultureInfo.InvariantCulture);
        }

        // ---- Expression parser (precedence climbing) ----

        private enum TokenKind { Num, Str, FStr, Name, Op, LParen, RParen }

        private sealed record Token(TokenKind Kind, string Text);

        private static readonly string[] Operators = { "**", "//", "+", "-", "*", "/", "%" };

        private static readonly Dictionary<string, int> Precedence = new()
        {
            ["+"] = 1, ["-"] = 1, ["*"] = 2, ["/"] = 2, ["//"] = 2, ["%"] = 2, ["**"] = 3
        };

        private static List<Token> Tokenize(string src, Lang lang)
        {
            var tokens = new List<Token>();
            var i = 0;
            while (i < src.Length)
            {
                var ch = src[i];
                if (char.IsWhiteSpace(ch))
                {
                    i++;
                    continue;
                }

                var fPrefix = lang == Lang.Python && (ch is 'f' or 'F') && i + 1 < src.Length && src[i + 1] is '"' or '\'';
                if (ch is '"' or '\'' or '`' || fPrefix)
                {
                    var quote = fPrefix ? src[i + 1] : ch;
                    var j = fPrefix ? i + 2 : i + 1;
                    var text = new StringBuilder();
                    while (j < src.Length && src[j] != quote)
                    {
                        if (src[j] == '\\' && j + 1 < src.Length)
                        {
                            var next = src[j + 1];
                            text.Append(next == 'n' ? '\n' : next == 't' ? '\t' : next);
                            j += 2;
                        }
                        else
                        {
                            text.Append(src[j++]);
                        }
                    }
                    if (j >= src.Length) throw new RunError("SyntaxError: unterminated string literal");
                    tokens.Add(new Token(fPrefix || quote == '`' ? TokenKind.FStr : TokenKind.Str, text.ToString()));
                    i = j + 1;
                    continue;
                }

                if (char.IsDigit(ch) || ch == '.')
                {
                    var match = Regex.Match(src[i..], @"^\d*\.?\d+");
                    if (!match.Success) throw new RunError($"SyntaxError: unexpected character '{ch}'");
                    tokens.Add(new Token(TokenKind.Num, match.Value));
                    i += match.Value.Length;
                    continue;
                }

                if (char.IsLetter(ch) || ch is '_' or '$')
                {
                    var match = Regex.Match(src[i..], @"^[A-Za-z_$][\w$]*");
                    tokens.Add(new Token(TokenKind.Name, match.Value));
                    i += match.Value.Length;
                    continue;
                }

                var op = Operators.FirstOrDefault(candidate => string.CompareOrdinal(src, i, candidate, 0, candidate.Length) == 0);
                if (op != null)
                {
                    tokens.Add(new Token(TokenKind.Op, op));
                    i += op.Length;
                    continue;
                }

                if (ch is '(' or ')')
                {
                    tokens.Add(new Token(ch == '(' ? TokenKind.LParen : TokenKind.RParen, ch.ToString()));
                    i++;
                    continue;
                }

                throw new RunError($"SyntaxError: unexpected character '{ch}'");
            }
            return tokens;
        }

        private static object? Evaluate(string src, Lang lang, Dictionary<string, object?> vars)
        {
            var tokens = Tokenize(src, lang);
            var pos = 0;

            object? Lookup(string name)
            {
                if (lang == Lang.Python)
                {
                    if (name == "True") return true;
                    if (name == "False") return false;
                    if (name == "None") return null;
                }
                else
                {
                    if (name == "true") return true;
                    if (name == "false") return false;
                    if (name == "null") return null;
                }
                if (vars.TryGetValue(name, out var value)) return value;
                throw new RunError(lang == Lang.Python
                    ? $"NameError: name '{name}' is not defined"
                    : $"ReferenceError: {name} is not defined");
            }

            object? Primary()
            {
                if (pos >= tokens.Count) throw new RunError("SyntaxError: expression ended too early");
                var token = tokens[pos++];
                switch (token.Kind)
                {
                    case TokenKind.Num:
                        return double.Parse(token.Text, CultureInfo.InvariantCulture);
                    case TokenKind.Str:
                        return token.Text;
                    case TokenKind.FStr:
                        var pattern = lang == Lang.Python ? @"\{([^}]+)\}" : @"\$\{([^}]+)\}";
                        return Regex.Replace(token.Text, pattern, m => Display(Evaluate(m.Groups[1].Value, lang, vars), lang));
                    case TokenKind.Name:
                        return Lookup(token.Text);
                    case TokenKind.LParen:
                        var value = Expression(0);
                        if (pos >= tokens.Count || tokens[pos++].Kind != TokenKind.RParen)
                            throw new RunError("SyntaxError: '(' was never closed");
                        return value;
                    case TokenKind.Op when token.Text == "-":
                        return -ToNumber(Primary());
                    default:
                        throw new RunError($"SyntaxError: unexpected '{token.Text}'");
                }
            }

            object? Expression(int minPrecedence)
            {
                var left = Primary();
                while (pos < tokens.Count && tokens[pos].Kind == TokenKind.Op && Precedence[tokens[pos].Text] >= minPrecedence)
                {
                    var op = tokens[pos].Text;
                    var precedence = Precedence[op];
                    pos++;
                    // ** is right-associative, the rest are left-associative.
                    var right = Expression(op == "**" ? precedence : precedence + 1);
                    left = Apply(op, left, right, lang);
                }
                return left;
            }

            var result = Expression(0);
            if (pos < tokens.Count) throw new RunError($"SyntaxError: invalid syntax near '{tokens[pos].Text}'");
            return result;
        }

        private static double ToNumber(object? value) => value switch
        {
            double d => d,
            bool b => b ? 1 : 0,
            _ => throw new RunError($"TypeError: expected a number, got '{value}'")
        };

        private static object? Apply(string op, object? a, object? b, Lang lang)
        {
            if (op == "+" && (a is string || b is string))
            {
                if (lang == Lang.Python && (a is not string || b is not string))
                    throw new RunError("TypeError: can only concatenate str (not \"int\") to str");
                return Display(a, lang) + Display(b, lang);
            }
            if (op == "*" && lang == Lang.Python && a is string text && b is double times)
            {
                return string.Concat(Enumerable.Repeat(text, Math.Max(0, (int)times)));
            }

            var x = ToNumber(a);
            var y = ToNumber(b);
            return op switch
            {
                "+" => x + y,
                "-" => x - y,
                "*" => x * y,
                "/" when y == 0 && lang == Lang.Python => throw new RunError("ZeroDivisionError: division by zero"),
                "/" => x / y,
                "//" when y == 0 => throw new RunError("ZeroDivisionError: integer division or modulo by zero"),
                "//" => Math.Floor(x / y),
                "%" => x % y,
                "**" => Math.Pow(x, y),
                _ => throw new RunError($"SyntaxError: unknown operator {op}")
            };
        }
    }
}
