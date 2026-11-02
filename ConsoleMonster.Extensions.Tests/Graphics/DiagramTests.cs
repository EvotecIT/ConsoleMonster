using System;
using System.IO;
using System.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Topology;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Export;
using ConsoleMonster.Graphics;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;
using ConsoleMonster.Output;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;
using ConsoleMonster.Testing;
using ConsoleMonster.Live;
using Xunit;

namespace ConsoleMonster.Extensions.Tests.Graphics {
    public sealed class DiagramTests {
        [Theory]
        [InlineData("null")]
        [InlineData("empty")]
        [InlineData("whitespace")]
        public void BlankNodeKeys_AreRejectedBeforeTheGraphicsSessionQueuesAnUpdate(string keyCase) {
            string? key = keyCase == "null" ? null : keyCase == "empty" ? "" : " \t";
            var diagram = Ui.Diagram("Monitor").Node("VPN", node => node.Subtitle("vpn.contoso.com"));
            using var writer = new StringWriter();
            using var session = new LiveSession(diagram, new LiveSessionOptions {
                Target = new WriterTarget(writer, 100), CaptureConsole = false, TaskbarProgress = false
            });
            session.Start();
            DiagramHandle handle = diagram.Handle(session);
            Assert.Throws<ArgumentException>(() => handle.Value(key!, "invalid", StatusKind.Fail));
            handle.Value("VPN", "42 ms", StatusKind.Warn);
            session.Stop();
            Assert.Equal(0, session.ApplyErrors);
            Assert.Equal(0, session.RenderErrors);
            Assert.Equal("42 ms", diagram.Values["VPN"]);
            Assert.Equal(StatusKind.Warn, diagram.Nodes[0].Status);
            Assert.Contains("42 ms", Text(diagram, 100));
            Assert.DoesNotContain("invalid", Text(diagram, 100));
        }

        public DiagramTests() { DiagramRendererProvider.Register(); }
        private static string Text(Diagram diagram, int width) => ConsoleExporter.ToText(diagram, new ExportOptions { Width = width, Glyphs = GlyphTier.Full });

        [Fact]
        public void RegisteredRenderer_PreservesSubtitleAndIndependentValueInTheBox() {
            var diagram = new Diagram("Monitor") { Nodes = { new DiagramNode("DNS") { Subtitle = "1.1.1.1", Value = "12 ms" } }, ShowLegend = false };
            string text = Text(diagram, 120);
            Assert.Contains("┌", text);
            Assert.Contains("DNS", text);
            Assert.Contains("1.1.1.1", text);
            Assert.Contains("12 ms", text);
            int subtitle = Array.FindIndex(text.Split('\n'), line => line.Contains("1.1.1.1"));
            int value = Array.FindIndex(text.Split('\n'), line => line.Contains("12 ms"));
            Assert.Equal(subtitle + 1, value);
            diagram.SetValue("DNS", "15 ms", StatusKind.Warn);
            Assert.Contains("1.1.1.1", Text(diagram, 120));
            Assert.Contains("15 ms", Text(diagram, 120));
        }

        [Theory]
        [InlineData(12)]
        [InlineData(34)]
        public void NarrowRenderer_KeepsLongFailureLabelsAndLiveValuesAsAWrappingTree(int width) {
            var diagram = new Diagram("Office") {
                Groups = { new DiagramGroup("HQ") { Nodes = { new DiagramNode("File server") { Subtitle = "Disk 91%", Value = "840 GB", Status = StatusKind.Warn } } } },
                Links = { new DiagramLink("File server", "Offsite backup") { Label = "RPC server unavailable", Status = StatusKind.Fail } }
            };
            string text = Text(diagram, width);
            Assert.DoesNotContain("┌", text);
            string flattened = string.Concat(text.Split('\n').Select(line => line.Trim().TrimStart('│', '├', '└', '─', '|', '-', '`', '+', ' ')));
            string words = string.Concat(flattened.Where(ch => !char.IsWhiteSpace(ch)));
            Assert.Contains("RPCserverunavailable", words);
            Assert.Contains("840GB", words);
            Assert.Contains("Disk91%", words);
        }

        [Fact]
        public void ModelChanges_InvalidateGeometryWithoutReplacingThePublicWidget() {
            var diagram = new Diagram("First") { Nodes = { new DiagramNode("A") } };
            Assert.Contains("A", Text(diagram, 120));
            diagram.Nodes[0].Label = "Renamed component";
            diagram.Nodes.Add(new DiagramNode("B"));
            diagram.Links.Add(new DiagramLink("Renamed component", "B") { Label = "HTTPS" });
            string text = Text(diagram, 120);
            Assert.Contains("Renamed component", text);
            Assert.Contains("HTTPS", text);
            Assert.Contains("B", text);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void DetailsKey_PersistsThroughThePublicAdapterAndNextFrame(bool throughApp) {
            var diagram = Ui.Diagram("Worker").Node("Worker", node => node.Value("42"));
            diagram.FocusNode("Worker");
            if (throughApp) {
                var capture = new ScreenCapture(110, 40);
                var interactive = TestTerminal.Interactive("d Escape", capture);
                interactive.Target = TestTerminal.Target(capture.Width, capture.Height);
                diagram.Run(new ConsoleAppOptions { Interactive = interactive, Live = new LiveSessionOptions { CaptureConsole = false, TaskbarProgress = false } });
                Assert.DoesNotContain("Value: 42", capture.LastFrame!.ToPlainText());
            } else {
                Assert.True(diagram.HandleInput(InputEvent.Character('d')));
                Assert.DoesNotContain("Value: 42", Text(diagram, 110));
            }
            Assert.False(diagram.ShowDetails);
            Text(diagram, 110);
            Assert.False(diagram.ShowDetails);
        }

        [Fact]
        public void ImportedBackwardLink_PreservesDirectionInTheCoreModelAndTree() {
            Assert.Throws<ArgumentNullException>(() => ((TopologyChart)null!).ToDiagram());
            var chart = TopologyChart.Create().WithLayout(TopologyLayoutMode.Layered, TopologyLayoutDirection.LeftToRight)
                .AddAutoNode("A", "A").AddAutoNode("B", "B")
                .AddEdge("back", "A", "B", "reply", direction: VisualLinkDirection.Backward);
            Diagram diagram = chart.ToDiagram();
            DiagramLink link = Assert.Single(diagram.Links);
            Assert.Equal("B", link.From);
            Assert.Equal("A", link.To);
            Assert.Contains("reply", Text(diagram, 34));
            diagram.Title = "Changed";
            Assert.Contains("reply", Text(diagram, 120));
            Assert.Equal("B", link.From);
        }

        [Theory]
        [InlineData(StatusKind.Pass)]
        [InlineData(StatusKind.Fail)]
        [InlineData(StatusKind.Warn)]
        [InlineData(StatusKind.Skip)]
        [InlineData(StatusKind.Info)]
        [InlineData(StatusKind.Pending)]
        [InlineData(StatusKind.Running)]
        [InlineData(StatusKind.Unknown)]
        [InlineData(StatusKind.None)]
        public void CoreNodeMetadata_IsRetainedInTheMarkLegendAndFocusedDetails(StatusKind status) {
            var diagram = new Diagram { Nodes = { new DiagramNode("Worker") { Kind = DiagramNodeKind.Workstation, Status = status } }, ShowSummary = false };
            string text = Text(diagram, 120);
            string nodeLine = text.Split('\n').First(line => line.Contains("Worker") && line.Contains("│"));
            Assert.Contains(GlyphSet.Full.StatusSymbol(status), nodeLine);
            if (status == StatusKind.Pass) Assert.DoesNotContain(status.ToString(), text);
            else Assert.Contains(status.ToString(), text);
            diagram.ShowLegend = false;
            diagram.FocusNode("Worker");
            Assert.Contains("Worker  ·  Workstation  ·  " + status, Text(diagram, 120));
        }

        [Fact]
        public void FocusedDetails_DistinguishesOmittedStatusFromExplicitNone() {
            var node = new DiagramNode("Worker") { Kind = DiagramNodeKind.Workstation };
            var diagram = new Diagram { Nodes = { node }, ShowLegend = false, ShowSummary = false };
            diagram.FocusNode("Worker");
            string omitted = Text(diagram, 120);
            Assert.Contains("Worker  ·  Workstation", omitted);
            Assert.DoesNotContain("  ·  None", omitted);
            node.Status = StatusKind.None;
            Assert.Contains("Worker  ·  Workstation  ·  None", Text(diagram, 120));
        }

        [Fact]
        public void PlaybackInNarrowTree_IgnoresAResizeResetAndContinuesAtTheNextForwardFrame() {
            var diagram = Ui.Diagram("Checkout").UseCase("Order", u => u.Delay(1000).Step("Shopper", "Web", "POST").Reply("Web", "Shopper", "Thanks"));
            diagram.Play();
            void Frame(int milliseconds) {
                var profile = TerminalProfile.Fixed(12, 60, ColorDepth.None, GlyphTier.Full);
                diagram.Measure(new ConsoleMonster.Layout.Constraints(12), new RenderContext(profile, Theme.Dark, elapsed: TimeSpan.FromMilliseconds(milliseconds)));
            }
            Frame(2000); Frame(2500); Frame(0); Frame(2900);
            Assert.Equal(0, diagram.StepIndex);
            Frame(3000);
            Assert.Equal(1, diagram.StepIndex);
        }

        [Fact]
        public void Run_NarrowTree_ShowsPlaybackKeysAndScrollsTheActivePayload() {
            string payload = string.Join("\n", Enumerable.Range(1, 24).Select(index => "payload-" + index)) + "\npayload-last";
            var diagram = Ui.Diagram("Narrow playback")
                .UseCase("Inspect", useCase => useCase.Step("Source", "Destination", "Request", step => step.Payload(new System.Collections.Generic.Dictionary<string, object?> { { "Body", payload } })));
            diagram.MaxPayloadLines = 30;
            diagram.Step(0);
            var capture = new ScreenCapture(34, 14);
            var interactive = TestTerminal.Interactive(string.Join(" ", Enumerable.Repeat("Down", 45)) + " Escape", capture);
            interactive.Target = TestTerminal.Target(capture.Width, capture.Height);
            diagram.Run(new ConsoleAppOptions { Interactive = interactive, Live = new LiveSessionOptions { CaptureConsole = false, TaskbarProgress = false } });
            Assert.NotNull(capture.FindFrame("payload-last"));
            Assert.NotNull(capture.FindFrame("play/pause"));
            Assert.NotNull(capture.FindFrame("↑/↓ scroll"));
            Assert.Equal(0, diagram.StepIndex);
        }

        [Theory]
        [InlineData(null)]
        [InlineData(InteractionMode.Line)]
        public void Run_RedirectedSnapshotRetainsTheAttentionSummaryAndDoesNotAutoplay(InteractionMode? mode) {
            var diagram = Ui.Diagram("Monitor").Node("VPN", n => n.Status(StatusKind.Fail).Subtitle("vpn.contoso.com")).UseCase("Connect", u => u.AutoPlay().Step("VPN", "VPN", "connect"));
            using var writer = new StringWriter();
            var original = new InteractiveOptions { Mode = mode, Target = new WriterTarget(writer, 60) };
            var options = new ConsoleAppOptions { Interactive = original };
            diagram.Run(options);
            Assert.Contains("Needs attention", writer.ToString());
            Assert.Contains("vpn.contoso.com", writer.ToString());
            Assert.Same(original, options.Interactive);
            Assert.False(diagram.Playing);
        }
    }
}
