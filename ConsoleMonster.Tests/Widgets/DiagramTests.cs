using System;
using System.Linq;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Export;
using ConsoleMonster.Live;
using ConsoleMonster.Layout;
using ConsoleMonster.Output;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Widgets {
    public sealed class DiagramTests {
        private static string Text(Diagram diagram, int width = 60) => ConsoleExporter.ToText(diagram, new ExportOptions { Width = width });

        [Theory]
        [InlineData(GlyphTier.Full, "→")]
        [InlineData(GlyphTier.Basic, "→")]
        [InlineData(GlyphTier.Ascii, "->")]
        public void Tree_ShowsOnlySuppliedStatusesAndKeepsMetadataAndLinksInline(GlyphTier tier, string arrow) {
            var source = new DiagramNode("File server") { Subtitle = "FS01", Value = "disk 91%" };
            var diagram = new Diagram("Office") { Groups = { new DiagramGroup("LAN") { Nodes = { source, new DiagramNode("Backup") } } }, Links = { new DiagramLink("File server", "Backup") { Label = "nightly" } } };
            string Render() => ConsoleExporter.ToText(diagram, new ExportOptions { Width = 80, Glyphs = tier });
            string unset = Render();
            Assert.Contains("File server  FS01  disk 91%", unset);
            Assert.Contains(arrow + " Backup  nightly", unset);
            Assert.DoesNotContain("Pass", unset);
            Assert.DoesNotContain("Needs attention", unset);
            source.Status = StatusKind.Pass;
            Assert.DoesNotContain("Pass", Render());
            source.Status = StatusKind.Warn;
            string warning = Render();
            Assert.Contains("Warn", warning);
            Assert.DoesNotContain("Pass", warning);
            Assert.Contains("Needs attention", warning);
            string summary = warning.Substring(warning.IndexOf("Needs attention", StringComparison.Ordinal));
            Assert.DoesNotContain("├", summary);
            Assert.DoesNotContain("└", summary);
            Assert.Contains("File server  disk 91%", summary);
            Assert.DoesNotContain("FS01", summary);
            diagram.Links[0].Status = StatusKind.Pass;
            string healthyLink = Render().Split('\n').First(line => line.Contains(arrow + " Backup"));
            Assert.DoesNotContain("✓", healthyLink);
            Assert.DoesNotContain("PASS", healthyLink);
        }

        [Fact]
        public void Tree_NestsAForestAndRetainsCycleCrossLinkAndEdgeEvidence() {
            var diagram = new Diagram("Team") { Nodes = { new DiagramNode("Anna"), new DiagramNode("Marek"), new DiagramNode("Julia"), new DiagramNode("Tom"), new DiagramNode("Ola"), new DiagramNode("Ben") }, Links = {
                new DiagramLink("Anna", "Marek"), new DiagramLink("Anna", "Julia"), new DiagramLink("Marek", "Tom"), new DiagramLink("Marek", "Ola"), new DiagramLink("Julia", "Ben")
            } };
            string forest = ConsoleExporter.ToText(diagram, new ExportOptions { Width = 80, Glyphs = GlyphTier.Full });
            Assert.Contains("    ├── Marek", forest);
            Assert.Contains("    │   ├── Tom", forest);
            Assert.Contains("    │   └── Ola", forest);
            Assert.Contains("    └── Julia", forest);
            Assert.Contains("        └── Ben", forest);
            Assert.DoesNotContain("→", forest);
            diagram.Links.Add(new DiagramLink("Tom", "Anna") { Label = "escalates", Status = StatusKind.Warn });
            diagram.Links.Add(new DiagramLink("Julia", "Tom") { Label = "consults" });
            diagram.ShowSummary = false;
            string references = ConsoleExporter.ToText(diagram, new ExportOptions { Width = 80, Glyphs = GlyphTier.Full });
            Assert.Contains("→ Anna  escalates", references);
            Assert.Contains("→ Tom  consults", references);
            Assert.Equal(1, references.Split('\n').Count(line => line.TrimEnd().EndsWith(" Ben", StringComparison.Ordinal)));
        }

        [Fact]
        public void Attention_PrefersFailedConnectionEvidenceToAnUnhelpfulSubtitle() {
            var diagram = new Diagram("Office") { Nodes = { new DiagramNode("Backup") { Subtitle = "Offsite", Status = StatusKind.Fail } }, Links = { new DiagramLink("File server", "Backup") { Label = "failed two nights", Status = StatusKind.Fail } } };
            string text = Text(diagram, 80);
            string summary = text.Substring(text.IndexOf("Needs attention", StringComparison.Ordinal));
            Assert.Contains("Backup  failed two nights", summary);
            Assert.DoesNotContain("Offsite", summary);
        }

        [Fact]
        public void Tree_GroupsNodesAndRetainsEveryConnectionSubtitleAndValue() {
            var diagram = new Diagram("Small office") {
                Groups = { new DiagramGroup("Office LAN") { Nodes = { new DiagramNode("File server") { Id = "files", Subtitle = "disk 91%", Status = StatusKind.Warn, Value = "840 GB", To = { "Printer" } } } } },
                Links = { new DiagramLink("files", "Backup") { Label = "failed two nights; RPC server unavailable", Status = StatusKind.Fail } }
            };
            string text = Text(diagram, 34);
            foreach (string required in new[] { "Small office", "Office LAN", "File server", "disk 91%", "840 GB", "Printer", "Backup", "Needs attention" }) Assert.Contains(required, text);
            string joined = string.Join(" ", text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(line => line.Trim().TrimStart('│', '├', '└', '─', '|', '-', '+', ' ')));
            Assert.Contains("failed two nights; RPC", joined);
            Assert.Contains("server unavailable", joined);
            Assert.Equal(new[] { "Backup", "Printer", "files" }, diagram.Resolve().Select(n => n.Id).OrderBy(id => id, StringComparer.Ordinal));
            diagram.ShowSummary = false;
            Assert.DoesNotContain("Needs attention", Text(diagram));
        }

        [Fact]
        public void FromLinks_FluentAndInitializerUseLabelIdentitiesAndImplicitEndpoints() {
            var data = Diagram.FromLinks(new[] { (Source: "File server", Target: "Backup", Description: "nightly", Health: StatusKind.Fail) }, r => r.Source, r => r.Target, r => r.Description, r => r.Health, r => "Office LAN");
            Diagram fluent = Ui.Diagram();
            Assert.Same(fluent, fluent.Direction(DiagramDirection.Right).Summary(false).ShowLegend(false)
                .Group("Office LAN", g => g.Node("File server"))
                .Link("File server", "Backup", l => l.Label("nightly").Status(StatusKind.Fail)));
            Assert.False(fluent.ShowSummary);
            Assert.False(fluent.ShowLegend);
            fluent.Summary().ShowLegend();
            var initialized = new Diagram { Groups = { new DiagramGroup("Office LAN") { Nodes = { new DiagramNode("File server") } } }, Links = { new DiagramLink("File server", "Backup") { Label = "nightly", Status = StatusKind.Fail } } };
            Assert.Equal(Text(initialized), Text(data));
            Assert.Equal(Text(initialized), Text(fluent));
            var expected = new StringTerminalWriter();
            var actual = new StringTerminalWriter();
            TerminalProfile profile = TerminalProfile.Fixed(60, 30);
            Terminal.Write(initialized, profile, expected);
            fluent.Show(profile, actual);
            Assert.Equal(expected.ToString(), actual.ToString());
            var composed = new Panel().Content(c => c.Diagram(d => d
                .Group("Office LAN", g => g.Node("File server"))
                .Link("File server", "Backup", l => l.Label("nightly").Status(StatusKind.Fail))));
            Assert.Equal(ConsoleExporter.ToText(new Panel(initialized)), ConsoleExporter.ToText(composed));
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
