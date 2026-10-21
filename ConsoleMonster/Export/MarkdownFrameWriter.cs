namespace ConsoleMonster.Export {
    /// <summary>Keeps arbitrary rendered tables, trees and composed widgets intact in CommonMark code blocks.</summary>
    internal static class MarkdownFrameWriter {
        internal static string Write(string text) {
            int longest = 0;
            int run = 0;
            foreach (char c in text) {
                run = c == '`' ? run + 1 : 0;
                if (run > longest) {
                    longest = run;
                }
            }
            string fence = new string('`', System.Math.Max(3, longest + 1));
            return fence + "text\n" + text + (text.Length > 0 && !text.EndsWith("\n", System.StringComparison.Ordinal) ? "\n" : string.Empty) + fence + "\n";
        }
    }
}
