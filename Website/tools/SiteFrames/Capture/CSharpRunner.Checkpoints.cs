using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Interactive;
using ConsoleMonster.Output;
using ConsoleMonster.Sessions;
using ConsoleMonster.Styling;

namespace SiteFrames.Capture {
    internal static partial class CSharpRunner {
        /// <summary>Runs an ordinary compiled simulated program with logical checkpoints inside a recorded native child.</summary>
        internal static List<SiteFrames.Vt.OutputChunk> RecordCheckpoints(string dll, IEnumerable<string> arguments, int width, int height,
            IDictionary<string, string?> environment, string directory, TimeSpan timeout) =>
            TerminalCapture.Run(DotnetHost, new[] { typeof(CSharpRunner).Assembly.Location, "capture-csharp-worker", dll,
                width.ToString(CultureInfo.InvariantCulture), height.ToString(CultureInfo.InvariantCulture) }.Concat(arguments),
                width, height, environment, directory, timeout, hostedOutput: true);

        internal static int RunCheckpointWorker(string[] args) {
            if (args.Length < 4) { throw new ArgumentException("A compiled program, width and height are required."); }
            CultureInfo.CurrentCulture = Site.Culture;
            CultureInfo.CurrentUICulture = Site.Culture;
            CultureInfo.DefaultThreadCurrentCulture = Site.Culture;
            CultureInfo.DefaultThreadCurrentUICulture = Site.Culture;
            Console.OutputEncoding = new UTF8Encoding(false);
            int width = int.Parse(args[2], CultureInfo.InvariantCulture), height = int.Parse(args[3], CultureInfo.InvariantCulture);
            var profile = Profile(width, height, ColorDepth.TrueColor, GlyphTier.Full).WithMode(OutputMode.Interactive);
            bool stream = Environment.GetEnvironmentVariable("CONSOLEMONSTER_MODE") == "stream";
            if (stream) { profile = profile.WithMode(OutputMode.Stream); }
            var target = new RecordingTarget(profile) { SkipDelays = true, CaptureViewport = true, Theme = Theme.Dark };
            ExecuteCompiled(Assembly.Load(File.ReadAllBytes(args[1])), args[1], profile, Theme.Dark,
                new InteractiveOptions { Target = target, Profile = profile, NonInteractive = true }, args.Skip(4).ToArray(), target, false);
            using var recorder = SessionRecorder.Start(new SessionRecordingOptions { InProcess = true, Width = width, Height = height,
                Path = Environment.GetEnvironmentVariable("SITEFRAMES_OUTPUT_RECORDING") ?? throw new InvalidOperationException("Missing hosted capture destination.") });
            if (stream) {
                recorder.CaptureOutput(target.Output);
                if (recorder.LimitReached) { throw new IOException("Hosted capture reached its recorder byte limit."); }
                return 0;
            }
            if (target.Screens.Frames.Count == 0) { throw new InvalidOperationException("The program produced no logical frame checkpoints."); }
            // The shared terminal encoder owns alternate-screen transitions, frame diffs, widths and border rendering.
            using var writer = new StringTerminalWriter();
            using var output = ConsoleMonster.Terminal.CreateOutput(profile, writer);
            void CaptureWritten() {
                recorder.CaptureOutput(writer.ToString());
                writer.Clear();
                if (recorder.LimitReached) { throw new IOException("Hosted capture reached its recorder byte limit."); }
            }
            output.BeginLive();
            CaptureWritten();
            try {
                for (int index = 0; index < target.Screens.Frames.Count; index++) {
                    output.RenderFrame(target.Screens.Frames[index]);
                    CaptureWritten();
                    if (target.Screens.FrameHolds.TryGetValue(index, out TimeSpan hold)) { recorder.AddMarker(hold); }
                }
            }
            finally { output.EndLive(); }
            output.WriteBlock(target.Transcript());
            CaptureWritten();
            return 0;
        }
    }
}
