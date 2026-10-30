using System;
using System.Linq;
using ChartForgeX.Topology;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Export;
using ConsoleMonster.Graphics;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;
using ConsoleMonster.Live;
using ConsoleMonster.Rendering;
using ConsoleMonster.Styling;
using ConsoleMonster.Testing;
using ConsoleMonster.Tests.Rendering;
using Xunit;

namespace ConsoleMonster.Extensions.Tests.Graphics {
    public sealed class TopologyDiagramTests {
        private static RenderContext Context(int width, GlyphTier tier = GlyphTier.Full, ColorDepth depth = ColorDepth.None) =>
            new RenderContext(TerminalProfile.Fixed(width, 60, depth, tier), Theme.Dark);

        private static string Text(TopologyDiagram view, int width, GlyphTier tier = GlyphTier.Full) =>
            FrameComposer.RenderBlock(view, width, Context(width, tier)).ToPlainText();

        [Fact]
        public void AdReplication_DiagramAndUseCasePanel_MatchTheGoldenCapture() {
            var view = new TopologyDiagram(TopologyFixtures.AdReplication());

            Golden.Match("topology-ad-replication-130.txt", Text(view, 130));
        }

        [Fact]
        public void AdReplication_ActiveStep_HighlightsTheEdgeAndShowsItsPayload() {
            var view = new TopologyDiagram(TopologyFixtures.AdReplication());
            view.Step(1);
            string text = Text(view, 130);

            Golden.Match("topology-ad-replication-step-2-130.txt", text);
            Assert.Contains("▶ 2. ADMIN-PC → DC01  request  LDAP modify", text, StringComparison.Ordinal);
            Assert.Contains("\"replace\": { \"title\": \"Site manager\" }", text, StringComparison.Ordinal);
            Assert.Contains("┏", text, StringComparison.Ordinal);
        }

        [Fact]
        public void TierBridge_AsciiTier_UsesOnlyAsciiAndKeepsTheFailurePath() {
            var view = new TopologyDiagram(TopologyFixtures.TierBridge());
            view.Step(7);
            string text = Text(view, 140, GlyphTier.Ascii);

            Golden.Match("topology-tierbridge-step-8-ascii-140.txt", text);
            Assert.All(text, ch => Assert.True(ch < 128, "Non-ASCII U+" + ((int)ch).ToString("X4")));
            Assert.Contains("> 8. Approver -> Approval  request  Approve", text, StringComparison.Ordinal);
        }

        [Fact]
        public void BasicTier_MapsRoundedDashedAndHeavyGlyphsToWgl4() {
            var view = new TopologyDiagram(TopologyFixtures.TierBridge());
            view.Step(2);
            string text = Text(view, 120, GlyphTier.Basic);

            foreach (string glyph in new[] { "╭", "╌", "╎", "━", "┏", "▶" }) Assert.DoesNotContain(glyph, text, StringComparison.Ordinal);
            Assert.Contains("╔", text, StringComparison.Ordinal);
            Assert.Contains("►", text, StringComparison.Ordinal);
        }

        [Fact]
        public void Keys_StepFocusAndUseCase_ChangeTheView() {
            var view = new TopologyDiagram(TopologyFixtures.AdReplication());

            Assert.True(view.HandleInput(InputEvent.Character('n')));
            Assert.Equal(0, view.StepIndex);
            Assert.True(view.HandleInput(InputEvent.Press(ConsoleKey.RightArrow)));
            Assert.True(view.HandleInput(InputEvent.Press(ConsoleKey.End)));
            Assert.Equal(8, view.StepIndex);
            Assert.True(view.HandleInput(InputEvent.Character('b')));
            Assert.Equal(7, view.StepIndex);
            Assert.True(view.HandleInput(InputEvent.Character('r')));
            Assert.Equal(0, view.StepIndex);
            Assert.True(view.HandleInput(InputEvent.Press(ConsoleKey.DownArrow)));
            Assert.Equal("admin", view.FocusNodeId);
            Assert.True(view.HandleInput(InputEvent.Press(ConsoleKey.Tab)));
            Assert.Equal("link-down", view.ScenarioId);
            Assert.Null(view.StepIndex);
            Assert.False(view.HandleInput(InputEvent.Character('z')));
            Assert.False(view.HandleInput(InputEvent.Character('n', KeyModifiers.Control)));
        }

        [Fact]
        public void Playback_AdvancesStepsWithTheSessionClockAndStopsAtTheEnd() {
            var view = new TopologyDiagram(TopologyFixtures.AdReplication().WithPlaybackForTest(400));
            view.Play();
            RenderContext context = Context(130);
            Render(view, context.WithClock(0, TimeSpan.FromSeconds(10)));
            Assert.Equal(0, view.StepIndex);
            Render(view, context.WithClock(1, TimeSpan.FromSeconds(10.85)));
            Assert.Equal(2, view.StepIndex);
            Assert.True(view.Playing);
            Render(view, context.WithClock(2, TimeSpan.FromSeconds(30)));
            Assert.Equal(8, view.StepIndex);
            Assert.False(view.Playing);
        }

        [Fact]
        public void Playback_ClockGoingBackwards_RestartsFromTheCurrentStep() {
            var view = new TopologyDiagram(TopologyFixtures.AdReplication().WithPlaybackForTest(400));
            view.Play();
            RenderContext context = Context(130);
            Render(view, context.WithClock(0, TimeSpan.FromSeconds(10.5)));
            Assert.Equal(0, view.StepIndex);
            Render(view, context.WithClock(1, TimeSpan.Zero));
            Assert.Equal(1, view.StepIndex);
            Assert.True(view.Playing);
        }

        [Fact]
        public void Constructor_UseCaseWithMissingEdge_IsSkipped() {
            TopologyChart chart = TopologyFixtures.AdReplication();
            chart.Scenarios.Insert(0, new TopologyScenario { Id = "broken", Label = "Broken" });
            chart.Scenarios[0].Steps.Add(new TopologyScenarioStep { Id = "no-such-edge", Kind = TopologyScenarioStepKind.Edge });
            var view = new TopologyDiagram(chart);

            Assert.Equal("replicate", view.ScenarioId);
            view.NextScenario(1);
            Assert.Equal("link-down", view.ScenarioId);
            view.NextScenario(1);
            Assert.Equal("replicate", view.ScenarioId);
        }

        [Fact]
        public void DetailsKey_FirstPress_FocusesTheFirstNode() {
            var view = new TopologyDiagram(TopologyFixtures.TierBridge());

            Assert.True(view.HandleInput(InputEvent.Character('d')));
            Assert.Equal("inspection", view.FocusNodeId);
            Assert.True(view.ShowDetails);
            Assert.True(view.HandleInput(InputEvent.Character('d')));
            Assert.False(view.ShowDetails);
        }

        [Fact]
        public void LiveEvents_ValuesStatusesAndFocus_UpdateNodesAndDetails() {
            var view = new TopologyDiagram(TopologyFixtures.AdReplication());
            view.Apply(new LiveEvent("topology", "value", "dc03", "lag 42 min", "critical"));
            view.Apply(new LiveEvent("topology", "value", "dc01", "98 %"));
            view.Apply(new LiveEvent("topology", "focus", "dc01"));
            view.SelectScenario(null);
            string text = Text(view, 130);

            Golden.Match("topology-ad-replication-live-130.txt", text);
            Assert.Contains("lag 42 min", text, StringComparison.Ordinal);
            Assert.Contains("■", text, StringComparison.Ordinal);
            Assert.Contains("◆ DC01  ·  Server  ·  Healthy  ·  Site: HQ", text, StringComparison.Ordinal);
            Assert.Contains("Value: 98 %", text, StringComparison.Ordinal);
            Assert.Contains("USN: 48211", text, StringComparison.Ordinal);
            Assert.Equal("dc03 = lag 42 min", view.Describe(new LiveEvent("topology", "value", "dc03", "lag 42 min")));
        }

        [Fact]
        public void Handle_PostsEventsThatTheViewApplies() {
            var view = new TopologyDiagram(TopologyFixtures.TierBridge());
            TopologyDiagramHandle handle = view.Handle(new ImmediateDispatcher(view));
            handle.Value("quarantine", "3 held", TopologyHealthStatus.Critical);
            handle.Step(4);
            handle.Focus("approval");

            Assert.Equal("3 held", view.Values["quarantine"]);
            Assert.Equal(4, view.StepIndex);
            Assert.Equal("approval", view.FocusNodeId);
        }

        [Fact]
        public void Colors_MapRolesAndEmphasisToThemeTokens() {
            var view = new TopologyDiagram(TopologyFixtures.AdReplication());
            view.Step(4);
            RenderContext context = Context(130, depth: ColorDepth.TrueColor);
            CellBuffer frame = FrameComposer.RenderBlock(view, 130, context);
            Style accent = context.Style(ThemeToken.Accent);
            Style subtle = context.Style(ThemeToken.Subtle);

            Assert.Contains(Cells(frame), cell => cell.Glyph == "━" && cell.Style == accent);
            Assert.Contains(Cells(frame), cell => cell.Glyph == "┌" && cell.Style == subtle);
        }

        [Fact]
        public void Run_ScriptedKeys_PlaysTheUseCaseInTheApp() {
            var capture = new ScreenCapture(130, 40);
            InteractiveOptions interactive = TestTerminal.Interactive("n n n Down Escape", capture);
            interactive.Target = TestTerminal.Target(130, 40);
            var view = new TopologyDiagram(TopologyFixtures.AdReplication());
            var options = new ConsoleAppOptions { Interactive = interactive, Live = new LiveSessionOptions { CaptureConsole = false, TaskbarProgress = false } };
            view.Run(options);

            Assert.Null(options.HandleInput);
            Assert.Equal(ConsoleKey.Tab, options.Keys.NextFocus!.Value.Key);
            Assert.False(view.ShowKeys);
            Assert.NotNull(capture.FindFrame("p play/pause"));
            Assert.Equal(2, view.StepIndex);
            Assert.Equal("admin", view.FocusNodeId);
            Assert.NotNull(capture.FindFrame("step 3 of 9"));
            Assert.NotNull(capture.FindFrame("◆ Admin"));
        }

        [Fact]
        public void ToPng_ActiveStep_RendersTheChartForgeXImage() {
            var view = new TopologyDiagram(TopologyFixtures.TierBridge());
            view.Step(2);
            byte[] png = view.ToPng();

            Assert.Equal(new byte[] { 0x89, 0x50, 0x4E, 0x47 }, png.Take(4).ToArray());
        }

        private static void Render(TopologyDiagram view, RenderContext context) {
            var buffer = new CellBuffer(130, 60);
            view.Render(new Region(buffer), context);
        }

        private static System.Collections.Generic.IEnumerable<Cell> Cells(CellBuffer buffer) {
            for (int y = 0; y < buffer.Height; y++) {
                for (int x = 0; x < buffer.Width; x++) yield return buffer[x, y];
            }
        }
    }

    internal static class TopologyTestExtensions {
        public static TopologyChart WithPlaybackForTest(this TopologyChart chart, int delay) {
            foreach (TopologyScenario scenario in chart.Scenarios) scenario.PlaybackDelayMilliseconds = delay;
            return chart;
        }
    }
}
