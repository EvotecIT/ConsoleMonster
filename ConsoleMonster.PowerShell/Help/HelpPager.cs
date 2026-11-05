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
            // A rich terminal gets the document viewer; anything else pages screen by screen as before.
            if (Show(cmdlet, view, CancellationToken.None)) { return true; }
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

        /// <summary>
        /// Opens help in the document viewer when the terminal is rich and the help is longer than the screen. Related
        /// command names open their own help there, with back and forward history.
        /// </summary>
        internal static bool Show(PSCmdlet cmdlet, IWidget view, CancellationToken cancellationToken, int? width = null) {
            string? title = view is Widgets.HelpView help ? help.Topic.Name : null;
            return Interactive.WidgetPager.TryShow(view, string.IsNullOrEmpty(title) ? null : title, PSInteractive.Options(cmdlet, null), cancellationToken, width, Opener(cmdlet));
        }

        // Resolves a related link's command or about topic through the caller's runspace, on the pipeline thread the viewer runs on.
        private static Func<string, IWidget?> Opener(PSCmdlet cmdlet) => name =>
            ConsoleHostHelp.TryCreate(name, cmdlet.InvokeCommand, out IWidget? widget, out ErrorRecord? _) ? widget : null;

        private static int NextPage(PSCmdlet cmdlet, Capabilities.TerminalProfile profile, int page, int count) {
            // One footer only; erase it before the next page (or return to the prompt).
            RenderContext context = Terminal.CreateContext(profile);
            string hint = string.Format(profile.Culture, HelpMessages.Get("PagerHint", profile.Language), page, count);
            Terminal.WriteLines(new[] { StyledLine.From(new string(context.Glyphs.Get(Glyphs.Glyph.RuleLine)[0], Math.Max(0, profile.Width - 1)), context.Style(Styling.ThemeToken.Muted)) }, profile: profile);
            Terminal.WriteLines(new[] { context.Markup(hint).Lines[0].WithBaseStyle(context.Style(Styling.ThemeToken.Muted)) }, false, profile);
            try {
                using (InputLease input = ConsoleInput.Acquire()) {
                    Interactive.InteractiveOptions options = Interactive.InteractiveOptions.Default;
                    bool pointer = options.ResolveMouse();
                    using Interactive.MouseReporting? mouse = Interactive.MouseReporting.Open(pointer, options, profile, input.Source);
                    while (!cmdlet.Stopping) {
                        InputEvent? key = input.Source.Read(TimeSpan.FromMilliseconds(100), CancellationToken.None);
                        if (key == null) { continue; }
                        if (key.Kind == InputEventKind.Mouse) {
                            // The wheel pages like Space and b; a click goes on like Space.
                            // Native Windows input reports the mouse even with reporting off, so the opt-out is checked here too.
                            int? step = pointer ? PointerStep(key) : null;
                            if (step.HasValue) { return step.Value; }
                            continue;
                        }
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

        internal static int? PointerStep(InputEvent pointer) {
            if (pointer.MouseAction == MouseAction.Wheel && pointer.WheelDelta != 0) { return pointer.WheelDelta < 0 ? 1 : -1; }
            if (pointer.MouseAction == MouseAction.Down && pointer.MouseButton == 1) { return 1; }
            return null;
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
