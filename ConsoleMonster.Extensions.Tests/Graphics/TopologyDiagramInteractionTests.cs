using System;
using System.IO;
using System.Linq;
using ChartForgeX.Topology;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Graphics;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;
using ConsoleMonster.Live;
using ConsoleMonster.Output;
using ConsoleMonster.Testing;
using Xunit;

namespace ConsoleMonster.Extensions.Tests.Graphics {
    public sealed class TopologyDiagramInteractionTests {
        [Theory]
        [InlineData(null)]
        [InlineData(InteractionMode.Line)]
        [InlineData(InteractionMode.Snapshot)]
        public void Run_NonInteractiveWriter_PrintsOneSnapshotAndPreservesOptions(InteractionMode? mode) {
            var view = new TopologyDiagram(SingleNodeChart().WithTitle("Redirected topology")
                .AddScenario("flow", "Flow", scenario => scenario.WithPlayback(autoPlay: true).AddNodeStep("worker", "Inspect")));
            using var writer = new StringWriter();
            var interactive = new InteractiveOptions { Target = new WriterTarget(writer, 80), Mode = mode };
            var options = new ConsoleAppOptions { Interactive = interactive };

            AppState state = view.Run(options);

            string output = writer.ToString();
            Assert.Contains("Worker", output, StringComparison.Ordinal);
            Assert.Equal(1, output.Split(new[] { "Redirected topology" }, StringSplitOptions.None).Length - 1);
            Assert.DoesNotContain('\u001b', output);
            Assert.Same(interactive, options.Interactive);
            Assert.Equal(mode, interactive.Mode);
            Assert.Null(options.HandleInput);
            Assert.False(view.ShowKeys);
            Assert.False(view.Playing);
            Assert.Null(view.StepIndex);
            Assert.False(state.ExitRequested);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Run_KeyboardView_HonorsSelectedScenarioAutoplay(bool autoPlay) {
            var view = new TopologyDiagram(SingleNodeChart().AddScenario("flow", "Flow", scenario => scenario
                .WithPlayback(stepDurationMilliseconds: 60000, loop: true, autoPlay: autoPlay).AddNodeStep("worker", "Inspect")));
            var capture = new ScreenCapture(100, 40);
            ConsoleAppOptions options = AppOptions("Escape", capture);
            bool? observedPlaying = null;
            options.ProcessEvents = state => { observedPlaying = view.Playing; state.RequestExit(); };

            view.Run(options);

            Assert.Equal(autoPlay, observedPlaying);
            Assert.Equal(autoPlay ? (int?)0 : null, view.StepIndex);
            Assert.Contains("Worker", capture.Frames[0].ToPlainText(), StringComparison.Ordinal);
        }

        [Fact]
        public void EmptyTopology_FocusAndStepKeys_AreUnhandledWithoutChangingDetails() {
            var view = new TopologyDiagram(TopologyChart.Create()) { ShowDetails = false };
            InputEvent[] inputs = {
                InputEvent.Press(ConsoleKey.UpArrow), InputEvent.Press(ConsoleKey.DownArrow),
                InputEvent.Character('d'), InputEvent.Press(ConsoleKey.Tab),
                InputEvent.Press(ConsoleKey.Home), InputEvent.Press(ConsoleKey.End),
                InputEvent.Press(ConsoleKey.LeftArrow), InputEvent.Press(ConsoleKey.RightArrow),
                InputEvent.Press(ConsoleKey.Spacebar), InputEvent.Character('p'),
                InputEvent.Character('n'), InputEvent.Character('b'), InputEvent.Character('r'),
                InputEvent.Character('e'), InputEvent.Character('x'), InputEvent.Character('s')
            };

            foreach (InputEvent input in inputs) Assert.False(view.HandleInput(input));

            Assert.Null(view.FocusNodeId);
            Assert.Null(view.StepIndex);
            Assert.False(view.Playing);
            Assert.False(view.ShowDetails);
        }

        [Fact]
        public void SingleNode_ArrowFocusKeys_ReportWhetherFocusChanged() {
            var view = new TopologyDiagram(SingleNodeChart());

            Assert.True(view.HandleInput(InputEvent.Press(ConsoleKey.DownArrow)));
            Assert.Equal("worker", view.FocusNodeId);
            Assert.False(view.HandleInput(InputEvent.Press(ConsoleKey.DownArrow)));
            Assert.False(view.HandleInput(InputEvent.Press(ConsoleKey.UpArrow)));
            Assert.Equal("worker", view.FocusNodeId);
        }

        [Fact]
        public void Run_TallDiagram_ArrowsScrollToTheBottomAndBack() {
            string payload = string.Join("\n", Enumerable.Range(1, 24).Select(index => "Payload row " + index)) + "\nScroll destination";
            TopologyChart chart = SingleNodeChart().WithTitle("Scroll origin")
                .AddScenario("flow", "Long payload", scenario => scenario
                    .AddNodeStep("worker", "Inspect", configure: step => step.WithPayload(payload)));
            var view = new TopologyDiagram(chart) { MaxPayloadLines = 30 };
            view.FirstStep();
            int rows = TestTerminal.Render(view, 100).Height + 10;
            var capture = new ScreenCapture(100, 10);
            ConsoleAppOptions options = AppOptions("Up " + string.Concat(Enumerable.Repeat("Down ", rows)), capture);
            var input = (ScriptedInput)options.Interactive!.Input!;
            input.WaitForOutput(capture, "↑/↓ scroll", TimeSpan.FromSeconds(5));
            input.Append(string.Concat(Enumerable.Repeat("Up ", rows)));
            input.WaitForOutput(capture, "Scroll origin", TimeSpan.FromSeconds(5));
            input.Append("Escape");

            view.Run(options);

            string first = capture.Frames[0].ToPlainText();
            Assert.Contains("Scroll origin", first, StringComparison.Ordinal);
            Assert.DoesNotContain("Scroll destination", first, StringComparison.Ordinal);
            Assert.True(capture.FindFrame("Scroll destination") != null,
                "The lower payload must become visible while scrolling. Frames:\n" + string.Join("\n---\n", capture.Frames.Select(frame => frame.ToPlainText()).Distinct()));
            Assert.NotNull(capture.FindFrame("↑/↓ scroll"));
            Assert.Contains("Scroll origin", capture.LastFrame!.ToPlainText(), StringComparison.Ordinal);
            Assert.Null(view.FocusNodeId);
            Assert.False(view.ShowKeys);
        }

        [Fact]
        public void AsciiTier_UserText_IsSanitizedAcrossDiagramAndPanels() {
            TopologyChart chart = TopologyChart.Create()
                .WithTitle("Title ✅").WithSubtitle("Subtitle 漢")
                .WithLayout(TopologyLayoutMode.Layered, TopologyLayoutDirection.LeftToRight)
                .WithViewport(700, 300, 28)
                .AddGroup("zone", "Zone ✅", 0, 0, 0, 0)
                .AddAutoNode("worker", "Worker 漢", TopologyNodeKind.Server, groupId: "zone", subtitle: "Node subtitle ✅")
                .AddAutoNode("store", "Store ✅", TopologyNodeKind.Database)
                .AddEdge("save", "worker", "store", "Save ✅")
                .AddScenario("flow", "Scenario ✅", scenario => scenario
                    .WithDescription("Description 漢")
                    .AddMessageStep("save", TopologyScenarioInteraction.Request, "Request ✅", configure: step => step.WithPayload("Payload ✅ 漢")));
            chart.Nodes[0].Metrics["Metric ✅"] = "Metric value 漢";
            var view = new TopologyDiagram(chart);
            view.FirstStep();

            string initial = TestTerminal.RenderText(view, 180, glyphs: GlyphTier.Ascii);
            Assert.All(initial, ch => Assert.True(ch < 128, "Non-ASCII U+" + ((int)ch).ToString("X4")));
            foreach (string visible in new[] { "Title [OK]", "Subtitle ?", "Worker", "Store", "Zone", "Save", "Node subtitle", "Scenario [OK]", "Description ?", "Request [OK]", "Payload [OK] ?" })
                Assert.Contains(visible, initial, StringComparison.Ordinal);

            view.FocusNode("worker");
            view.SetValue("worker", "Live ✅ 漢");
            string live = TestTerminal.RenderText(view, 180, glyphs: GlyphTier.Ascii);
            Assert.All(live, ch => Assert.True(ch < 128, "Non-ASCII U+" + ((int)ch).ToString("X4")));
            Assert.Contains("Value: Live [OK] ?", live, StringComparison.Ordinal);
            Assert.Contains("Metric [OK]: Metric value ?", live, StringComparison.Ordinal);
            Assert.Contains("Node subtitle [OK]", live, StringComparison.Ordinal);
        }

        [Fact]
        public void BasicTier_HighlightAndFocus_KeepDistinctConnectedBorders() {
            var view = new TopologyDiagram(TopologyFixtures.AdReplication()) { ShowDetails = false };
            view.Step(4);
            view.FocusNode("admin");

            string[] lines = TestTerminal.RenderText(view, 130, glyphs: GlyphTier.Basic).Split('\n');

            AssertBox(lines, "╒", "╕", "╘", "╛", "│");
            AssertBox(lines, "╔", "╗", "╚", "╝", "║");
            Assert.DoesNotContain(lines, line => line.Contains("┏") || line.Contains("┃"));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void KeyHints_ReflectAvailableActionsAndKeysPerformThem(bool multipleScenarios) {
            TopologyChart chart = SingleNodeChart().AddScenario("first", "First", scenario => scenario
                .AddNodeStep("worker", "Start").AddNodeStep("worker", "Finish"));
            if (multipleScenarios) chart.AddScenario("second", "Second", scenario => scenario.AddNodeStep("worker", "Other"));
            var view = new TopologyDiagram(chart) { ShowKeys = true };

            string hints = TestTerminal.RenderText(view, 110, glyphs: GlyphTier.Ascii);
            Assert.Contains("r/Home restart", hints, StringComparison.Ordinal);
            Assert.Contains("e/End last", hints, StringComparison.Ordinal);
            Assert.Contains("x clear step", hints, StringComparison.Ordinal);
            Assert.Contains("Up/Down focus  d details", hints, StringComparison.Ordinal);
            if (multipleScenarios) {
                Assert.Contains("s/Tab next use case  Shift+Tab previous use case", hints, StringComparison.Ordinal);
                Assert.True(view.HandleInput(InputEvent.Press(ConsoleKey.Tab)));
                Assert.Equal("second", view.ScenarioId);
                Assert.True(view.HandleInput(InputEvent.Press(ConsoleKey.Tab, KeyModifiers.Shift)));
                Assert.Equal("first", view.ScenarioId);
                Assert.True(view.HandleInput(InputEvent.Character('s')));
                Assert.Equal("second", view.ScenarioId);
                view.SelectScenario("first");
            } else {
                Assert.DoesNotContain("use case", hints, StringComparison.Ordinal);
                Assert.False(view.HandleInput(InputEvent.Press(ConsoleKey.Tab)));
                Assert.False(view.HandleInput(InputEvent.Character('s')));
            }
            Assert.True(view.HandleInput(InputEvent.Press(ConsoleKey.End)));
            Assert.Equal(1, view.StepIndex);
            Assert.True(view.HandleInput(InputEvent.Press(ConsoleKey.Home)));
            Assert.Equal(0, view.StepIndex);
            Assert.True(view.HandleInput(InputEvent.Character('x')));
            Assert.Null(view.StepIndex);
            Assert.True(view.HandleInput(InputEvent.Character('d')));
            Assert.Equal("worker", view.FocusNodeId);
            Assert.True(view.ShowDetails);
            Assert.True(view.HandleInput(InputEvent.Character('d')));
            Assert.False(view.ShowDetails);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Run_CustomExitAndPriorHandler_AreHonoredAndOptionsRestored(bool showKeys) {
            var view = new TopologyDiagram(SingleNodeChart().AddScenario("flow", "Flow", scenario => scenario.AddNodeStep("worker", "Visit"))) { ShowKeys = showKeys };
            var capture = new ScreenCapture(110, 50);
            ConsoleAppOptions options = AppOptions("d Escape n F10", capture);
            int detailsKeys = 0;
            Func<AppState, InputEvent, bool> prior = (state, input) => {
                if (!input.IsChar('d')) return false;
                detailsKeys++;
                return true;
            };
            options.HandleInput = prior;
            options.Keys.Exit = new KeyChord(ConsoleKey.F10);
            KeyChord? next = options.Keys.NextFocus;
            KeyChord? previous = options.Keys.PreviousFocus;

            AppState state = view.Run(options);

            Assert.True(state.ExitRequested);
            Assert.Equal(1, detailsKeys);
            Assert.Null(view.FocusNodeId);
            Assert.Equal(0, view.StepIndex);
            Assert.Same(prior, options.HandleInput);
            Assert.Equal(next, options.Keys.NextFocus);
            Assert.Equal(previous, options.Keys.PreviousFocus);
            Assert.Equal(new KeyChord(ConsoleKey.F10), options.Keys.Exit);
            Assert.Equal(showKeys, view.ShowKeys);
        }

        [Fact]
        public void KeyHints_NoScenarioOrConfiguredExit_OnlyShowAvailableNavigation() {
            var view = new TopologyDiagram(SingleNodeChart()) { ShowKeys = true };
            var capture = new ScreenCapture(100, 40);
            ConsoleAppOptions options = AppOptions("Ctrl+C", capture);
            options.Keys.Exit = null;

            AppState state = view.Run(options);

            string text = capture.LastFrame!.ToPlainText();
            Assert.True(state.ExitRequested);
            Assert.Contains("↑/↓ focus  d details", text, StringComparison.Ordinal);
            Assert.DoesNotContain("play/pause", text, StringComparison.Ordinal);
            Assert.DoesNotContain("use case", text, StringComparison.Ordinal);
            Assert.DoesNotContain("quit", text, StringComparison.Ordinal);
            Assert.Null(options.Keys.Exit);
            Assert.True(view.ShowKeys);
        }

        private static TopologyChart SingleNodeChart() => TopologyChart.Create()
            .WithLayout(TopologyLayoutMode.Layered, TopologyLayoutDirection.LeftToRight)
            .WithViewport(300, 200, 28)
            .AddAutoNode("worker", "Worker", TopologyNodeKind.Server);

        private static ConsoleAppOptions AppOptions(string script, ScreenCapture capture) {
            InteractiveOptions interactive = TestTerminal.Interactive(script, capture);
            interactive.Target = TestTerminal.Target(capture.Width, capture.Height);
            return new ConsoleAppOptions {
                Interactive = interactive,
                Live = new LiveSessionOptions { CaptureConsole = false, TaskbarProgress = false }
            };
        }

        private static void AssertBox(string[] lines, string topLeft, string topRight, string bottomLeft, string bottomRight, string side) {
            for (int row = 0; row + 2 < lines.Length; row++) {
                int left = lines[row].IndexOf(topLeft, StringComparison.Ordinal);
                if (left < 0) continue;
                int right = lines[row].IndexOf(topRight, left + 1, StringComparison.Ordinal);
                if (right <= left + 1) continue;
                Assert.All(lines[row].Substring(left + 1, right - left - 1), ch => Assert.Equal('═', ch));
                for (int bottom = row + 2; bottom < lines.Length; bottom++) {
                    if (lines[bottom].Length <= right || lines[bottom][left].ToString() != bottomLeft || lines[bottom][right].ToString() != bottomRight) continue;
                    Assert.Equal(side, lines[row + 1][left].ToString());
                    Assert.Equal(side, lines[row + 1][right].ToString());
                    Assert.All(lines[bottom].Substring(left + 1, right - left - 1), ch => Assert.Equal('═', ch));
                    return;
                }
            }
            Assert.Fail("Expected a connected " + topLeft + topRight + bottomLeft + bottomRight + " box.");
        }
    }
}
