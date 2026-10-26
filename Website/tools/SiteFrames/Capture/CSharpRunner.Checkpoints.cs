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
using ConsoleMonster.Styling;

namespace SiteFrames.Capture {
    internal static partial class CSharpRunner {
        /// <summary>Runs an ordinary compiled simulated program with logical checkpoints inside a recorded native child.</summary>
        internal static List<SiteFrames.Vt.OutputChunk> RecordCheckpoints(string dll, IEnumerable<string> arguments, int width, int height,
            IDictionary<string, string?> environment, string directory, TimeSpan timeout) =>
            TerminalCapture.Run(DotnetHost, new[] { typeof(CSharpRunner).Assembly.Location, "capture-csharp-worker", dll,
                width.ToString(CultureInfo.InvariantCulture), height.ToString(CultureInfo.InvariantCulture) }.Concat(arguments),
                width, height, environment, directory, timeout);

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
            if (stream) { Console.Write(target.Output); return 0; }
            if (target.Screens.Frames.Count == 0) { throw new InvalidOperationException("The program produced no logical frame checkpoints."); }
            // The shared terminal encoder owns alternate-screen transitions, frame diffs, widths and border rendering.
            using var output = ConsoleMonster.Terminal.CreateOutput();
            output.BeginLive();
            try { foreach (var frame in target.Screens.Frames) { output.RenderFrame(frame); } }
            finally { output.EndLive(); }
            output.WriteBlock(target.Transcript());
            return 0;
        }
    }
}
