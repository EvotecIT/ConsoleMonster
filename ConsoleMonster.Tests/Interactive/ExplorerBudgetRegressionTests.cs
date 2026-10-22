using System;
using System.Collections;
using System.Collections.Specialized;
using System.Linq;
using ConsoleMonster.Data;
using ConsoleMonster.Interactive;
using ConsoleMonster.Testing;
using Xunit;

namespace ConsoleMonster.Tests.Interactive {
    /// <summary>Filtering and preview probes leave the navigation budget for visible members.</summary>
    public sealed class ExplorerBudgetRegressionTests {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void NonexpandableEnumerableRootsRenderTheirSummary(bool depthLimited) {
            InspectorOptions options = Options(5);
            if (depthLimited) { options.MaxDepth = 0; }
            object value = depthLimited ? (object)new[] { 7 } : "literal-root";
            ObjectExplorer explorer = new ObjectExplorer(value, options);
            Assert.False(explorer.Root.CanExpand);
            Assert.Empty(explorer.Root.Children);

            string frame = TestTerminal.RenderText(explorer.Snapshot(), 80, 20);

            Assert.Contains(depthLimited ? "depth limit" : "literal-root", frame);
            Assert.Contains("$x", frame);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void DictionaryFiltersApplyBeforeTheVisibleNodeLimit(bool include) {
            OrderedDictionary values = new OrderedDictionary();
            for (int i = 0; i < 40; i++) { values.Add("Hidden" + i, i); }
            values.Add("Wanted", "visible");
            InspectorOptions options = Options(2);
            if (include) { options.Include.Add("Wanted"); }
            else { options.Exclude.Add("Hidden*"); }
            ExplorerNode root = new ObjectExplorer(values, options).Root;
            ExplorerNode member = Assert.Single(root.Children);
            Assert.Equal("Wanted", member.Name);
            Assert.Equal("visible", member.Value);
            Assert.Equal("$x['Wanted']", member.Path);
            Assert.False(root.HasMore);
        }

        [Fact]
        public void HeterogeneousPreviewDoesNotSpendInvisibleMemberNodes() {
            object[] rows = { new { First = 1 }, new { Other = 2 }, new { Last = 3 } };
            ObjectExplorer explorer = new ObjectExplorer(rows, Options(5));
            TestTerminal.RenderText(explorer.Snapshot(), 80, 20);
            ExplorerNode last = explorer.Root.Children.Last();
            Assert.Equal("Last", Assert.Single(last.Children).Name);
            Assert.Equal(3, last.Children[0].Value);
        }

        [Fact]
        public void PreviewThatCannotFitTheRemainingBudgetDoesNotPartiallyExpandRows() {
            var rows = new[] { new { A = 1, B = 2 }, new { A = 3, B = 4 }, new { A = 5, B = 6 } };
            ObjectExplorer explorer = new ObjectExplorer(rows, Options(7));
            TestTerminal.RenderText(explorer.Snapshot(), 80, 20);
            ExplorerNode last = explorer.Root.Children.Last();
            Assert.Equal(new[] { "A", "B" }, last.Children.Select(n => n.Name));
            Assert.Equal(new object?[] { 5, 6 }, last.Children.Select(n => n.Value));
            Assert.False(last.HasMore);
        }

        [Fact]
        public void ShortViewportDoesNotMaterializeAnInvisiblePreview() {
            var rows = new[] { new { A = 1 }, new { A = 2 }, new { A = 3 } };
            ObjectExplorer explorer = new ObjectExplorer(rows, Options(5));
            TestTerminal.RenderText(explorer.Snapshot(), 80, 8);
            Assert.Equal(3, Assert.Single(explorer.Root.Children.Last().Children).Value);
        }

        [Fact]
        public void DisplayedUniformPreviewReadsValuesOnceAndReusesThemForNavigation() {
            CountingRow[] rows = { new CountingRow(), new CountingRow(), new CountingRow() };
            ObjectExplorer explorer = new ObjectExplorer(rows, Options(7));
            var snapshot = explorer.Snapshot();
            string frame = TestTerminal.RenderText(snapshot, 80, 20);
            TestTerminal.RenderText(snapshot, 80, 20);
            Assert.Contains("Payload", frame);
            foreach (ExplorerNode node in explorer.Root.Children) { Assert.Equal("payload", Assert.Single(node.Children).Value); }
            Assert.All(rows, row => Assert.Equal(1, row.Reads));
        }

        [Fact]
        public void ThrowingCollectionCountStaysExpandableAndDoesNotAbortRendering() {
            ObjectExplorer explorer = new ObjectExplorer(new { Items = new ThrowingCount() }, Options(10));
            string frame = TestTerminal.RenderText(explorer.Snapshot(), 80, 20);
            ExplorerNode node = Assert.Single(explorer.Root.Children);
            Assert.Contains("count unavailable", frame);
            Assert.Null(node.Error);
            Assert.True(node.CanExpand);
            Assert.Equal(7, Assert.Single(node.Children).Value);
        }

        [Fact]
        public void CachedChildShapesAreCheckedBeforeUncachedPreviewRowsSpendNodes() {
            OrderedDictionary[] rows = Enumerable.Range(0, 4).Select(_ => new OrderedDictionary { ["A"] = 1 }).ToArray();
            ObjectExplorer explorer = new ObjectExplorer(rows, Options(10));
            var nodes = explorer.Root.Children;
            Assert.Single(nodes[2].Children);
            for (int i = 0; i < 3; i++) { rows[i].Add("B", 2); }
            TestTerminal.RenderText(explorer.Snapshot(), 80, 20);
            Assert.Equal("A", Assert.Single(nodes[3].Children).Name);
            Assert.False(nodes[3].HasMore);
        }

        [Fact]
        public void UniformArrayRowsRetainTheirInlineGrid() {
            string[][] rows = { new[] { "matrix-11", "matrix-12" }, new[] { "matrix-21", "matrix-22" } };
            ObjectExplorer explorer = new ObjectExplorer(rows, Options(7));
            string frame = TestTerminal.RenderText(explorer.Snapshot(), 80, 20);
            foreach (string value in rows.SelectMany(row => row)) { Assert.Contains(value, frame); }
        }

        private static InspectorOptions Options(int maxNodes) => new InspectorOptions { MaxNodes = maxNodes, TimeBudget = TimeSpan.Zero };

        private sealed class CountingRow {
            internal int Reads;
            public string Payload { get { Reads++; return "payload"; } }
        }

        private sealed class ThrowingCount : ICollection {
            public int Count => throw new InvalidOperationException("count unavailable");
            public bool IsSynchronized => false;
            public object SyncRoot => this;
            public void CopyTo(Array array, int index) => throw new NotSupportedException();
            public IEnumerator GetEnumerator() { yield return 7; }
        }
    }
}
