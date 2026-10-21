using System;
using System.Collections.Generic;
using System.Text;
using ConsoleMonster.Canvas;
using ConsoleMonster.Highlighting;

namespace ConsoleMonster.Rendering {
    /// <summary>Finds visible URL/path tokens in literal log and markup rows using the existing smart tokenizer.</summary>
    internal static class HyperlinkText {
        private static readonly SmartHighlighter Tokens = new SmartHighlighter();

        /// <summary>Maps terminal columns to targets, so wide glyphs and partial frame updates retain the correct link.</summary>
        internal static string?[] Find(CellBuffer buffer, int row) {
            StringBuilder text = new StringBuilder();
            List<int> columns = new List<int>();
            for (int x = 0; x < buffer.Width; x++) {
                Cell cell = buffer[x, row];
                if (cell.IsContinuation) {
                    continue;
                }
                // Table borders delimit values, just like spaces; they must never become part of a file or URL target.
                string glyph = cell.Glyph == "│" || cell.Glyph == "┃" || cell.Glyph == "║" ? " " : cell.Glyph;
                text.Append(glyph);
                for (int i = 0; i < glyph.Length; i++) {
                    columns.Add(x);
                }
            }
            string source = text.ToString();
            string?[] targets = new string?[buffer.Width];
            foreach (HighlightToken token in Tokens.Tokenize(source)) {
                if (token.Role != HighlightRole.Link && token.Role != HighlightRole.Identifier) {
                    continue;
                }
                string? target = HyperlinkTarget.From(token.Link ?? source.Substring(token.Start, token.Length));
                if (target == null) {
                    continue;
                }
                for (int i = token.Start; i < token.Start + token.Length; i++) {
                    targets[columns[i]] = target;
                }
            }
            return targets;
        }
    }
}
