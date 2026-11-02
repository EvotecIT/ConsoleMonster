using System;
using System.Collections.Generic;
using System.Globalization;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Output;
using ConsoleMonster.Rendering;
using ConsoleMonster.Styling;
using ConsoleMonster.Tests.Live;
using ConsoleMonster.Widgets;
using Xunit;
using Calendar = ConsoleMonster.Widgets.Calendar;

namespace ConsoleMonster.Tests.Rendering {
    public sealed partial class BehaviourContractTests {
        [Theory]
        [InlineData(LiveMode.FullScreen, false, "environment")]
        [InlineData(LiveMode.FullScreen, true, "environment")]
        [InlineData(LiveMode.Inline, false, "environment")]
        [InlineData(LiveMode.Inline, true, "environment")]
        [InlineData(LiveMode.FullScreen, true, "defaults")]
        [InlineData(LiveMode.FullScreen, false, "plain")]
        public void AccessibleLive_NarratesWithoutEnteringALiveScreen(LiveMode mode, bool borrowedOutput, string source) {
            using var preferences = new AccessibilityEnvironment(source == "defaults" ? null : "1", null, source == "defaults");
            using var text = new StringWriter(CultureInfo.InvariantCulture);
            var profile = source == "plain" ? TerminalProfile.Fixed(48, 12, ColorDepth.None, GlyphTier.Ascii, OutputMode.Plain) : TerminalProfile.Fixed(48, 12);
            var target = new WriterTarget(text, profile);
            using var output = borrowedOutput ? new VtOutput(target.Writer!, profile, false) : null;
            using (var session = new LiveSession(new Spinner("Work", "work"), new LiveSessionOptions {
                Mode = mode, Profile = profile, Target = target, Output = output, Live = true,
                ReducedMotion = false, CaptureConsole = false, HandleCancelKey = false, TaskbarProgress = false,
                StreamHeartbeat = TimeSpan.Zero, StreamTimestamps = false
            })) {
                session.Start();
                session.Post(new LiveEvent("work", "start"));
                session.Post(new LiveEvent("work", "state", status: "pass"));
            }
            Assert.Contains("Work started", text.ToString());
            Assert.Contains("Work PASS", text.ToString());
            Assert.DoesNotContain("\u001b[?1049h", text.ToString());
            Assert.DoesNotContain("\u001b[2J", text.ToString());
            AssertPromptReady(text.ToString());
        }

        [Theory]
        [InlineData(null, "reduced", null, true)]
        [InlineData(null, "reduced", false, false)]
        [InlineData(null, null, true, true)]
        [InlineData("1", null, false, true)]
        public void MotionPreference_PreservesLayoutAndThrottlesLiveFrames(string? accessible, string? motion, bool? option, bool reduced) {
            using var preferences = new AccessibilityEnvironment(accessible, motion);
            long ticks = 0;
            var output = new FakeOutput();
            var times = new ConcurrentQueue<long>();
            output.Frame = () => times.Enqueue(Stopwatch.GetTimestamp());
            var target = new FakeTarget();
            RenderContext? rendered = null;
            target.Rendering = context => rendered = context;
            using var session = new LiveSession(target, new LiveSessionOptions {
                Output = output, Profile = output.Profile, CaptureConsole = false, HandleCancelKey = false,
                TaskbarProgress = false, FramesPerSecond = 60, ReducedMotion = option,
                Clock = () => TimeSpan.FromTicks(Interlocked.Read(ref ticks))
            });
            session.Start();
            if (accessible != null) {
                session.Post(new LiveEvent("work", "state", text: "narrated"));
                Assert.True(session.Flush(TimeSpan.FromSeconds(5)));
                Assert.Contains("narrated", output.Lines);
                Assert.Equal(0, output.Frames);
                return;
            }
            Assert.True(SpinWait.SpinUntil(() => Volatile.Read(ref output.Frames) > 0, TimeSpan.FromSeconds(5)));
            Assert.NotNull(rendered);
            Assert.Equal(OutputMode.Interactive, rendered!.Profile.Mode);
            Assert.Equal(reduced, rendered.ReducedMotion);
            if (reduced) {
                // A simulation/replay clock can advance rapidly; it must not accelerate physical redraws.
                Interlocked.Exchange(ref ticks, TimeSpan.FromHours(1).Ticks);
                session.Post(new LiveEvent("work", "state", text: "updated"));
                Assert.True(session.Flush(TimeSpan.FromSeconds(5)));
                Assert.Single(target.Events);
                Assert.False(SpinWait.SpinUntil(() => Volatile.Read(ref output.Frames) > 1, TimeSpan.FromMilliseconds(200)));
                Assert.True(SpinWait.SpinUntil(() => Volatile.Read(ref output.Frames) == 2, TimeSpan.FromSeconds(5)));
                long[] drawn = times.ToArray();
                Assert.True(drawn[1] - drawn[0] >= Stopwatch.Frequency, "Reduced motion must cap actual frame writes independently of the session clock.");
                session.Stop();
                Assert.Equal(3, output.Frames); // The final state is not lost to the throttle.
            } else {
                Assert.True(SpinWait.SpinUntil(() => Volatile.Read(ref output.Frames) >= 3, TimeSpan.FromSeconds(5)));
            }
        }

        [Fact]
        public async System.Threading.Tasks.Task ReducedMotionStatus_PublishesTheLatestUpdateWithoutAnAnimationTimer() {
            var result = await SessionProcessTests.Run("reduced-status");
            Assert.Equal(0, result.Exit);
            Assert.Contains("first", result.Output);
            Assert.Contains("[FAIL] second", result.Output);
            Assert.True(result.Output.IndexOf("second", StringComparison.Ordinal) < result.Output.IndexOf("CHECKPOINT", StringComparison.Ordinal),
                "The latest update must appear without another Show or finalization: " + result.Output + result.Error);
        }

        [Theory]
        [InlineData("highcontrast", false, false, ThemePreference.HighContrast)]
        [InlineData("colorblind", false, false, ThemePreference.ColorBlind)]
        [InlineData(null, true, true, ThemePreference.HighContrast)]
        [InlineData("auto", true, true, ThemePreference.HighContrast)]
        [InlineData("dark", true, true, ThemePreference.Dark)]
        [InlineData("light", true, true, ThemePreference.Light)]
        [InlineData(null, false, true, ThemePreference.Auto)]
        public void ThemeDetection_RespectsNamedPalettesAndWindowsHighContrast(string? name, bool windows, bool systemHighContrast, ThemePreference expected) {
            var variables = new Dictionary<string, string> { ["CONSOLEMONSTER_ACCESSIBLE"] = "1" };
            if (name != null) variables["CONSOLEMONSTER_THEME"] = name;
            var profile = new CapabilityDetector(new TerminalEnvironment(systemHighContrast, variables, isWindows: windows)).Detect();
            Assert.Equal(expected, profile.ThemePreference);
            if (expected == ThemePreference.HighContrast) Assert.Same(Theme.HighContrast, Terminal.ResolveTheme(profile));
            if (expected == ThemePreference.ColorBlind) Assert.Same(Theme.ColorBlindSafe, Terminal.ResolveTheme(profile));
            // Real rendering copies must retain accessible motion even after culture, size, mode and font refresh.
            profile = new CapabilityDetector(new TerminalEnvironment()).RefreshFont(profile.WithCulture(CultureInfo.InvariantCulture)
                .WithLanguage(CultureInfo.InvariantCulture).WithSize(30, 5).WithMode(OutputMode.Interactive), "Cascadia Mono");
            var context = new RenderContext(profile, Theme.Dark, tick: 17).WithClock(21, TimeSpan.FromSeconds(2));
            Assert.True(context.ReducedMotion);
            Assert.Equal(context.Glyphs.Get(Glyph.Running), context.Spinner());
            Assert.Equal(0, context.Tick);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void AccessibleQuestions_UseNumberedAnswers(bool environment) {
            using var preferences = new AccessibilityEnvironment(environment ? "1" : null, null);
            var lines = new global::ConsoleMonster.Tests.Interactive.PromptModeTests.FakeLines("2");
            var choice = new SelectPrompt<string>("Environment", new[] { "Production", "Staging" }).Show(new InteractiveOptions {
                Mode = environment ? InteractionMode.Rich : InteractionMode.Accessible,
                Profile = TerminalProfile.Fixed(48, 12), LineConsole = lines
            });
            Assert.Equal("Staging", choice);
            Assert.Contains("1", lines.Text);
            Assert.Contains("2", lines.Text);
            Assert.Contains("Staging", lines.Text);
            Assert.Equal(-1, lines.Text.IndexOf('\u001b'));
        }

        private sealed class AccessibilityEnvironment : IDisposable {
            private readonly string? _accessible = Environment.GetEnvironmentVariable("CONSOLEMONSTER_ACCESSIBLE");
            private readonly string? _motion = Environment.GetEnvironmentVariable("CONSOLEMONSTER_MOTION");
            private readonly InteractiveOptions _defaults = InteractiveOptions.Default;
            internal AccessibilityEnvironment(string? accessible, string? motion, bool accessibleDefault = false) {
                if (accessibleDefault) InteractiveOptions.Default = new InteractiveOptions { Mode = InteractionMode.Accessible };
                Environment.SetEnvironmentVariable("CONSOLEMONSTER_ACCESSIBLE", accessible);
                Environment.SetEnvironmentVariable("CONSOLEMONSTER_MOTION", motion);
            }
            public void Dispose() {
                InteractiveOptions.Default = _defaults;
                Environment.SetEnvironmentVariable("CONSOLEMONSTER_ACCESSIBLE", _accessible);
                Environment.SetEnvironmentVariable("CONSOLEMONSTER_MOTION", _motion);
            }
        }
    }
}
