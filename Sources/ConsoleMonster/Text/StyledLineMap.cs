using System.Collections.Generic;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Text {
    // One segmentation of the whole line, independent of its style boundaries.
    internal sealed class StyledLineMap {
        private readonly StyledLine _line;
        internal StyledLineMap(StyledLine line) {
            _line = line;
            Text = line.PlainText;
            Clusters = new List<TextCluster>(DisplayWidth.Clusters(Text));
        }
        internal string Text { get; }
        internal List<TextCluster> Clusters { get; }

        internal bool IsSpace(int index) {
            TextCluster cluster = Clusters[index];
            return cluster.Length == 1 && (Text[cluster.Start] == ' ' || Text[cluster.Start] == '\t');
        }

        internal StyledLine Slice(int start, int end) {
            if (start >= end) {
                return StyledLine.Empty;
            }
            int first = Clusters[start].Start;
            TextCluster last = Clusters[end - 1];
            int limit = last.Start + last.Length;
            List<TextSpan> spans = new List<TextSpan>();
            int offset = 0;
            foreach (TextSpan span in _line) {
                int left = System.Math.Max(first, offset);
                int right = System.Math.Min(limit, offset + span.Text.Length);
                if (right > left) {
                    spans.Add(new TextSpan(span.Text.Substring(left - offset, right - left), span.Style));
                }
                offset += span.Text.Length;
                if (offset >= limit) {
                    break;
                }
            }
            return new StyledLine(spans);
        }

        internal Style StyleAt(int index) {
            int position = Clusters[index].Start;
            foreach (TextSpan span in _line) {
                if (position < span.Text.Length) {
                    return span.Style;
                }
                position -= span.Text.Length;
            }
            return Style.Plain;
        }

        internal int PrefixEnd(int start, int budget) {
            int end = start;
            while (end < Clusters.Count && Clusters[end].Width <= budget) {
                budget -= Clusters[end].Width;
                end++;
            }
            return end;
        }
    }
}
