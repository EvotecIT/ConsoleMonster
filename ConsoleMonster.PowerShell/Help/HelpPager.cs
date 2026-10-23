using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Threading;
using ConsoleMonster.Canvas;
using ConsoleMonster.Input;
using ConsoleMonster.Layout;
using ConsoleMonster.Text;

namespace ConsoleMonster.PowerShell {
    /// <summary>Pages the existing help view through the shared renderer and exclusive console input lease.</summary>
    internal static class HelpPager {
        internal static bool Write(PSCmdlet cmdlet, IWidget view, bool paging) {
            if (!paging || Console.IsInputRedirected) {
                PSFormatting.Write(cmdlet, view, 0);
                return true;
            }
            Capabilities.TerminalProfile profile = Terminal.GetCurrentProfile();
            int pageSize = Math.Max(1, profile.Height - 3);
            // One immutable render snapshot gives back navigation the same content and an exact page count.
            List<StyledLine> lines = new List<StyledLine>(Lines(view, profile));
            int pageCount = Math.Max(1, (lines.Count + pageSize - 1) / pageSize);
            int page = 0;
            while (true) {
                int start = page * pageSize;
                Terminal.WriteLines(lines.GetRange(start, Math.Min(pageSize, lines.Count - start)), profile: profile);
                if (page == pageCount - 1) { break; }
                int direction = NextPage(cmdlet, profile, page + 1, pageCount);
                if (direction == 0) { return false; }
                page = Math.Max(0, page + direction);
            }
            Terminal.WriteLines(new[] { StyledLine.From(string.Empty) }, profile: profile);
            return true;
        }

        private static int NextPage(PSCmdlet cmdlet, Capabilities.TerminalProfile profile, int page, int count) {
            // One footer only; erase it before the next page (or return to the prompt).
            RenderContext context = Terminal.CreateContext(profile);
            string hint = string.Format(profile.Culture, HelpMessages.Get("PagerHint", profile.Language), page, count);
            Terminal.WriteLines(new[] { StyledLine.From(new string(context.Glyphs.Get(Glyphs.Glyph.RuleLine)[0], Math.Max(0, profile.Width - 1)), context.Style(Styling.ThemeToken.Muted)) }, profile: profile);
            Terminal.WriteLines(new[] { context.Markup(hint).Lines[0].WithBaseStyle(context.Style(Styling.ThemeToken.Muted)) }, false, profile);
            try {
                using (InputLease input = ConsoleInput.Acquire()) {
                    while (!cmdlet.Stopping) {
                        InputEvent? key = input.Source.Read(TimeSpan.FromMilliseconds(100), CancellationToken.None);
                        if (key == null) { continue; }
                        if (key.Key == ConsoleKey.C && (key.Modifiers & KeyModifiers.Control) != 0) { throw new PipelineStoppedException(); }
                        if (key.Key == ConsoleKey.Q || key.Key == ConsoleKey.Escape) { return 0; }
                        if (key.Key == ConsoleKey.B || key.Char == 'b' || key.Char == 'B') { return -1; }
                        if (key.Key == ConsoleKey.Spacebar || key.Char == ' ') { return 1; }
                    }
                    throw new PipelineStoppedException();
                }
            } finally {
                Console.Write("\r" + new string(' ', Markup.Strip(hint).Length) + "\r");
            }
        }

        private static IEnumerable<StyledLine> Lines(IWidget view, Capabilities.TerminalProfile profile) {
            foreach (CellBuffer block in Terminal.RenderChunks(view, profile.Width, Terminal.CreateContext(profile))) {
                for (int y = 0; y < block.Height; y++) {
                    List<TextSpan> spans = new List<TextSpan>();
                    int end = block.Width;
                    while (end > 0 && block[end - 1, y].Glyph == " ") { end--; }
                    for (int x = 0; x < end; x++) {
                        Cell cell = block[x, y];
                        if (!cell.IsContinuation) { spans.Add(new TextSpan(cell.Glyph, cell.Style)); }
                    }
                    yield return new StyledLine(spans);
                }
            }
        }
    }
}
