using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ConsoleMonster.Canvas;
using ConsoleMonster.Export;
using ConsoleMonster.Headless;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Sessions;
using ConsoleMonster.Serialization;
using ConsoleMonster.Styling;
using ConsoleMonster.Testing;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Headless {
    /// <summary>The headless driver runs the real app and shows what a terminal would; in-process recordings hold the composed screen.</summary>
    [Collection("Live sessions")]
    public sealed class HeadlessTerminalTests : IDisposable {
        private readonly string _folder = Path.Combine(Path.GetTempPath(), "cm-headless-" + Guid.NewGuid().ToString("N"));

        public HeadlessTerminalTests() { Directory.CreateDirectory(_folder); }

        [Fact]
        public void CompletedInput_HoldsItsTrailingPauseBeforeEndOfInput() {
            using HeadlessTerminal terminal = new HeadlessTerminal(40, 10);
            terminal.Send("Down wait:200");
            terminal.CompleteInput();
            Assert.NotNull(terminal.Input!.Read(TimeSpan.Zero, CancellationToken.None));
            Assert.Null(terminal.Input.Read(TimeSpan.Zero, CancellationToken.None));
            using CancellationTokenSource stop = new CancellationTokenSource();
            stop.Cancel();
            Assert.Throws<OperationCanceledException>(() => terminal.Input.Read(TimeSpan.FromSeconds(1), stop.Token));
            Assert.Throws<InputClosedException>(() => terminal.Input.Read(TimeSpan.FromSeconds(1), CancellationToken.None));
        }

        [Fact]
        public void Prompt_UsesOnlyTheLiveSessionOnItsOwnTarget() {
            using HeadlessTerminal dashboard = new HeadlessTerminal(80, 24);
            using HeadlessTerminal other = new HeadlessTerminal(60, 12);
            List<string> dashboardFrames = new List<string>();
            dashboard.FrameRendered += frame => { lock (dashboardFrames) { dashboardFrames.Add(frame.ToPlainText()); } };
            using LiveSession session = new LiveSession(TextBlock.Plain("Dashboard A"), new LiveSessionOptions {
                Target = dashboard, CaptureConsole = false, TaskbarProgress = false, HandleCancelKey = false
            });
            session.Start();
            dashboard.WaitForText("Dashboard A", TimeSpan.FromSeconds(10));
            other.Send("Enter");
            Assert.True(new ConfirmPrompt("Only terminal B", true).Show(new InteractiveOptions { Target = other, Mode = InteractionMode.Rich }));
            Assert.Contains("Only terminal B", other.Snapshot().ToPlainText());
            lock (dashboardFrames) { Assert.DoesNotContain(dashboardFrames, frame => frame.Contains("Only terminal B")); }
            dashboard.Send("Enter");
            Assert.True(new ConfirmPrompt("Same terminal A", true).Show(new InteractiveOptions { Target = dashboard, Mode = InteractionMode.Rich }));
            lock (dashboardFrames) { Assert.Contains(dashboardFrames, frame => frame.Contains("Same terminal A")); }
        }

        [Fact]
        public void CapturedFrames_AsciicastClearsRowsRemovedByResize() {
            CellBuffer large = new CellBuffer(20, 4);
            large.Write(0, 3, "old", Style.Plain);
            RecordedFrame[] frames = {
                new RecordedFrame(large, 0) { Duration = 100 },
                new RecordedFrame(new CellBuffer(10, 2), 100) { Duration = 100 },
                new RecordedFrame(new CellBuffer(20, 4), 200) { Duration = 100 }
            };
            string[] lines = Recording.Export(frames, ExportFormat.Asciicast).Trim().Split('\n');
            VtScreen screen = new VtScreen(20, 4);
            for (int i = 1; i <= frames.Length; i++) {
                var output = (List<object?>)MiniJson.Read(lines[i])!;
                screen.Feed((string)output[2]!);
                if (i == 1) { Assert.Contains("old", screen.Snapshot().ToPlainText()); }
                else { Assert.DoesNotContain("old", screen.Snapshot().ToPlainText()); }
            }
        }

        public void Dispose() { Directory.Delete(_folder, true); }

        private static IWidget Layout() {
            Select color = new Select("color", new[] { new Choice("Red", "Red"), new Choice("Green", "Green"), new Choice("Blue", "Blue") }) { Label = "Color" };
            AppBinding picked = new AppBinding(state => new Panel(TextBlock.Plain("Picked " + state.Get<string>("color"))) { Title = "Result" });
            return new Rows(color, picked);
        }

        /// <summary>
        /// The VT bytes the headless driver receives are the ones a real terminal receives (same output, same profile); after
        /// the built-in terminal model reads them, the screen must equal the frames the session composed, cell for cell.
        /// </summary>
        [Fact]
        public async Task App_OnHeadlessDriver_ShowsTheComposedFrameCellForCell() {
            using HeadlessTerminal terminal = new HeadlessTerminal(80, 24);
            ScreenCapture composed = new ScreenCapture(80, 24);
            ConsoleApp.Run(Layout(), options: new ConsoleAppOptions {
                Interactive = new InteractiveOptions {
                    Mode = InteractionMode.Rich, Input = TestTerminal.Keys("Down Enter Escape"), Capture = composed,
                    Theme = Theme.Dark, Profile = terminal.Profile, Labels = new PromptLabels(), Target = TestTerminal.Target()
                },
                Live = new LiveSessionOptions { CaptureConsole = false, TaskbarProgress = false }
            });

            List<CellBuffer> shown = new List<CellBuffer>();
            terminal.FrameRendered += frame => { lock (shown) { shown.Add(frame); } };
            Task<AppState> run = ConsoleApp.RunAsync(Layout(), options: new ConsoleAppOptions {
                Interactive = new InteractiveOptions { Target = terminal, Labels = new PromptLabels() },
                Live = new LiveSessionOptions { CaptureConsole = false, TaskbarProgress = false }
            });
            terminal.WaitForText("esc quit", TimeSpan.FromSeconds(10));
            terminal.Send("Down Enter");
            terminal.WaitForText("Picked Green", TimeSpan.FromSeconds(10));
            terminal.Send("Escape");
            Assert.Equal("Green", (await run)["color"]);

            CellBuffer expected = composed.LastFrame!;
            CellBuffer actual;
            lock (shown) { actual = shown.Last(frame => frame.ToPlainText().Contains("Picked Green")); }
            Assert.Equal(expected.Width, actual.Width);
            Assert.Equal(expected.Height, actual.Height);
            for (int y = 0; y < expected.Height; y++) {
                for (int x = 0; x < expected.Width; x++) {
                    // VT output never writes the bottom-right cell, so a terminal does not scroll.
                    if (y == expected.Height - 1 && x == expected.Width - 1) { continue; }
                    Assert.True(expected[x, y].Equals(actual[x, y]), "Cell " + x + "," + y + ": composed '" + expected[x, y].Glyph + "' " + expected[x, y].Style + ", shown '" + actual[x, y].Glyph + "' " + actual[x, y].Style);
                }
            }
        }

        /// <summary>
        /// An in-process app recording on any terminal holds what the user saw: the captured log panel, the footer and the
        /// command palette dialog, not only the app's own bindings.
        /// </summary>
        [Fact]
        public async Task SessionRecorder_RecordsAppFooterLogPanelAndDialog() {
            string path = Path.Combine(_folder, "app.cast");
            using HeadlessTerminal terminal = new HeadlessTerminal(80, 24);
            bool logged = false;
            using (SessionRecorder recorder = SessionRecorder.Start(new SessionRecordingOptions { Path = path, Width = 80, Height = 24, InProcess = true })) {
                ConsoleAppOptions options = new ConsoleAppOptions {
                    Interactive = new InteractiveOptions { Target = terminal, Labels = new PromptLabels() },
                    Live = new LiveSessionOptions { TaskbarProgress = false, Recorder = recorder },
                    ProcessEvents = state => { if (!logged) { logged = true; Console.WriteLine("deploy started"); } }
                };
                options.Command.Add(new AppCommand("deploy", "Deploy now", (context, token) => Task.CompletedTask));
                Task<AppState> run = ConsoleApp.RunAsync(Layout(), options: options);
                terminal.WaitForText("deploy started", TimeSpan.FromSeconds(10));
                terminal.Send("Ctrl+K");
                terminal.WaitForText("Deploy now", TimeSpan.FromSeconds(10));
                terminal.Send("Escape");
                terminal.WaitForText("esc quit", TimeSpan.FromSeconds(10));
                terminal.Send("Escape");
                await run;
            }

            SessionRecording recording = SessionRecording.Read(path);
            List<string> screens = recording.CaptureFrames(new RecordingExportOptions()).Select(frame => frame.Frame.ToPlainText()).ToList();
            Assert.Contains(screens, screen => screen.Contains("esc quit") && screen.Contains("deploy started") && screen.Contains("Picked"));
            Assert.Contains(screens, screen => screen.Contains("Deploy now") && screen.Contains("Picked"));
        }

        /// <summary>Captured screens leave through the public animation export as one animated SVG frame per screen.</summary>
        [Fact]
        public void CapturedFrames_ExportAsAnimatedSvg() {
            ScreenCapture capture = new ScreenCapture(40, 6);
            ConsoleApp.Run(Layout(), options: new ConsoleAppOptions {
                Interactive = TestTerminal.Interactive("Down Down Escape", capture),
                Live = new LiveSessionOptions { CaptureConsole = false, TaskbarProgress = false }
            });
            List<RecordedFrame> frames = capture.ToRecordedFrames(TimeSpan.FromMilliseconds(400), TimeSpan.FromSeconds(1));
            System.Xml.Linq.XDocument svg = System.Xml.Linq.XDocument.Parse(Recording.Export(frames, ExportFormat.Svg));
            System.Xml.Linq.XNamespace ns = "http://www.w3.org/2000/svg";
            Assert.Equal(frames.Count, svg.Descendants(ns + "g").Single(g => (string?)g.Attribute("class") == "cm-strip").Elements(ns + "g").Count());
            Assert.Contains("@keyframes cm-play", svg.ToString());
            Assert.True(frames.Count >= 3);
            Assert.Equal(frames.Sum(frame => frame.Duration), frames.Last().Start + frames.Last().Duration);
        }
    }
}
