using System;
using System.IO;
using System.Linq;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Export;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Rendering;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Widgets {
    public sealed class FlowTests {
        private static Flow Chain(bool cut = false) => new Flow("chain")
            .Add(new FlowNode("client", "client", StatusKind.Pass))
            .Add(new FlowNode("host", "host:443", StatusKind.Pass))
            .Add(new FlowNode("leaf", "leaf", StatusKind.Pass))
            .Add(new FlowNode("issuer", "intermediate", cut ? StatusKind.Fail : StatusKind.Pass))
            .Add(new FlowNode("root", "root", cut ? StatusKind.Skip : StatusKind.Pass))
            .Add(new FlowEdge("tls", "host", "leaf", "TLS"))
            .Add(new FlowEdge("issuer", "leaf", "issuer", "issuer", cut ? StatusKind.Fail : StatusKind.Pass, cut));
        private static CellBuffer Frame(IWidget widget, int width, GlyphTier tier = GlyphTier.Full) => FrameComposer.RenderBlock(widget, width,
            new RenderContext(TerminalProfile.Fixed(width, 120, ColorDepth.None, tier), Theme.Dark));

        [Theory]
        [InlineData(120)] [InlineData(80)] [InlineData(48)]
        public void ChainMatchesReviewedGoldenAtSupportedWidths(int width) {
            Flow flow = Chain(true).WithBranch(new FlowBranch("issuer", "expired 2026-09-30", Status: StatusKind.Fail));
            string actual = Frame(flow, width).ToPlainText().Replace("\r\n", "\n").TrimEnd();
            string path = Path.Combine(AppContext.BaseDirectory, "Golden", "flow-" + width + ".txt");
            Assert.Equal(File.ReadAllText(path).Replace("\r\n", "\n").TrimEnd(), actual);
        }
        [Theory]
        [InlineData(GlyphTier.Full)] [InlineData(GlyphTier.Basic)] [InlineData(GlyphTier.Ascii)]
        public void EveryStateRemainsDistinctWithoutColour(GlyphTier tier) {
            string[] rendered = new[] { StatusKind.Pass, StatusKind.Warn, StatusKind.Fail, StatusKind.Pending, StatusKind.Skip }
                .Select(status => Frame(new Flow().Add(new FlowNode("n", "hop", status)), 48, tier).ToPlainText()).ToArray();
            Assert.Equal(5, rendered.Distinct().Count());
            string cut = Frame(Chain(true), 120, tier).ToPlainText();
            Assert.Contains(tier == GlyphTier.Ascii ? " x " : " ✗ ", cut);
            if (tier == GlyphTier.Ascii) Assert.All(cut, c => Assert.InRange((int)c, 0, 127));
        }
        [Theory]
        [InlineData(120)] [InlineData(48)] [InlineData(18)]
        public void UnicodeLabelsKeepCellGeometryAndWholeWords(int width) {
            var flow = new Flow().Add(new FlowNode("a", "東京 🔒"))
                .Add(new FlowNode("b", "certificate authority"));
            string text = Frame(flow, width).ToPlainText();
            Assert.Contains("東京", text);
            Assert.Contains("🔒", text);
            Assert.Contains("certificate", text);
            Assert.Contains("authority", text);
            foreach (string line in text.Split('\n')) Assert.True(DisplayWidth.Measure(line.TrimEnd('\r')) <= width);
        }
        [Theory]
        [InlineData(120)] [InlineData(48)]
        public void BranchReturnsToLaterNodeAndCompositionKeepsEveryLabel(int width) {
            Flow flow = Chain().WithBranch(new FlowBranch("host", "fallback", "root", StatusKind.Warn));
            string text = Frame(new Panel(flow, "Path"), width).ToPlainText();
            foreach (string label in new[] { "client", "host:443", "leaf", "intermediate", "root", "fallback" }) Assert.Contains(label, text);
            Assert.Contains("Path", text);
            Assert.Contains("fallback", ConsoleExporter.ToHtml(flow, new ExportOptions { Width = width }));
            Assert.Contains("fallback", ConsoleExporter.ToSvg(flow, new ExportOptions { Width = width }));
        }
        [Theory]
        [InlineData(GlyphTier.Full, false)] [InlineData(GlyphTier.Full, true)]
        [InlineData(GlyphTier.Ascii, false)] [InlineData(GlyphTier.Ascii, true)]
        public void VerticalEdgesJoinBothBordersAndKeepTheirLabels(GlyphTier tier, bool cut) {
            var flow = new Flow().Add(new FlowNode("a", "client"))
                .Add(new FlowNode("b", "host:443"))
                .Add(new FlowEdge("tls", "a", "b", "TLS", Cut: cut))
                .WithBranch(new FlowBranch("a", "retry"));
            string[] lines = Frame(flow, 20, tier).ToPlainText().Split('\n');
            int source = Array.FindIndex(lines, line => line.Contains("client"));
            int destination = Array.FindIndex(lines, line => line.Contains("host:443"));
            Assert.Equal(tier == GlyphTier.Ascii ? '|' : '┬', lines[source + 1][2]);
            Assert.Equal(tier == GlyphTier.Ascii ? 'v' : '▼', lines[destination - 1][2]);
            for (int i = source + 2; i < destination - 1; i++) {
                Assert.Contains(lines[i][2], tier == GlyphTier.Ascii ? "|+x" : "│├✗");
            }
            Assert.Contains("TLS", string.Join("\n", lines));
            Assert.Contains("retry", string.Join("\n", lines));
            Assert.DoesNotContain("↓", string.Join("\n", lines));
        }

        [Theory]
        [InlineData(GlyphTier.Full)] [InlineData(GlyphTier.Basic)] [InlineData(GlyphTier.Ascii)]
        public void VerdictTitleIdentifiesStatusWithoutColour(GlyphTier tier) {
            foreach (StatusKind status in new[] { StatusKind.Pass, StatusKind.Fail }) {
                string title = status == StatusKind.Pass ? "TRUSTED" : "UNTRUSTED";
                string marker = tier == GlyphTier.Ascii ? (status == StatusKind.Pass ? "+" : "x") : tier == GlyphTier.Basic ? (status == StatusKind.Pass ? "√" : "×") : (status == StatusKind.Pass ? "✓" : "✗");
                var panel = new Panel(new TextBlock("Chain"), title) { Verdict = true, Status = status, Expand = false };
                string text = Frame(panel, 48, tier).ToPlainText();
                Assert.Contains(marker + " " + title, text);
                if (tier == GlyphTier.Ascii) Assert.All(text, c => Assert.InRange((int)c, 0, 127));
            }
        }

        [Fact]
        public void LiveHandleChangesNodesAndCutsThenReconnectsEdges() {
            Flow flow = Chain();
            var dispatcher = new ImmediateDispatcher(flow);
            FlowHandle handle = flow.Handle(dispatcher);
            handle.Node("issuer", StatusKind.Fail);
            handle.Edge("issuer", StatusKind.Fail, true);
            Assert.Equal(StatusKind.Fail, flow.Nodes[3].Status);
            Assert.True(flow.Edges.Single(e => e.Key == "issuer").Cut);
            handle.Edge("issuer", StatusKind.Warn);
            Assert.True(flow.Edges.Single(e => e.Key == "issuer").Cut);
            handle.Edge("issuer", StatusKind.Pass, false);
            Assert.False(flow.Edges.Single(e => e.Key == "issuer").Cut);
        }
        [Theory]
        [InlineData(120, false)] [InlineData(48, false)] [InlineData(48, true)]
        public void TableCellsDrawAndReplayLiveFlowInsteadOfStringifyingIt(int width, bool cards) {
            Flow flow = Chain();
            var table = new Table { RecordViewRows = 0, CardsBelow = 0, AlwaysCards = cards, Expand = true };
            table.AddColumn("Path");
            table.AddRow(TableCell.FromValue(flow));
            string text = Frame(table, width).ToPlainText();
            foreach (string label in new[] { "client", "host:443", "leaf", "intermediate", "root" }) Assert.Contains(label, text);
            Assert.DoesNotContain("ConsoleMonster.Widgets.Flow", text);
            Assert.Contains(flow, table.Children);
            string data = Recording.ExportPlayerData(new Panel(table), new[] {
                new LiveEvent("chain", "node", "issuer", state: "Fail", time: 100),
                new LiveEvent("chain", "cut", "issuer", state: "Fail", time: 200)
            }, new RecordingExportOptions { Width = width, Height = 80 });
            Assert.Equal(StatusKind.Fail, flow.Nodes[3].Status);
            Assert.True(flow.Edges.Single(e => e.Key == "issuer").Cut);
            Assert.Contains("consolemonster-player", data);
        }

        [Theory]
        [InlineData(true)] [InlineData(false)]
        public void WidgetFootersSurviveCardsAndRecordFieldsReceiveReplay(bool keyCell) {
            Flow footer = Chain();
            var table = new Table { AlwaysCards = true };
            table.AddColumn("Name"); table.AddColumn("Path");
            table.AddRow("Certificate", "finished");
            table.SetFooter(keyCell ? new[] { TableCell.FromValue(footer), new TableCell("") }
                : new[] { new TableCell(""), TableCell.FromValue(footer) });
            string text = Frame(table, 48).ToPlainText();
            Assert.Contains("client", text);
            Assert.Contains("intermediate", text);
            var record = new RecordView("Trust").AddField("Path", footer);
            Assert.Contains(footer, record.Children);
            Recording.ExportPlayerData(record, new[] { new LiveEvent("chain", "node", "issuer", state: "Fail", time: 100) },
                new RecordingExportOptions { Width = 48, Height = 80 });
            Assert.Equal(StatusKind.Fail, footer.Nodes[3].Status);
            var rich = RichText.Plain("label");
            var richTable = new Table(); richTable.AddColumn("Name"); richTable.AddRow(TableCell.FromValue(rich));
            Assert.Empty(richTable.Children);
            Assert.Empty(new RecordView().AddField("Name", rich).Children);
        }

        [Fact]
        public void InvalidTopologyFailsBeforeDrawing() {
            Flow flow = Chain();
            Assert.Throws<ArgumentException>(() => flow.Add(new FlowEdge("wrong", "client", "root")));
            Assert.Throws<ArgumentException>(() => flow.WithBranch(new FlowBranch("root", "back", "client")));
            Assert.Throws<ArgumentException>(() => flow.Add(new FlowNode("", "empty key")));
        }
        [Fact]
        public void LedgerPairsAndVerdictComposeAtNarrowWidth() {
            var phases = new Phases { StepCounter = true };
            phases.Set("dns", StatusKind.Pass, "Resolve DNS", "resolved");
            phases.Set("tls", StatusKind.Fail, "Validate certificate chain", "expired intermediate");
            var facts = new KeyValueList { Columns = 2 }.Add("Host", "portal.example.test").Add("Protocol", "TLS 1.3");
            string text = Frame(new Rows(new Panel(phases), facts, new Panel(new TextBlock("Repair chain"), "UNTRUSTED") { Verdict = true, Status = StatusKind.Fail }), 48).ToPlainText();
            Assert.Contains("[01/02]", text);
            Assert.Contains("expired intermediate", text);
            Assert.Contains("UNTRUSTED", text);
            Assert.Contains("╔", text);
        }
    }
}
