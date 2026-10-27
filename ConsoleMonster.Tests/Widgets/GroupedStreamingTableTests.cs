using System;
using System.Collections.Generic;
using System.Linq;
using ConsoleMonster.Canvas;
using ConsoleMonster.Styling;
using ConsoleMonster.Testing;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Widgets {
    public sealed class GroupedStreamingTableTests {
        private static GroupedStreamingTable<Row, int> Streaming(List<string> closed) => new GroupedStreamingTable<Row, int>(
            new[] { new TableColumn("Name"), new TableColumn("Value") }, row => row.Group,
            row => new[] { TableCell.FromValue(row.Name), TableCell.FromValue(row.Value) },
            group => 0, (sum, row) => sum + row.Value,
            (group, sum, count) => { closed.Add(group + ":" + count); return TextBlock.Plain(group + " closed " + count + " rows total " + sum); });

        private static RenderContext Context(int width = 80) => Terminal.CreateContext(TestTerminal.Profile(width), Theme.Dark);

        [Fact]
        public void RenderChunks_ContiguousGroups_ClosesOnceBeforeTheNextHeaderAndAtEnd() {
            List<string> closed = new List<string>();
            var stream = Streaming(closed);
            Row[] source = { new Row("A", "first", 2), new Row("A", "second", 3), new Row("B", "third", 7), new Row("A", "fourth", 11) };
            List<string> blocks = stream.RenderChunks(source, 80, Context()).Select(frame => frame.ToPlainText()).ToList();
            Assert.Equal(new[] { "A:2", "B:1", "A:1" }, closed);
            Assert.Equal(10, blocks.Count); // Three headings, four data rows, three closures.
            Assert.Equal("A", blocks[0]);
            Assert.Contains("first", blocks[1]);
            Assert.Contains("second", blocks[2]);
            Assert.Equal("A closed 2 rows total 5", blocks[3]);
            Assert.Equal("B", blocks[4]);
            Assert.Equal("B closed 1 rows total 7", blocks[6]);
            Assert.Equal("A", blocks[7]);
            Assert.Equal("A closed 1 rows total 11", blocks[9]);
        }

        [Fact]
        public void RenderChunks_UnboundedGroup_FirstOutputUsesBoundedLookaheadAndDisposesInput() {
            int read = 0;
            bool disposed = false;
            IEnumerable<Row> Infinite() {
                try {
                    while (true) { read++; yield return new Row("open", "item", 1); }
                } finally { disposed = true; }
            }
            List<string> closed = new List<string>();
            var stream = Streaming(closed);
            using (IEnumerator<CellBuffer> output = stream.RenderChunks(Infinite(), 80, Context()).GetEnumerator()) {
                Assert.Equal(0, read);
                Assert.True(output.MoveNext());
                Assert.Equal(200, read);
                for (int i = 0; i < 205; i++) Assert.True(output.MoveNext());
                Assert.InRange(read, 205, 206);
                Assert.Empty(closed);
            }
            Assert.True(disposed);
            Assert.Empty(closed);
        }

        [Fact]
        public void RenderChunks_SampleWidths_DoNotGrowForALateLongValue() {
            List<string> closed = new List<string>();
            var stream = Streaming(closed);
            stream.SampleRows = 1;
            Row[] source = { new Row("A", "short", 2), new Row("A", "a much longer late name", 3) };
            List<string> blocks = stream.RenderChunks(source, 80, Context()).Select(frame => frame.ToPlainText()).ToList();
            string firstData = blocks[1].Split('\n').Last();
            string lateData = blocks[2].Split('\n').First();
            Assert.Equal(firstData.IndexOf('2'), lateData.IndexOf('3'));
            Assert.Equal(new[] { "A:2" }, closed);
        }

        [Fact]
        public void RenderChunks_Selectors_EnumerateAndProjectEachRecordOnce() {
            int projected = 0;
            int enumerated = 0;
            IEnumerable<Row> Source() {
                enumerated++;
                yield return new Row("A", "one", 1);
                yield return new Row("B", "two", 2);
            }
            var stream = new GroupedStreamingTable<Row, int>(new[] { new TableColumn("Name") }, row => row.Group,
                row => { projected++; return new[] { TableCell.FromValue(row.Name) }; }, group => 0,
                (count, row) => count + 1, (group, count, rows) => TextBlock.Plain("done"));
            Assert.NotEmpty(stream.RenderChunks(Source(), 60, Context(60)).ToList());
            Assert.Equal(1, enumerated);
            Assert.Equal(2, projected);
        }

        [Fact]
        public void RenderChunks_EmptyInput_DoesNotCreateOrCloseAGhostGroup() {
            List<string> closed = new List<string>();
            Assert.Empty(Streaming(closed).RenderChunks(Array.Empty<Row>(), 80, Context()));
            Assert.Empty(closed);
        }

        [Fact]
        public void RenderChunks_NarrowCards_RetainsEveryFieldAndStillClosesTheGroup() {
            int closures = 0;
            var stream = new GroupedStreamingTable<string, int>(
                new[] { new TableColumn("Name"), new TableColumn("First detail"), new TableColumn("Second detail") },
                value => "scope", value => new[] { TableCell.FromValue(value), TableCell.FromValue("first-long-literal-value"), TableCell.FromValue("second-long-literal-value") },
                group => 0, (count, value) => count + 1, (group, state, count) => { closures++; return TextBlock.Plain("closed"); });
            string text = string.Join("\n", stream.RenderChunks(new[] { "item" }, 30, Context(30)).Select(frame => frame.ToPlainText()));
            string compact = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", string.Empty);
            Assert.Contains("first-long-literal-value", compact);
            Assert.Contains("second-long-literal-value", compact);
            Assert.Contains("closed", text);
            Assert.Equal(1, closures);
        }

        private sealed class Row {
            internal Row(string group, string name, int value) { Group = group; Name = name; Value = value; }
            internal string Group { get; }
            internal string Name { get; }
            internal int Value { get; }
        }
    }
}
