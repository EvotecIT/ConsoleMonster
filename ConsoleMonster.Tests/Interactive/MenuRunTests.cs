using System;
using System.IO;
using System.Linq;
using System.Threading;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Interactive {
    /// <summary>
    /// A run started from a menu (or after a form shown on its own) draws inside the app's frame: a dashboard names the
    /// content panel and the breadcrumb with its title and shows its subtitle, status and clock as the panel caption; a run
    /// of any other widget is called "Run".
    /// </summary>
    [Collection("Live sessions")]
    public sealed class MenuRunTests {
        private const int Width = 90;
        private const int Height = 24;

        private static InteractiveOptions Options(ScreenCapture capture, string keys, GlyphTier tier = GlyphTier.Full) {
            return new InteractiveOptions {
                Mode = InteractionMode.Rich,
                Input = new ScriptedInput(keys) { Paced = true, RealTime = true },
                Capture = capture,
                Theme = Theme.Dark,
                Profile = TerminalProfile.Fixed(capture.Width, capture.Height, ColorDepth.TrueColor, tier),
                Labels = new PromptLabels()
            };
        }

        private static string Storage() => Path.Combine(Path.GetTempPath(), "cm-menu-run-" + Guid.NewGuid().ToString("N").Substring(0, 6));

        // A frame drawn while the run lasts: the footer offers ctrl+c and the caption shows the given text.
        private static CellBuffer RunFrame(ScreenCapture capture, string text) {
            CellBuffer? frame = capture.Frames.FirstOrDefault(f => f.GetRowText(f.Height - 1).Contains("ctrl+c") && f.ToPlainText().Contains(text));
            Assert.True(frame != null, "No run frame shows '" + text + "'. Last frame:\n" + capture.LastFrame?.ToPlainText());
            return frame!;
        }

        private static Menu Patching(IWidget shown, Action<RunContext>? during = null) {
            Menu menu = new Menu("Toolbox") { Subtitle = "lab" };
            menu.Item("Patch", i => i.Hotkey('p').Action(ctx => ctx.Run(shown, run => {
                Thread.Sleep(350);
                during?.Invoke(run);
                Thread.Sleep(350);
                return new RunResult("Done");
            })));
            menu.Exit("Quit");
            return menu;
        }

        [Theory]
        [InlineData(GlyphTier.Full, "›", "·")]
        [InlineData(GlyphTier.Ascii, ">", "|")]
        public void ShellRun_Dashboard_TitlesThePanelAndBreadcrumbAndCaptionsSubtitleStatusAndClock(GlyphTier tier, string crumb, string separator) {
            Dashboard dashboard = new Dashboard("[bold]Patching[/]", new ProgressBar("Overall", "overall") { Total = 2 }) { Subtitle = "4 servers", Status = "copying files", StatusLevel = StatusKind.Running };
            ScreenCapture capture = new ScreenCapture(Width, Height);
            Patching(dashboard, run => run.Session.Post(new LiveEvent("dashboard", "status", text: "verifying"))).Run(new MenuOptions { Layout = MenuLayout.Shell, Interactive = Options(capture, "Enter wait:1100 Escape Ctrl+Q", tier), StorageRoot = Storage() });

            CellBuffer running = RunFrame(capture, "4 servers  " + separator + "  copying files  " + separator + "  00:0");
            Assert.Contains("Toolbox " + crumb + " Patch " + crumb + " Patching", running.GetRowText(0));
            // The content panel's top border carries the dashboard's title (markup stripped), not "Run".
            Assert.Contains("Patching", running.GetRowText(1));
            Assert.DoesNotContain(" Run ", running.GetRowText(1));
            Assert.DoesNotContain("[bold]", running.ToPlainText());
            // A status the work posts while it runs replaces the caption's status on the next frame.
            RunFrame(capture, "4 servers  " + separator + "  verifying  " + separator + "  00:0");
        }

        [Fact]
        public void ShellRun_DashboardWithoutSubtitleOrStatus_CaptionsOnlyTheClock() {
            Dashboard dashboard = new Dashboard("Patching", new ProgressBar("Overall", "overall") { Total = 2 });
            ScreenCapture capture = new ScreenCapture(Width, Height);
            Patching(dashboard).Run(new MenuOptions { Layout = MenuLayout.Shell, Interactive = Options(capture, "Enter wait:1100 Escape Ctrl+Q"), StorageRoot = Storage() });

            CellBuffer running = RunFrame(capture, "00:0");
            Assert.Contains("Toolbox › Patch › Patching", running.GetRowText(0));
            Assert.DoesNotContain("·  00:0", running.ToPlainText());
        }

        // A menu whose run moves a simulated clock 83 s and returns a result without a duration.
        private static Menu Clocked(SimulatedClock clock, LiveSessionOptions options, Action<RunResult> done) {
            Menu menu = new Menu("Toolbox");
            menu.Item("Patch", i => i.Hotkey('p').Action(ctx => done(ctx.Run(new ProgressBar("Overall", "overall") { Total = 2 }, run => {
                clock.Advance(83);
                return new RunResult("Done");
            }, options))));
            menu.Exit("Quit");
            return menu;
        }

        [Fact]
        public void ShellRun_SimulatedClock_GivesTheResultTheClocksTime() {
            SimulatedClock clock = new SimulatedClock();
            RunResult? result = null;
            ScreenCapture capture = new ScreenCapture(Width, Height);
            Clocked(clock, clock.ToSessionOptions(), r => result = r).Run(new MenuOptions { Layout = MenuLayout.Shell, Interactive = Options(capture, "Enter wait:600 Escape Ctrl+Q"), StorageRoot = Storage() });
            Assert.NotNull(result);
            Assert.Equal(TimeSpan.FromSeconds(83), result!.Elapsed);
            Assert.Contains(capture.Frames, f => f.ToPlainText().Contains("00:01:23"));
        }

        [Fact]
        public void InlineRun_SimulatedClock_GivesTheResultTheClocksTime() {
            SimulatedClock clock = new SimulatedClock();
            LiveSessionOptions options = ConsoleMonster.Tests.Live.LiveSessionTests.Options(new ConsoleMonster.Tests.Live.FakeOutput(false));
            options.Clock = clock.Read;
            RunResult? result = null;
            RecordingSurface surface = new RecordingSurface(80, 20, true);
            PromptHarness.Guard(surface, () => {
                Clocked(clock, options, r => result = r).Run(new MenuOptions { Layout = MenuLayout.Inline, Interactive = PromptHarness.Options("p End Enter", surface) });
                return 0;
            });
            Assert.NotNull(result);
            Assert.Equal(TimeSpan.FromSeconds(83), result!.Elapsed);
        }

        [Fact]
        public void ShellRun_PlainWidget_IsTitledWithTheRunLabel() {
            ScreenCapture capture = new ScreenCapture(Width, Height);
            Patching(new ProgressBar("Overall", "overall") { Total = 2 }).Run(new MenuOptions { Layout = MenuLayout.Shell, Interactive = Options(capture, "Enter wait:1100 Escape Ctrl+Q"), StorageRoot = Storage() });

            CellBuffer running = RunFrame(capture, "00:0");
            Assert.Contains("Toolbox › Patch › Run", running.GetRowText(0));
            Assert.Contains("Run", running.GetRowText(1));
        }

        [Fact]
        public void FormRun_AfterSubmit_TitlesThePanelWithTheDashboard() {
            Form form = new Form("Settings") { Review = false };
            form.Page("General").Text("Name", "Name");
            Dashboard dashboard = new Dashboard("Applying settings", new ProgressBar("Overall", "overall") { Total = 1 }) { Subtitle = "lab-east", Status = "saving" };
            ScreenCapture capture = new ScreenCapture(Width, Height);
            FormResult result;
            try {
                result = form.Show(new FormOptions {
                    Interactive = Options(capture, "Enter Enter wait:1100 Enter"),
                    AfterSubmit = ctx => ctx.Run(dashboard, run => {
                        Thread.Sleep(700);
                        return new RunResult("Saved");
                    })
                });
            } catch (InputClosedException) {
                throw new Xunit.Sdk.XunitException("The script ended at this frame:\n" + capture.LastFrame?.ToPlainText());
            }

            Assert.True(result.Submitted);
            CellBuffer running = RunFrame(capture, "lab-east  ·  saving  ·  00:0");
            Assert.Contains("Settings › Applying settings", running.GetRowText(0));
            Assert.Contains("Applying settings", running.GetRowText(1));
        }
    }
}
