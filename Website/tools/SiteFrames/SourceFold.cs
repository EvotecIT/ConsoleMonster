using System;
using System.Collections.Generic;

namespace SiteFrames {
    /// <summary>
    /// The short form of a folded block (<c>#region more</c>, <c>// &lt;more&gt;</c>): its outline, with a
    /// "… N more lines in the full program" line for each run of lines it leaves out. What a shown line opens is closed
    /// on screen too, so a short form never shows <c>$servers = @(</c> without its <c>)</c>:
    /// <list type="bullet">
    /// <item>The first statement keeps its first line and, when that line opens brackets, the line that closes them.</item>
    /// <item>The rest folds into one line, followed by the block's last line when that is a statement of its own at the
    /// block's indentation (not a comment, not a continuation); when the rest is one bracketed statement, its first and
    /// closing lines are kept instead.</item>
    /// <item>A fold never stands in for a single line (the line is shown), and blank lines after the last code line stay.</item>
    /// </list>
    /// Brackets are counted outside strings and comments, which is enough for example code; when a first statement never
    /// closes inside the block, the block's last line is shown as its end.
    /// </summary>
    internal static class SourceFold {
        public static List<string> Fold(List<string> block, bool csharp) {
            List<string> shown = new List<string>();
            int last = block.Count - 1;
            while (last > 0 && block[last].Trim().Length == 0) {
                last--;
            }
            int[] depth = Depths(block, last, csharp);
            int end = StatementEnd(depth, 0, last);
            shown.Add(block[0]);
            if (end > 0) {
                Hide(block, 1, end - 1, csharp, shown);
                shown.Add(block[end]);
            }
            int rest = end + 1;
            if (rest == last) {
                shown.Add(block[last]);
            } else if (rest < last) {
                int restEnd = StatementEnd(depth, rest, last);
                if (restEnd == last && depth[rest] > depth[rest - 1]) {
                    shown.Add(block[rest]);
                    Hide(block, rest + 1, last - 1, csharp, shown);
                    shown.Add(block[last]);
                } else if (StandsAlone(block, depth, rest, last, csharp)) {
                    Hide(block, rest, last - 1, csharp, shown);
                    shown.Add(block[last]);
                } else {
                    Hide(block, rest, last, csharp, shown);
                }
            }
            for (int i = last + 1; i < block.Count; i++) {
                shown.Add(block[i]);
            }
            return shown;
        }

        // The line that ends the statement starting at `start`: where the bracket depth comes back to what it was before
        // it, the start itself when its line leaves nothing open, or the block's last line when it never comes back.
        private static int StatementEnd(int[] depth, int start, int last) {
            int before = start == 0 ? 0 : depth[start - 1];
            if (depth[start] <= before) {
                return start;
            }
            for (int i = start + 1; i <= last; i++) {
                if (depth[i] <= before) {
                    return i;
                }
            }
            return last;
        }

        // Whether the last line can be shown after a fold: a statement of its own, at the block's indentation or less.
        private static bool StandsAlone(List<string> block, int[] depth, int rest, int last, bool csharp) {
            string line = block[last].Trim();
            string previous = block[last - 1].TrimEnd();
            bool comment = csharp ? line.StartsWith("//", StringComparison.Ordinal) || line.StartsWith("/*", StringComparison.Ordinal) || line.StartsWith("*", StringComparison.Ordinal) : line.StartsWith("#", StringComparison.Ordinal);
            bool continues = previous.EndsWith("`", StringComparison.Ordinal) || previous.EndsWith("|", StringComparison.Ordinal) || previous.EndsWith("=", StringComparison.Ordinal) || previous.EndsWith("+", StringComparison.Ordinal) || previous.EndsWith("&&", StringComparison.Ordinal) || previous.EndsWith("=>", StringComparison.Ordinal);
            return !comment && !continues && depth[last - 1] == depth[rest - 1] && depth[last] == depth[rest - 1] && Indent(block[last]).Length <= Indent(block[0]).Length;
        }

        // Replaces lines from..to with one fold line; a single line is shown as it is, since a fold would not be shorter.
        private static void Hide(List<string> block, int from, int to, bool csharp, List<string> shown) {
            int count = to - from + 1;
            if (count <= 0) {
                return;
            }
            if (count == 1) {
                shown.Add(block[from]);
                return;
            }
            string indent = Indent(block[from - 1]);
            for (int i = from; i <= to; i++) {
                if (block[i].Trim().Length > 0) {
                    indent = Indent(block[i]);
                    break;
                }
            }
            shown.Add(indent + (csharp ? "// … " : "# … ") + count + " more lines in the full program");
        }

        // The bracket depth after each line, counted from the start of the block, outside strings and comments.
        private static int[] Depths(List<string> block, int last, bool csharp) {
            int[] depths = new int[last + 1];
            int depth = 0;
            bool blockComment = false;
            for (int i = 0; i <= last; i++) {
                string line = block[i];
                for (int c = 0; c < line.Length; c++) {
                    char ch = line[c];
                    if (blockComment) {
                        if (csharp ? ch == '*' && Next(line, c) == '/' : ch == '#' && Next(line, c) == '>') {
                            blockComment = false;
                            c++;
                        }
                        continue;
                    }
                    if (csharp ? ch == '/' && Next(line, c) == '*' : ch == '<' && Next(line, c) == '#') {
                        blockComment = true;
                        c++;
                        continue;
                    }
                    if (csharp ? ch == '/' && Next(line, c) == '/' : ch == '#') {
                        break;
                    }
                    if (ch == '"' || ch == '\'') {
                        c = StringEnd(line, c, csharp);
                        continue;
                    }
                    if (ch == '(' || ch == '[' || ch == '{') {
                        depth++;
                    } else if (ch == ')' || ch == ']' || ch == '}') {
                        depth--;
                    }
                }
                depths[i] = depth;
            }
            return depths;
        }

        // The index of the quote that closes the string opening at `start` (or the line's end): a doubled quote stays inside
        // in both languages, and an escape (a backslash in C#, a backtick in a PowerShell double-quoted string) skips the
        // next character.
        private static int StringEnd(string line, int start, bool csharp) {
            char quote = line[start];
            bool verbatim = csharp && start > 0 && line[start - 1] == '@';
            for (int c = start + 1; c < line.Length; c++) {
                char ch = line[c];
                if (!verbatim && (csharp ? ch == '\\' : quote == '"' && ch == '`')) {
                    c++;
                } else if (ch == quote) {
                    if (Next(line, c) == quote) {
                        c++;
                    } else {
                        return c;
                    }
                }
            }
            return line.Length;
        }

        private static char Next(string line, int c) => c + 1 < line.Length ? line[c + 1] : '\0';

        private static string Indent(string line) => line.Substring(0, line.Length - line.TrimStart().Length);
    }
}
