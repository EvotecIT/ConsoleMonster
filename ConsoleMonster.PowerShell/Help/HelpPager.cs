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
            int pageSize = Math.Max(1, profile.Height - 2);
            List<StyledLine> page = new List<StyledLine>();
            using (IEnumerator<StyledLine> lines = Lines(view, profile).GetEnumerator()) {
                while (lines.MoveNext()) {
                    page.Add(lines.Current);
                    if (page.Count <= pageSize) { continue; }
                    StyledLine next = page[pageSize];
                    page.RemoveAt(pageSize);
                    Terminal.WriteLines(page, profile: profile);
                    page.Clear();
                    if (!NextPage(cmdlet, profile)) { return false; }
                    page.Add(next);
                }
            }
            Terminal.WriteLines(page, profile: profile);
            Terminal.WriteLines(new[] { StyledLine.From(string.Empty) }, profile: profile);
            return true;
        }

        private static bool NextPage(PSCmdlet cmdlet, Capabilities.TerminalProfile profile) {
            // One footer only; erase it before the next page (or return to the prompt).
            string hint = HelpMessages.Get("PagerHint", profile.Language);
            Terminal.WriteLines(new[] { StyledLine.From(hint, Terminal.CreateContext(profile).Style(Styling.ThemeToken.Muted)) }, false, profile);
            try {
                using (InputLease input = ConsoleInput.Acquire()) {
                    while (!cmdlet.Stopping) {
                        InputEvent? key = input.Source.Read(TimeSpan.FromMilliseconds(100), CancellationToken.None);
                        if (key == null) { continue; }
                        if (key.Key == ConsoleKey.C && (key.Modifiers & KeyModifiers.Control) != 0) { throw new PipelineStoppedException(); }
                        if (key.Key == ConsoleKey.Q || key.Key == ConsoleKey.Escape) { return false; }
                        if (key.Key == ConsoleKey.Spacebar || key.Char == ' ') { return true; }
                    }
                    throw new PipelineStoppedException();
                }
            } finally {
                Console.Write("\r" + new string(' ', hint.Length) + "\r");
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
