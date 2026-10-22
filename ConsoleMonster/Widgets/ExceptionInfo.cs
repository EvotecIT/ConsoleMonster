using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ConsoleMonster.Widgets {
    /// <summary>
    /// What <see cref="ExceptionView"/> shows of an error: the type, the message, the stack frames, the inner error and,
    /// for PowerShell errors, the category, the error id, the script stack and the position message. Built from a .NET
    /// exception with <see cref="FromException"/>, or filled in by a host (the PowerShell module maps ErrorRecords).
    /// </summary>
    public sealed class ExceptionInfo {
        private static readonly Regex DotNetFrame = new Regex(@"^\s*at (?<method>.+?)(?: in (?<file>.+?):line (?<line>\d+))?\s*$", RegexOptions.CultureInvariant);
        private static readonly Regex ScriptFrame = new Regex(@"^\s*at (?<method>.+?), (?<file>.+?): line (?<line>\d+)\s*$", RegexOptions.CultureInvariant);

        /// <summary>Creates an error description.</summary>
        /// <param name="type">The type name (short or full).</param>
        /// <param name="message">The message.</param>
        public ExceptionInfo(string type, string message) {
            Type = type ?? string.Empty;
            Message = message ?? string.Empty;
        }

        /// <summary>The type name.</summary>
        public string Type { get; }

        /// <summary>The message.</summary>
        public string Message { get; }

        /// <summary>The .NET stack frames, innermost call first, as the runtime lists them.</summary>
        public List<ExceptionFrame> Frames { get; } = new List<ExceptionFrame>();

        /// <summary>The PowerShell script stack (commands and script lines), or empty.</summary>
        public List<ExceptionFrame> ScriptFrames { get; } = new List<ExceptionFrame>();

        /// <summary>The PowerShell position message ("At C:\x.ps1:12 char:5" with the line and the marker), or null.</summary>
        public string? PositionMessage { get; set; }

        /// <summary>The original invocation line, without the host's position-message prefixes.</summary>
        public string? SourceLine { get; set; }

        /// <summary>The invocation's script path, when known.</summary>
        public string? ScriptPath { get; set; }

        /// <summary>The one-based source line number, or zero when unavailable.</summary>
        public int LineNumber { get; set; }

        /// <summary>The one-based UTF-16 offset of the failing expression, or zero when unavailable.</summary>
        public int ColumnNumber { get; set; }

        /// <summary>The failing expression's length in UTF-16 characters (default one).</summary>
        public int ErrorLength { get; set; } = 1;

        /// <summary>The PowerShell category and reason, for example "ObjectNotFound (C:\missing:String)", or null.</summary>
        public string? Category { get; set; }

        /// <summary>The PowerShell error target, when supplied by the producer.</summary>
        public string? Target { get; set; }

        /// <summary>The PowerShell error id, for example "PathNotFound,Microsoft.PowerShell.Commands.GetItemCommand", or null.</summary>
        public string? ErrorId { get; set; }

        /// <summary>The error that caused this one, or null.</summary>
        public ExceptionInfo? Inner { get; set; }

        /// <summary>Describes a .NET exception and its inner exceptions (aggregate exceptions follow their first inner).</summary>
        /// <param name="exception">The exception.</param>
        /// <param name="depth">How many inner exceptions to follow (default 8).</param>
        /// <returns>The description.</returns>
        public static ExceptionInfo FromException(Exception exception, int depth = 8) {
            if (exception == null) {
                throw new ArgumentNullException(nameof(exception));
            }
            ExceptionInfo info = new ExceptionInfo(exception.GetType().Name, exception.Message);
            info.Frames.AddRange(ParseStackTrace(exception.StackTrace));
            Exception? inner = exception is AggregateException aggregate && aggregate.InnerExceptions.Count > 0 ? aggregate.InnerExceptions[0] : exception.InnerException;
            if (inner != null && depth > 0) {
                info.Inner = FromException(inner, depth - 1);
            }
            return info;
        }

        /// <summary>Parses a .NET stack trace ("at Type.Method(args) in C:\file.cs:line 42", one frame per line).</summary>
        /// <param name="stackTrace">The stack trace text, or null.</param>
        /// <returns>The frames.</returns>
        public static List<ExceptionFrame> ParseStackTrace(string? stackTrace) {
            List<ExceptionFrame> frames = new List<ExceptionFrame>();
            if (string.IsNullOrEmpty(stackTrace)) {
                return frames;
            }
            foreach (string raw in stackTrace!.Replace("\r\n", "\n").Split('\n')) {
                Match match = DotNetFrame.Match(raw);
                if (!match.Success) {
                    continue;
                }
                string method = match.Groups["method"].Value;
                int line = match.Groups["line"].Success ? int.Parse(match.Groups["line"].Value, CultureInfo.InvariantCulture) : 0;
                frames.Add(new ExceptionFrame(method, match.Groups["file"].Success ? match.Groups["file"].Value : null, line, IsFrameworkMethod(method)));
            }
            return frames;
        }

        /// <summary>Parses a PowerShell script stack trace ("at Get-Thing, C:\x.ps1: line 12", "at &lt;ScriptBlock&gt;, &lt;No file&gt;: line 1").</summary>
        /// <param name="scriptStackTrace">The script stack trace text, or null.</param>
        /// <returns>The frames.</returns>
        public static List<ExceptionFrame> ParseScriptStackTrace(string? scriptStackTrace) {
            List<ExceptionFrame> frames = new List<ExceptionFrame>();
            if (string.IsNullOrEmpty(scriptStackTrace)) {
                return frames;
            }
            foreach (string raw in scriptStackTrace!.Replace("\r\n", "\n").Split('\n')) {
                Match match = ScriptFrame.Match(raw);
                if (!match.Success) {
                    continue;
                }
                string file = match.Groups["file"].Value;
                bool noFile = file.StartsWith("<", StringComparison.Ordinal);
                frames.Add(new ExceptionFrame(match.Groups["method"].Value, noFile ? null : file, int.Parse(match.Groups["line"].Value, CultureInfo.InvariantCulture), false));
            }
            return frames;
        }

        /// <summary>Whether a method belongs to the framework (System, Microsoft, the PowerShell engine) rather than the app.</summary>
        /// <param name="method">The method as the stack trace names it.</param>
        /// <returns>True for framework frames.</returns>
        public static bool IsFrameworkMethod(string method) {
            return method.StartsWith("System.", StringComparison.Ordinal)
                || method.StartsWith("Microsoft.", StringComparison.Ordinal)
                || method.StartsWith("Internal.", StringComparison.Ordinal)
                || method.StartsWith("lambda_method", StringComparison.Ordinal)
                || method.StartsWith("Pester.", StringComparison.Ordinal)
                || method.StartsWith("CallSite.", StringComparison.Ordinal)
                || method.StartsWith("DynamicClass.", StringComparison.Ordinal);
        }
    }

    /// <summary>One frame of a stack trace.</summary>
    public sealed class ExceptionFrame {
        /// <summary>Creates a frame.</summary>
        /// <param name="method">The method, command or script block.</param>
        /// <param name="file">The file, or null.</param>
        /// <param name="line">The line, or 0.</param>
        /// <param name="isFramework">Whether it is a framework frame (collapsed by default).</param>
        public ExceptionFrame(string method, string? file, int line, bool isFramework) {
            Method = method ?? string.Empty;
            File = string.IsNullOrEmpty(file) ? null : file;
            Line = line;
            IsFramework = isFramework;
        }

        /// <summary>The method, command or script block.</summary>
        public string Method { get; }

        /// <summary>The file, or null.</summary>
        public string? File { get; }

        /// <summary>The line, or 0.</summary>
        public int Line { get; }

        /// <summary>Whether it is a framework frame.</summary>
        public bool IsFramework { get; }
    }
}
