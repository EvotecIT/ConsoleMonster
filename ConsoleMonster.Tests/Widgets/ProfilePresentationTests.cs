using System;
using System.Globalization;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Data;
using ConsoleMonster.Export;
using ConsoleMonster.Localization;
using ConsoleMonster.Layout;
using ConsoleMonster.Output;
using ConsoleMonster.Presenters;
using ConsoleMonster.Rendering;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Widgets {
    public sealed class ProfilePresentationTests {
        private static RenderContext Context(string language) => new RenderContext(
            TerminalProfile.Fixed(100, 40).WithLanguage(CultureInfo.GetCultureInfo(language)), Theme.Dark);

        private static string Render(IWidget widget, RenderContext context) => FrameComposer.RenderBlock(widget, 100, context).ToPlainText();

        [Fact]
        public void ProfileLanguageReachesInspectorsHierarchyAndStatusWordsWithoutRebuildingData() {
            InspectNode inspected = ObjectInspector.Inspect(new { Items = new[] { 1, 2, 3 } }, new InspectorOptions { MaxItems = 1 });
            InspectorView view = new InspectorView(inspected);
            Tree tree = InspectorTree.Build(inspected);
            Tree hierarchy = new Tree("Hierarchy");
            TreeHierarchy.Add(hierarchy.Root, new[] { 1, 2, 3 }, value => new TreeNode(value.ToString(CultureInfo.InvariantCulture)), value => null,
                new TreeHierarchyOptions { MaxItems = 1 });
            RenderContext english = Context("en");
            RenderContext polish = Context("pl");
            Assert.Contains("+2 more", Render(view, english));
            Assert.Contains("+2 więcej", Render(view, polish));
            Assert.Contains("elementy: 3", Render(view, polish));
            Assert.Contains("+2 więcej", Render(tree, polish));
            Assert.Contains("elementy: 3", Render(tree, polish));
            Assert.Contains("+2 więcej", Render(hierarchy, polish));
            Assert.Equal("BŁĄD", StatusKind.Fail.ToWord(polish));
            Assert.Equal("BŁĄD", StatusKind.Fail.ToWord(polish.Profile));
            using (Terminal.Use(new RecordingTarget(polish.Profile))) {
                Assert.Equal("BŁĄD", StatusKind.Fail.ToWord());
            }
            Assert.Contains("+2 more", Render(view, english));
            Assert.Contains("+2 more", Render(tree, english));
            Assert.Contains("+2 more", Render(hierarchy, english));
        }

        [Fact]
        public void HierarchyLocalizesCycleDepthAndNodeLimitsAndKeepsExplicitNotes() {
            RenderContext polish = Context("pl");
            Tree cycle = new Tree();
            object item = new object();
            TreeHierarchy.Add(cycle.Root, new[] { item }, value => new TreeNode("node") { Note = "owner note" }, value => new[] { item });
            Assert.Contains("owner note (cykl)", Render(cycle, polish));
            Tree depth = new Tree();
            TreeHierarchy.Add(depth.Root, new[] { 1 }, value => new TreeNode("node"), value => new[] { value + 1 }, new TreeHierarchyOptions { Depth = 0 });
            Assert.Contains("limit głębokości", Render(depth, polish));
            Tree nodes = new Tree();
            TreeHierarchy.Add(nodes.Root, new[] { 1, 2 }, value => new TreeNode("node"), value => null, new TreeHierarchyOptions { MaxNodes = 1 });
            Assert.Contains("osiągnięto limit węzłów", Render(nodes, polish));
            Assert.Equal("custom note", new TreeNode("literal") { Note = "custom note" }.Note);
        }

        [Theory]
        [InlineData("2026-10-21T12:00:00Z")]
        [InlineData("2026-10-21T14:00:00.0000000+02:00")]
        public void RecordingReferenceTimeIsSharedByWidgetsAndExplicitNowWins(string reference) {
            string? saved = Environment.GetEnvironmentVariable("CONSOLEMONSTER_NOW");
            DateTimeOffset expected = new DateTimeOffset(2026, 10, 21, 12, 0, 0, TimeSpan.Zero);
            try {
                Table table = new Table();
                table.AddColumn(new TableColumn("Changed") { Presenter = new DatePresenter(DateDisplay.Relative) });
                table.AddValues(expected.AddHours(-3));
                Environment.SetEnvironmentVariable("CONSOLEMONSTER_NOW", reference);
                Assert.Equal(expected, new PresenterContext(Context("en")).Now);
                Assert.Equal(expected, new PresentationOptions().CreateContext(Context("en")).Now);
                Assert.Contains("3 h ago", ConsoleExporter.ToMarkdown(table, new ExportOptions { Culture = CultureInfo.InvariantCulture }));
                table.Presentation.Now = expected.AddHours(1);
                Assert.Contains("4 h ago", ConsoleExporter.ToMarkdown(table, new ExportOptions { Culture = CultureInfo.InvariantCulture }));
            } finally {
                Environment.SetEnvironmentVariable("CONSOLEMONSTER_NOW", saved);
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData("invalid")]
        [InlineData("2026-10-21")]
        public void UnsetOrInvalidRecordingTimeUsesTheCurrentClock(string? reference) {
            string? saved = Environment.GetEnvironmentVariable("CONSOLEMONSTER_NOW");
            try {
                Environment.SetEnvironmentVariable("CONSOLEMONSTER_NOW", reference);
                DateTimeOffset before = DateTimeOffset.Now;
                DateTimeOffset actual = new PresenterContext(Context("en")).Now;
                Assert.InRange(actual, before, DateTimeOffset.Now);
            } finally {
                Environment.SetEnvironmentVariable("CONSOLEMONSTER_NOW", saved);
            }
        }
    }
}
