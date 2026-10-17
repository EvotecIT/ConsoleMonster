using System.Collections.Generic;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Output;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Output {
    /// <summary>The OSC 9;4 terminal progress indicator: sequences, gating and what widgets report.</summary>
    public sealed class TaskbarProgressTests {
        [Theory]
        [InlineData(TaskbarState.Clear, 50, "\u001b]9;4;0;0\u0007")]
        [InlineData(TaskbarState.Normal, 42, "\u001b]9;4;1;42\u0007")]
        [InlineData(TaskbarState.Error, 80, "\u001b]9;4;2;80\u0007")]
        [InlineData(TaskbarState.Indeterminate, 10, "\u001b]9;4;3;0\u0007")]
        [InlineData(TaskbarState.Paused, 150, "\u001b]9;4;4;100\u0007")]
        public void Sequence_PerState(TaskbarState state, int percent, string expected) {
            Assert.Equal(expected, TaskbarProgress.Sequence(state, percent));
        }

        private static TerminalProfile Profile(TerminalKind kind, OutputMode mode = OutputMode.Interactive, bool redirected = false, bool ci = false) {
            return new TerminalProfile(ColorDepth.TrueColor, GlyphTier.Full, mode, 100, 30, kind, true, redirected, true, ci);
        }

        [Fact]
        public void IsSupported_OnlyInCapableInteractiveTerminals() {
            Assert.True(TaskbarProgress.IsSupported(Profile(TerminalKind.WindowsTerminal)));
            Assert.False(TaskbarProgress.IsSupported(Profile(TerminalKind.Conhost)));
            Assert.False(TaskbarProgress.IsSupported(Profile(TerminalKind.WindowsTerminal, OutputMode.Stream)));
            Assert.False(TaskbarProgress.IsSupported(Profile(TerminalKind.WindowsTerminal, redirected: true)));
            Assert.False(TaskbarProgress.IsSupported(Profile(TerminalKind.WindowsTerminal, ci: true)));
            bool enabled = TaskbarProgress.Enabled;
            try {
                TaskbarProgress.Enabled = false;
                Assert.False(TaskbarProgress.IsSupported(Profile(TerminalKind.WindowsTerminal)));
            } finally {
                TaskbarProgress.Enabled = enabled;
            }
        }

        [Fact]
        public void Combine_ErrorWinsThenWarningsThenPercentThenIndeterminate() {
            Assert.Equal(TaskbarState.Clear, TaskbarProgress.Combine(new KeyValuePair<TaskbarState, double>[0], out _));
            Assert.Equal(TaskbarState.Indeterminate, TaskbarProgress.Combine(new[] { Pair(TaskbarState.Indeterminate, double.NaN) }, out _));
            Assert.Equal(TaskbarState.Normal, TaskbarProgress.Combine(new[] { Pair(TaskbarState.Indeterminate, double.NaN), Pair(TaskbarState.Normal, 0.2), Pair(TaskbarState.Normal, 0.6) }, out int percent));
            Assert.Equal(40, percent);
            Assert.Equal(TaskbarState.Paused, TaskbarProgress.Combine(new[] { Pair(TaskbarState.Normal, 0.5), Pair(TaskbarState.Paused, double.NaN) }, out percent));
            Assert.Equal(50, percent);
            Assert.Equal(TaskbarState.Error, TaskbarProgress.Combine(new[] { Pair(TaskbarState.Paused, 0.5), Pair(TaskbarState.Error, 1) }, out percent));
            Assert.Equal(75, percent);
        }

        [Fact]
        public void Widgets_ReportTheirProgress() {
            Lanes lanes = new Lanes();
            lanes.Apply(new LiveEvent("lanes", "add", "a", "A", total: 10));
            lanes.Apply(new LiveEvent("lanes", "add", "b", "B", total: 10));
            lanes.Apply(new LiveEvent("lanes", "advance", "a", state: "pass", value: 5));
            Assert.True(lanes.TryGetTaskbarProgress(out TaskbarState state, out double fraction));
            Assert.Equal(TaskbarState.Normal, state);
            Assert.Equal(0.25, fraction, 3);
            lanes.Apply(new LiveEvent("lanes", "advance", "b", state: "fail", value: 1));
            Assert.True(lanes.TryGetTaskbarProgress(out state, out _));
            Assert.Equal(TaskbarState.Error, state);

            ProgressBar bar = new ProgressBar("copy") { Value = 30, Total = 60 };
            Assert.True(bar.TryGetTaskbarProgress(out state, out fraction));
            Assert.Equal(TaskbarState.Normal, state);
            Assert.Equal(0.5, fraction, 3);
            bar.Status = StatusKind.Pass;
            Assert.False(bar.TryGetTaskbarProgress(out _, out _));

            Spinner spinner = new Spinner("query");
            Assert.True(spinner.TryGetTaskbarProgress(out state, out _));
            Assert.Equal(TaskbarState.Indeterminate, state);

            Dashboard dashboard = new Dashboard("run", new Rows()) { StatusLevel = StatusKind.Warn };
            Assert.True(dashboard.TryGetTaskbarProgress(out state, out _));
            Assert.Equal(TaskbarState.Paused, state);
        }

        [Fact]
        public void Set_WritesNothingOutsideTheConsoleTarget() {
            using (Terminal.Use(ConsoleMonster.Testing.TestTerminal.Target())) {
                TaskbarProgress.Set(TaskbarState.Normal, 40);
            }
            Assert.NotEqual(TaskbarState.Normal, TaskbarProgress.Current);
        }

        private static KeyValuePair<TaskbarState, double> Pair(TaskbarState state, double fraction) => new KeyValuePair<TaskbarState, double>(state, fraction);
    }
}
