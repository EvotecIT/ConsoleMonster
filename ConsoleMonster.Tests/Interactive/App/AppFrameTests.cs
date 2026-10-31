using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Testing;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Interactive.App {
    /// <summary>What a full-screen app frame holds: output written during the run, short windows and dialogs over one input.</summary>
    [Collection("Live sessions")]
    public sealed class AppFrameTests {
        private static (IWidget Layout, ConsoleAppOptions Options) DeployConsole(string keys, int width, int height, out ScreenCapture capture) {
            Select target = new Select("target", new[] { new Choice("Staging"), new Choice("Production"), new Choice("Lab forest") }) { Label = "Deploy to" };
            Toggle dry = new Toggle("dry") { Label = "Dry run" };
            capture = new ScreenCapture(width, height);
            InteractiveOptions interactive = TestTerminal.Interactive(keys, capture);
            interactive.Target = TestTerminal.Target();
            ConsoleAppOptions options = new ConsoleAppOptions { Interactive = interactive, Live = new LiveSessionOptions { CaptureConsole = false, TaskbarProgress = false } };
            options.Command.Add(new AppCommand("deploy", "Deploy now", (context, token) => Task.CompletedTask));
            options.Command.Add(new AppCommand("rollback", "Roll back last change", (context, token) => Task.CompletedTask));
            return (new Rows(new Columns().Add(target).Add(dry), new Panel(TextBlock.Plain("Target: Staging\nMode:   apply changes"), "Plan")), options);
        }

        /// <summary>A line a worker writes to the console while the app runs joins the app's action output; nothing lands below the footer.</summary>
        [Fact]
        public void ConsoleOutput_DuringTheApp_GoesToTheActionOutput() {
            (IWidget layout, ConsoleAppOptions options) = DeployConsole("", 90, 24, out ScreenCapture capture);
            options.Live = new LiveSessionOptions { CaptureConsole = true, TaskbarProgress = false };
            bool written = false;
            options.ProcessEvents = _ => { if (!written) { written = true; Console.WriteLine("[deploy] replication healthy"); } };
            ConsoleApp.Run(layout, options: options);
            string[] rows = capture.LastFrame!.ToPlainText().Split('\n');
            int line = Array.FindIndex(rows, row => row.Contains("[deploy] replication healthy"));
            int panel = Array.FindIndex(rows, row => row.Contains("Action output"));
            int footer = Array.FindIndex(rows, row => row.Contains("esc quit"));
            Assert.True(panel >= 0 && line > panel && line < footer, "the line is inside the action output panel, above the footer");
            Assert.Equal(footer, Array.FindLastIndex(rows, row => row.Trim().Length > 0));
        }

        /// <summary>
        /// In a window too short for every panel, the last panel shrinks to its border instead of being cut off under the
        /// action output; a panel never loses its bottom border to the panel below.
        /// </summary>
        [Fact]
        public void ShortWindow_ShrinksPanelsWithIntactBorders() {
            (IWidget layout, ConsoleAppOptions options) = DeployConsole("", 72, 14, out ScreenCapture capture);
            ConsoleApp.Run(layout, options: options);
            string[] rows = capture.LastFrame!.ToPlainText().Split('\n');
            int plan = Array.FindIndex(rows, row => row.Contains("─ Plan "));
            int output = Array.FindIndex(rows, row => row.Contains("Action output"));
            Assert.True(plan >= 0 && output > plan, "both panels are drawn");
            Assert.StartsWith("╰", rows[output - 1].TrimStart());
        }

        /// <summary>
        /// The command palette is one dialog with one title: the list inside it has no frame of its own, and the focused
        /// row's highlight spans the dialog's inner width.
        /// </summary>
        [Fact]
        public void Palette_HasOneFrameAndOneTitle() {
            (IWidget layout, ConsoleAppOptions options) = DeployConsole("Ctrl+K", 90, 24, out ScreenCapture capture);
            options.Interactive!.Profile = options.Interactive.Profile!.WithColorDepth(ColorDepth.TrueColor);
            ConsoleApp.Run(layout, options: options);
            string frame = capture.LastFrame!.ToPlainText();
            Assert.Contains("Roll back last change", frame);
            Assert.Single(Regex.Matches(frame, "Commands ─").Cast<Match>());
            string[] rows = frame.Split('\n');
            int y = Array.FindIndex(rows, row => row.Contains("Deploy now"));
            int start = rows[y].IndexOf("Deploy now", StringComparison.Ordinal);
            int border = rows[y].IndexOf('│', start);
            Assert.NotEqual(capture.LastFrame![start, y + 1].Style.BackgroundColor, capture.LastFrame[start, y].Style.BackgroundColor);
            Assert.Equal(capture.LastFrame[start, y].Style.BackgroundColor, capture.LastFrame[border - 2, y].Style.BackgroundColor);
        }
    }
}
