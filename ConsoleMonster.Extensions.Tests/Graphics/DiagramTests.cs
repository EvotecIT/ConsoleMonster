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
        public void MermaidFlowchart_DrawsFromAMarkdownFileAndInsideAMarkdownDocument() {
            const string markdown = "# Network\n\n```mermaid\nflowchart LR\n  A[Laptop] --> B[Router] --> C[Internet]\n```\n";
            string path = Path.Combine(Path.GetTempPath(), "cm-mermaid-" + Guid.NewGuid().ToString("N") + ".md");
            File.WriteAllText(path, markdown);
            try {
                Diagram loaded = Diagram.Load(path);
                Assert.Equal(new[] { "Laptop", "Router", "Internet" }, loaded.AllNodes().Select(node => node.Label).ToArray());
                Assert.Equal(new[] { "A>B", "B>C" }, loaded.Links.Select(link => link.From + ">" + link.To).ToArray());
            } finally { File.Delete(path); }

            MarkdownView document = ConsoleMonster.Markdown.MarkdownConsole.Parse(markdown);
            Diagram drawn = Assert.IsType<Diagram>(document.Children[1]);
            string text = Text(drawn, 100);
            Assert.Contains("Router", text);
            Assert.DoesNotContain("flowchart", text);
        }

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

        [Theory]
        [InlineData(StatusKind.Fail, GlyphTier.Full, "✗", true, "HTTPS")]
        [InlineData(StatusKind.Fail, GlyphTier.Basic, "×", false, "HTTPS")]
        [InlineData(StatusKind.Fail, GlyphTier.Ascii, "x", false, "HTTPS")]
        [InlineData(StatusKind.Warn, GlyphTier.Ascii, "!", false, "HTTPS")]
        [InlineData(StatusKind.Skip, GlyphTier.Ascii, "-", false, "HTTPS")]
        [InlineData(StatusKind.Info, GlyphTier.Ascii, "i", false, "HTTPS")]
        [InlineData(StatusKind.Pending, GlyphTier.Ascii, ".", false, "HTTPS")]
        [InlineData(StatusKind.Running, GlyphTier.Ascii, "*", false, "HTTPS")]
        [InlineData(StatusKind.Unknown, GlyphTier.Ascii, "o", false, "HTTPS")]
        [InlineData(StatusKind.Fail, GlyphTier.Ascii, "x", false, null)]
        public void LinkStatus_RemainsVisibleWithoutCompanionSections(StatusKind status, GlyphTier tier, string marker, bool imported, string? label) {
            TopologyChart? chart = imported ? TopologyChart.Create().WithTitle("Connections")
                .AddAutoNode("A", "A", TopologyNodeKind.Service)
                .AddAutoNode("B", "B", TopologyNodeKind.Service)
                .AddEdge("connection", "A", "B", label, status: TopologyHealthStatus.Critical) : null;
            Diagram diagram = chart != null ? chart.ToDiagram() : new Diagram("Connections") {
                Links = { new DiagramLink("A", "B") { Label = label, Status = status } }
            };
            diagram.ShowSummary = false;
            diagram.ShowLegend = false;
            var captionBefore = chart?.Edges[0].LabelContent;
            string text = ConsoleExporter.ToText(diagram, new ExportOptions { Width = 120, Glyphs = tier, ColorDepth = ColorDepth.None });
            Assert.Matches("[┌╭+][─-]+[┐╮+]", text);
            Assert.DoesNotContain("Needs attention", text);
            string caption = Assert.Single(text.Split('\n'), line => line.Contains(label ?? marker));
            int markerStart = label == null ? caption.IndexOf(marker, StringComparison.Ordinal)
                : caption.IndexOf(label, StringComparison.Ordinal) - marker.Length - 1;
            Assert.True(markerStart > 0);
            Assert.Equal(marker, caption.Substring(markerStart, marker.Length));
            Assert.Equal(' ', caption[markerStart - 1]);
            Assert.Equal(' ', caption[markerStart + marker.Length]);
            if (chart != null) {
                Assert.Equal(label, chart.Edges[0].Label);
                Assert.Same(captionBefore, chart.Edges[0].LabelContent);
            }
        }

        [Fact]
        public void ImportedTopology_ReportsOnlyTheStatusesItDeclares() {
            var chart = TopologyChart.Create().WithTitle("Replication")
                .AddAutoNode("admin", "Admin", TopologyNodeKind.Person)
                .AddAutoNode("dc01", "DC01", TopologyNodeKind.Server, TopologyHealthStatus.Healthy)
                .AddAutoNode("dc03", "DC03", TopologyNodeKind.Server, TopologyHealthStatus.Warning)
                .AddEdge("admin-dc01", "admin", "dc01", "LDAP")
                .AddEdge("dc01-dc03", "dc01", "dc03", "RPC");
            Diagram diagram = chart.ToDiagram();
            string text = ConsoleExporter.ToText(diagram, new ExportOptions { Width = 120, Glyphs = GlyphTier.Full, ColorDepth = ColorDepth.None });
            Assert.DoesNotContain("◌", text);
            string[] attention = text.Split('\n').SkipWhile(line => !line.Contains("Needs attention")).Skip(1).Where(line => line.Trim().Length > 0).ToArray();
            Assert.Contains(attention, line => line.Contains("DC03"));
            Assert.DoesNotContain(attention, line => line.Contains("Admin") || line.Contains("DC01"));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ClearedScenario_SurvivesGeometryRebuild(bool imported) {
            Diagram diagram;
            if (imported) {
                var chart = TopologyChart.Create().WithTitle("Before").AddAutoNode("A", "A", TopologyNodeKind.Service);
                chart.AddScenario("inspect", "Inspect", scenario => scenario.AddNodeStep("A", "Request"));
                diagram = chart.ToDiagram();
            } else {
                diagram = Ui.Diagram("Before").UseCase("Inspect", useCase => useCase.Step("A", "B", "Request"));
            }
            Assert.NotNull(diagram.ScenarioId);
            diagram.SelectScenario(null);
            Assert.DoesNotContain("Inspect", Text(diagram, 120));
            diagram.Title = "After";
            string text = Text(diagram, 120);
            Assert.Contains("After", text);
            Assert.Null(diagram.ScenarioId);
            Assert.Null(diagram.StepIndex);
            Assert.False(diagram.Playing);
            Assert.DoesNotContain("Inspect", text);
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

        [Fact]
        public void ImportedModelEdits_PreserveChartSubtitleNodeDetailsAndNodeOnlySteps() {
            var chart = TopologyChart.Create().WithTitle("Before").WithSubtitle("Deployment context")
                .WithLayout(TopologyLayoutMode.Layered, TopologyLayoutDirection.LeftToRight)
                .AddAutoNode("dns", "DNS", TopologyNodeKind.Generic)
                .AddAutoNode("worker", "Worker", TopologyNodeKind.Service)
                .AddEdge("dns", "worker", "dns", "observes", direction: VisualLinkDirection.None);
            chart.Nodes[0].Metrics["Region"] = "Warsaw";
            chart.AddScenario("inspect", "Inspect", scenario => {
                scenario.Description = "Inspect the service";
                scenario.Metadata["Owner"] = "Operations";
                scenario.AddNodeStep("dns", "Check DNS", configure: step => step.Metadata["Action"] = "Inspect");
                scenario.AddMessageStep("dns", TopologyScenarioInteraction.Response, "Reply", true, step => {
                    step.Description = "Keep the original response";
                    step.DurationMilliseconds = 750;
                    step.WithPayload("{\"answer\":\"ok\"}", TopologyScenarioPayloadFormat.Json);
                    step.Metadata["Content-Type"] = "application/json";
                });
            });
            var importedSteps = chart.Scenarios[0].Steps.ToArray();
            Diagram diagram = chart.ToDiagram();
            Assert.Equal("dns", diagram.UseCases[0].Steps[0].From);
            Assert.Equal("dns", diagram.UseCases[0].Steps[0].To);
            Assert.Contains("Deployment context", Text(diagram, 120));
            diagram.Title = "After";
            diagram.Nodes[0].Label = "DNS service";
            diagram.Nodes.Add(new DiagramNode("Gateway"));
            diagram.Links.Add(new DiagramLink("dns", "Gateway"));
            diagram.UseCases[0].Steps[0].Label = "Check DNS after update";
            diagram.UseCases[0].Steps.Add(new DiagramStep("dns", "Gateway", "Query Gateway") { Payload = { ["Payload"] = "ordinary field" } });
            diagram.FocusNode("dns");
            diagram.Step(0);
            string text = Text(diagram, 200);
            Assert.Contains("After", text);
            Assert.Contains("Deployment context", text);
            Assert.Contains("DNS service", text);
            Assert.Contains("Region: Warsaw", text);
            Assert.Contains("Inspect the service", text);
            Assert.Contains("Check DNS", text);
            Assert.Equal("inspect", diagram.ScenarioId);
            Assert.Equal("Operations", chart.Scenarios[0].Metadata["Owner"]);
            Assert.Equal(TopologyScenarioStepKind.Node, chart.Scenarios[0].Steps[0].Kind);
            Assert.Same(importedSteps[0], chart.Scenarios[0].Steps[0]);
            Assert.Equal("Check DNS after update", chart.Scenarios[0].Steps[0].Label);
            Assert.Equal("Inspect", chart.Scenarios[0].Steps[0].Metadata["Action"]);
            Assert.Same(importedSteps[1], chart.Scenarios[0].Steps[1]);
            Assert.Equal("Keep the original response", chart.Scenarios[0].Steps[1].Description);
            Assert.Equal(750, chart.Scenarios[0].Steps[1].DurationMilliseconds);
            Assert.True(chart.Scenarios[0].Steps[1].Reverse);
            Assert.Equal("{\"answer\":\"ok\"}", chart.Scenarios[0].Steps[1].Payload);
            Assert.Equal(TopologyScenarioPayloadFormat.Json, chart.Scenarios[0].Steps[1].PayloadFormat);
            Assert.Equal("application/json", chart.Scenarios[0].Steps[1].Metadata["Content-Type"]);
            Assert.Equal(3, chart.Scenarios[0].Steps.Count);
            Assert.Equal("Payload: ordinary field", chart.Scenarios[0].Steps[2].Payload);
            Assert.DoesNotContain(chart.Edges, edge => edge.SourceNodeId == "dns" && edge.TargetNodeId == "dns");
            diagram.UseCases[0].Steps[1].Payload["Payload"] = "{\"answer\":\"updated\"}";
            diagram.UseCases[0].Steps[1].Kind = DiagramStepKind.Failed;
            diagram.UseCases[0].Steps[2].Payload["Payload"] = "updated ordinary field";
            diagram.UseCases[0].Steps[0].From = diagram.UseCases[0].Steps[0].To = "Gateway";
            diagram.UseCases[0].Steps[0].Payload["Payload"] = "node field";
            Text(diagram, 200);
            Assert.Equal(TopologyScenarioStepKind.Node, chart.Scenarios[0].Steps[0].Kind);
            Assert.Equal("Gateway", chart.Scenarios[0].Steps[0].Id);
            Assert.Equal("Payload: node field", chart.Scenarios[0].Steps[0].Payload);
            Assert.Equal(TopologyScenarioPayloadFormat.Text, chart.Scenarios[0].Steps[0].PayloadFormat);
            Assert.Same(importedSteps[1], chart.Scenarios[0].Steps[1]);
            Assert.Equal("{\"answer\":\"updated\"}", chart.Scenarios[0].Steps[1].Payload);
            Assert.Equal(TopologyScenarioPayloadFormat.Json, chart.Scenarios[0].Steps[1].PayloadFormat);
            Assert.Equal(TopologyScenarioInteraction.Failure, chart.Scenarios[0].Steps[1].Interaction);
            Assert.Equal("application/json", chart.Scenarios[0].Steps[1].Metadata["Content-Type"]);
            Assert.Equal(750, chart.Scenarios[0].Steps[1].DurationMilliseconds);
            Assert.Equal("Payload: updated ordinary field", chart.Scenarios[0].Steps[2].Payload);
            Assert.Equal(TopologyNodeKind.Generic, chart.Nodes.Single(node => node.Id == "dns").Kind);
            Assert.Equal(VisualLinkDirection.None, chart.Edges.Single(edge => edge.Id == "dns").Direction);
        }

        [Fact]
        public void AsciiTopology_TransliteratesCallerTextBeforeCellProjection() {
            var chart = TopologyChart.Create().AddAutoNode("city", "Łódź Straße", TopologyNodeKind.Service);
            var caption = chart.Nodes[0].LabelContent;
            Diagram diagram = chart.ToDiagram();
            string text = ConsoleExporter.ToText(diagram, new ExportOptions { Width = 120, Glyphs = GlyphTier.Ascii });
            Assert.Contains("Lodz Strasse", text);
            Assert.Contains("+", text);
            Assert.Contains("Łódź Straße", Text(diagram, 120));
            Assert.Same(caption, chart.Nodes[0].LabelContent);
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
            Assert.DoesNotContain("play/pause", writer.ToString());
            Assert.DoesNotContain("focus", writer.ToString());
        }
    }
}
