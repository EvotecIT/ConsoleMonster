using System;
using System.IO;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Export;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Layout;
using ConsoleMonster.Output;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Building {
    public sealed class FluentContentTests {
        [Fact]
        public void InitializerAndNestedContentChainProduceIdenticalFrames() {
            var initializer = new Panel {
                Title = "Build", HeaderRight = "2 jobs", Caption = "Ready", Border = BoxStyle.Rounded,
                Content = new TextBlock("All checks passed")
            };
            Panel fluent = new Panel().Title("Build").HeaderRight("2 jobs").Caption("Ready").Border(BoxStyle.Rounded)
                .Content(c => c.Text("All checks passed"));
            var options = new ExportOptions { Width = 60 };
            Assert.Equal(ConsoleExporter.ToText(initializer, options), ConsoleExporter.ToText(fluent, options));
            var expected = new StringTerminalWriter();
            var actual = new StringTerminalWriter();
            TerminalProfile profile = TerminalProfile.Fixed(60, 20);
            Terminal.Write(initializer, profile, expected);
            fluent.Show(profile, actual);
            Assert.Equal(expected.ToString(), actual.ToString());
        }

        [Fact]
        public void EveryContainerUsesTheSameContentGrammar() {
            Action<ContentBuilder> build = c => { c.Text("First"); c.Text("Second"); };
            IContainer[] containers = {
                new Panel().Content(build), new Rows().Content(build), new Columns().Content(build),
                new Stack().Content(build), new Grid(2).Content(build), new Align().Content(build), new Padding().Content(build),
                new Dashboard("Build", new Rows()).Content(build), new RegionWidget("main").Content(build)
            };
            foreach (IContainer container in containers) {
                string rendered = ConsoleExporter.ToText(container, new ExportOptions { Width = 60 });
                if (!(container is Stack)) { Assert.Contains("First", rendered); }
                Assert.Contains("Second", rendered);
            }
            var panel = new Panel().Content(c => c.Panel("Outer", nested => nested.Text("Nested")));
            Assert.Contains("Nested", ConsoleExporter.ToText(panel, new ExportOptions { Width = 60 }));
        }

        [Fact]
        public void ThrowingContentLambdaLeavesTheOriginalContentIntact() {
            IWidget original = new TextBlock("Keep");
            var panel = new Panel(original);
            Assert.Throws<InvalidOperationException>(() => panel.Content(c => { c.Text("Partial"); throw new InvalidOperationException(); }));
            Assert.Same(original, panel.Content);
            panel.Content(c => { });
            Assert.Empty(((IContainer)panel.Content).Children);
        }

        [Fact]
        public void ExportEndingWritesTheSameBytesAsTheSharedExporter() {
            string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".txt");
            try {
                Panel panel = new Panel().Title("Build").Content(c => c.Text("Ready"));
                var options = new ExportOptions { Width = 40 };
                panel.Export(path, options: options);
                Assert.Equal(FrameExporter.Export(panel, "Text", options), File.ReadAllBytes(path));
            } finally { if (File.Exists(path)) { File.Delete(path); } }
        }

        [Theory]
        [InlineData("Markdown", "ConsoleMonster.Markdown")]
        [InlineData("Png", "ConsoleMonster.Graphics")]
        public void OptionalFormatsNameTheirPackageWhenNoProviderIsAvailable(string format, string package) {
            var error = Assert.Throws<NotSupportedException>(() => FrameExporter.Export(new TextBlock("Ready"), format));
            Assert.Contains(package, error.Message);
        }
    }
}
