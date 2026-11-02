using System;
using System.Linq;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Export;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Widgets {
    public sealed class DiagramTests {
        private static string Text(Diagram diagram, int width = 60) => ConsoleExporter.ToText(diagram, new ExportOptions { Width = width });

        [Fact]
        public void Tree_GroupsNodesAndRetainsEveryConnectionSubtitleAndValue() {
            var diagram = new Diagram("Small office") {
                Groups = { new DiagramGroup("Office LAN") { Nodes = { new DiagramNode("File server") { Id = "files", Subtitle = "disk 91%", Status = StatusKind.Warn, Value = "840 GB", To = { "Printer" } } } } },
                Links = { new DiagramLink("files", "Backup") { Label = "failed two nights; RPC server unavailable", Status = StatusKind.Fail } }
            };
            string text = Text(diagram, 34);
            foreach (string required in new[] { "Small office", "Office LAN", "File server", "disk 91%", "840 GB", "Printer", "Backup", "Needs attention" }) Assert.Contains(required, text);
            string joined = string.Concat(text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(line => line.Trim().TrimStart('│', '├', '└', '─', '|', '-', '+', ' ')));
            Assert.Contains("failed two nights; RPC", joined);
            Assert.Contains("server unavailable", joined);
            Assert.Equal(new[] { "Backup", "Printer", "files" }, diagram.Resolve().Select(n => n.Id).OrderBy(id => id, StringComparer.Ordinal));
            diagram.ShowSummary = false;
            Assert.DoesNotContain("Needs attention", Text(diagram));
        }

        [Fact]
        public void FromLinks_FluentAndInitializerUseLabelIdentitiesAndImplicitEndpoints() {
            var data = Diagram.FromLinks(new[] { (Source: "File server", Target: "Backup", Description: "nightly", Health: StatusKind.Fail) }, r => r.Source, r => r.Target, r => r.Description, r => r.Health, r => "Office LAN");
            var fluent = Ui.Diagram().Group("Office LAN", g => g.Node("File server")).Link("File server", "Backup", l => l.Label("nightly").Status(StatusKind.Fail)).Build;
            var initialized = new Diagram { Groups = { new DiagramGroup("Office LAN") { Nodes = { new DiagramNode("File server") } } }, Links = { new DiagramLink("File server", "Backup") { Label = "nightly", Status = StatusKind.Fail } } };
            Assert.Equal(Text(initialized), Text(data));
            Assert.Equal(Text(initialized), Text(fluent));
            Assert.Equal("Backup", data.Resolve().Single(node => node.Id == "Backup").Label);
        }

        [Theory]
        [InlineData(StatusKind.Warn)]
        [InlineData(StatusKind.Unknown)]
        [InlineData(StatusKind.None)]
        public void LiveValue_UsesCanonicalUpdatesAndKeepsSubtitleWhileChangingStatus(StatusKind status) {
            var diagram = new Diagram("Monitor", "ping") { Nodes = { new DiagramNode("VPN") { Subtitle = "vpn.contoso.com" } } };
            var dispatcher = new ImmediateDispatcher(diagram);
            DiagramHandle handle = diagram.Handle(dispatcher);
            handle.Value("VPN", "180 ms", status);
            string text = Text(diagram);
            Assert.Contains("vpn.contoso.com", text);
            Assert.Contains("180 ms", text);
            Assert.Contains("Needs attention", text);
            Assert.Equal(status, diagram.Nodes[0].Status);
            StatusKind replacement = status == StatusKind.Unknown ? StatusKind.None : StatusKind.Unknown;
            handle.Update(new WidgetUpdate { Key = "VPN", Status = replacement });
            Assert.Equal(replacement, diagram.Nodes[0].Status);
            Assert.Equal("180 ms", diagram.Nodes[0].Value);
            Assert.Throws<ConsoleUpdateException>(() => handle.Value("missing", "timeout", StatusKind.Fail));
            Assert.Equal("180 ms", diagram.Nodes[0].Value);
            handle.Value("VPN", null, StatusKind.Pass);
            Assert.Null(diagram.Nodes[0].Value);
            Assert.DoesNotContain("Needs attention", Text(diagram));
        }

        [Fact]
        public void EmptyAndInvalidModels_HaveExplicitResultsWithoutGraphics() {
            Assert.Contains("Empty", Text(new Diagram("Empty"), 12));
            var duplicate = new Diagram { Nodes = { new DiagramNode("A"), new DiagramNode("other") { Id = "A" } } };
            Assert.Throws<ArgumentException>(() => Text(duplicate));
            var group = new DiagramGroup("Cycle"); group.Groups.Add(group);
            Assert.Throws<ArgumentException>(() => Text(new Diagram { Groups = { group } }));
            Assert.Throws<NotSupportedException>(() => Diagram.Load("absent.md"));
        }

        [Fact]
        public void AsciiTree_UsesAsciiForLiteralLabelsSubtitlesValuesAndLinks() {
            var diagram = new Diagram("Łódź 🔒") { Nodes = { new DiagramNode("📦") { Subtitle = "✓ готово", Value = "98 % →" } }, Links = { new DiagramLink("📦", "Ω") { Label = "🔑 night →", Status = StatusKind.Fail } } };
            string text = ConsoleExporter.ToText(diagram, new ExportOptions { Width = 34, Glyphs = GlyphTier.Ascii });
            Assert.All(text, ch => Assert.True(ch < 128, "Non-ASCII character: " + ch));
            Assert.Contains("Needs attention", text);
            Assert.Contains("98 %", text);
        }
    }
}
