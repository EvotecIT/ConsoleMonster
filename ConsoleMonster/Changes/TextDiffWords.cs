using System.Collections.Generic;
using System.Text;
using ConsoleMonster.Text;

namespace ConsoleMonster.Changes {
    /// <summary>Word and punctuation alignment for replacement lines, using the same bounded sequence owner.</summary>
    internal static class TextDiffWords {
        internal static (string Before, string After) Highlight(string before, string after) {
            string[] a = Tokens(before);
            string[] b = Tokens(after);
            StringBuilder left = new StringBuilder();
            StringBuilder right = new StringBuilder();
            int x = 0;
            int y = 0;
            foreach (ChangeKind kind in TextSequenceDiff.Compare(a, b)) {
                if (kind == ChangeKind.Unchanged) {
                    left.Append(Markup.Escape(a[x++]));
                    right.Append(Markup.Escape(b[y++]));
                } else if (kind == ChangeKind.Removed) {
                    left.Append("[fail bold]").Append(Markup.Escape(a[x++])).Append("[/]");
                } else {
                    right.Append("[pass bold]").Append(Markup.Escape(b[y++])).Append("[/]");
                }
            }
            return (left.ToString(), right.ToString());
        }

        private static string[] Tokens(string text) {
            List<string> tokens = new List<string>();
            int start = 0;
            int previousCategory = -1;
            // Renderer styles are selected at each cluster's first position. Never split a cluster between tokens,
            // even when only its combining mark, surrogate pair, modifier or joined sequence has changed.
            foreach (TextCluster cluster in DisplayWidth.Clusters(text)) {
                int category = Category(text, cluster.Start);
                if (cluster.Start > start && (category != previousCategory || category == 2)) {
                    tokens.Add(text.Substring(start, cluster.Start - start));
                    start = cluster.Start;
                }
                previousCategory = category;
            }
            if (start < text.Length) {
                tokens.Add(text.Substring(start));
            }
            return tokens.ToArray();
        }

        private static int Category(string text, int index) => char.IsLetterOrDigit(text, index) || text[index] == '_'
            ? 0 : char.IsWhiteSpace(text, index) ? 1 : 2;
    }
}
