using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using ConsoleMonster.Sessions;

namespace SiteFrames.Capture {
    internal static partial class CSharpRunner {
        /// <summary>Records an append-only Console report before terminal transport can coalesce its printed lines.
        /// The ordinary program still executes in a native child and writes its actual report to the native console.</summary>
        internal static List<SiteFrames.Vt.OutputChunk> RecordPrinted(string dll, IEnumerable<string> arguments, int width, int height,
            IDictionary<string, string?> environment, string directory, TimeSpan timeout) =>
            TerminalCapture.Run(DotnetHost, new[] { typeof(CSharpRunner).Assembly.Location, "capture-csharp-printed-worker", dll,
                width.ToString(CultureInfo.InvariantCulture), height.ToString(CultureInfo.InvariantCulture) }.Concat(arguments),
                width, height, environment, directory, timeout, hostedOutput: true);

        internal static int RunPrintedWorker(string[] args) {
            if (args.Length < 4) { throw new ArgumentException("A compiled program, width and height are required."); }
            CultureInfo.CurrentCulture = Site.Culture;
            CultureInfo.CurrentUICulture = Site.Culture;
            CultureInfo.DefaultThreadCurrentCulture = Site.Culture;
            CultureInfo.DefaultThreadCurrentUICulture = Site.Culture;
            Console.OutputEncoding = new UTF8Encoding(false);
            using var recorder = SessionRecorder.Start(new SessionRecordingOptions {
                InProcess = true, Width = int.Parse(args[2], CultureInfo.InvariantCulture), Height = int.Parse(args[3], CultureInfo.InvariantCulture),
                Path = Environment.GetEnvironmentVariable("SITEFRAMES_OUTPUT_RECORDING") ?? throw new InvalidOperationException("Missing hosted capture destination.")
            });
            TextWriter original = Console.Out;
            Console.SetOut(new PrintedWriter(original, recorder));
            try {
                MethodInfo entry = Assembly.LoadFrom(args[1]).EntryPoint ?? throw new InvalidOperationException("The report has no entry point.");
                object? result = entry.Invoke(null, entry.GetParameters().Length == 0 ? null : new object[] { args.Skip(4).ToArray() });
                return result is int code ? code : 0;
            } finally { Console.SetOut(original); }
        }

        private sealed class PrintedWriter : TextWriter {
            private readonly TextWriter _console;
            private readonly SessionRecorder _recorder;
            private bool _previousCarriageReturn;

            internal PrintedWriter(TextWriter console, SessionRecorder recorder) {
                _console = console; _recorder = recorder; NewLine = console.NewLine;
            }

            public override Encoding Encoding => _console.Encoding;
            public override void Flush() => _console.Flush();
            public override void Write(char value) => Write(value.ToString());
            public override void Write(char[] buffer, int index, int count) => Write(new string(buffer, index, count));
            public override void WriteLine() => WriteLine(string.Empty);
            public override void WriteLine(string? value) {
                // Frame the real printed line as one native update; both destinations receive the same VT boundaries.
                Write("\u001b[?2026h");
                Write((value ?? string.Empty) + NewLine);
                Write("\u001b[?2026l");
            }
            public override void Write(string? value) {
                if (value == null) { return; }
                _console.Write(value);
                // This entry point is append-only text: Unix LF has the same line meaning as Windows CRLF.
                var text = new StringBuilder(value.Length);
                foreach (char character in value) {
                    if (character == '\n' && !_previousCarriageReturn) { text.Append('\r'); }
                    text.Append(character);
                    _previousCarriageReturn = character == '\r';
                }
                _recorder.CaptureOutput(text.ToString());
                if (_recorder.LimitReached) { throw new IOException("Printed report reached its recorder byte limit."); }
            }
        }
    }
}
