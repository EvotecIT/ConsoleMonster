using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ConsoleMonster.Canvas;
using ConsoleMonster.Testing;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Widgets {
    /// <summary>
    /// Histogram edge labels thin out evenly when the bars are narrow: every Nth edge is labelled with the smallest N that
    /// keeps a space between labels, and the first and last edges are always labelled.
    /// </summary>
    public sealed class HistogramLabelTests {
        // The labels on the bottom row with where each starts.
        private static List<KeyValuePair<int, string>> Labels(Histogram histogram, int width) {
            CellBuffer buffer = TestTerminal.Render(histogram, width);
            string row = buffer.GetRowText(buffer.Height - 1).TrimEnd();
            List<KeyValuePair<int, string>> labels = new List<KeyValuePair<int, string>>();
            for (int x = 0; x < row.Length; x++) {
                if (row[x] == ' ') {
                    continue;
                }
                int start = x;
                while (x < row.Length && row[x] != ' ') {
                    x++;
                }
                labels.Add(new KeyValuePair<int, string>(start, row.Substring(start, x - start)));
            }
            return labels;
        }

        // Edges 0, 5, 10, ... so every label names its edge (label / 5 is the edge index).
        private static Histogram Bins(int bins) {
            double[] edges = Enumerable.Range(0, bins + 1).Select(i => i * 5.0).ToArray();
            Histogram histogram = new Histogram(edges) { Height = 3 };
            foreach (double value in edges.Take(bins)) {
                histogram.Add(value + 1);
            }
            return histogram;
        }

        [Theory]
        [InlineData(4, 17)]
        [InlineData(8, 20)]
        [InlineData(12, 30)]
        [InlineData(16, 40)]
        [InlineData(20, 33)]
        [InlineData(24, 60)]
        [InlineData(10, 80)]
        public void Render_NarrowBars_LabelsEveryNthEdgeEvenlyAndKeepsTheEnds(int bins, int width) {
            List<KeyValuePair<int, string>> labels = Labels(Bins(bins), width);
            List<int> edges = labels.Select(l => int.Parse(l.Value, CultureInfo.InvariantCulture) / 5).ToList();

            Assert.Equal(0, edges[0]);
            Assert.Equal(bins, edges[edges.Count - 1]);
            Assert.Equal(width, labels[labels.Count - 1].Key + labels[labels.Count - 1].Value.Length);
            List<int> inner = edges.Take(edges.Count - 1).ToList();
            int step = inner.Count > 1 ? inner[1] - inner[0] : bins;
            for (int i = 1; i < inner.Count; i++) {
                Assert.Equal(step, inner[i] - inner[i - 1]);
            }
            // The step is the smallest that fits: one less would make two labels touch.
            if (step > 1 && inner.Count > 1) {
                int plotX = labels[0].Key;
                int plotWidth = width - plotX;
                bool touches = false;
                for (int b = step - 1; b < bins; b += step - 1) {
                    int previous = b - (step - 1);
                    int previousEnd = plotX + previous * plotWidth / bins + (previous * 5).ToString(CultureInfo.InvariantCulture).Length;
                    touches |= plotX + b * plotWidth / bins < previousEnd + 1;
                }
                Assert.True(touches, "A step of " + (step - 1) + " would also have fitted.");
            }
        }

        [Fact]
        public void Render_WideBars_LabelsEveryEdge() {
            List<KeyValuePair<int, string>> labels = Labels(Bins(4), 60);
            Assert.Equal(new[] { "0", "5", "10", "15", "20" }, labels.Select(l => l.Value).ToArray());
        }

        [Fact]
        public void Render_TwelveBinsAtThirtyColumns_SpacesTheLabelsEvenly() {
            // Skipping greedily used to show 0 5 10 15 25 30 40 45 60 (gaps of one and two); now every second edge is labelled.
            List<KeyValuePair<int, string>> labels = Labels(Bins(12), 30);
            Assert.Equal(new[] { "0", "10", "20", "30", "40", "50", "60" }, labels.Select(l => l.Value).ToArray());
        }

        [Fact]
        public void LabelStep_PicksTheSmallestStepWithoutTouchingLabels() {
            Assert.Equal(1, Histogram.LabelStep(new[] { 0, 4, 8 }, new[] { 2, 2, 2 }));
            Assert.Equal(2, Histogram.LabelStep(new[] { 0, 2, 4, 6 }, new[] { 2, 2, 2, 2 }));
            Assert.Equal(3, Histogram.LabelStep(new[] { 0, 2, 4, 6, 8 }, new[] { 4, 4, 4, 4, 4 }));
        }
    }
}
