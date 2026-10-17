using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Interactive {
    /// <summary>
    /// The help footer, generated from the active key bindings: <c>↑↓ move  space toggle  enter confirm  esc cancel</c>.
    /// Bindings are shown by priority while they fit; when some are dropped, the key-map binding stays at the end so the
    /// rest is always one key away. A toast replaces the hints for a moment.
    /// </summary>
    internal sealed class KeyFooter : IWidget {
        internal const int HelpOrder = 1000;
        private readonly IReadOnlyList<KeyBinding> _bindings;

        public KeyFooter(IReadOnlyList<KeyBinding> bindings) {
            _bindings = bindings ?? throw new ArgumentNullException(nameof(bindings));
        }

        /// <summary>Draws on the surface bar style (shell footers) instead of plain text.</summary>
        public bool OnSurface { get; set; }

        /// <summary>A toast shown instead of the hints.</summary>
        public Toast? Toast { get; set; }

        /// <summary>Text drawn right-aligned (counts, positions).</summary>
        public StyledLine? Right { get; set; }

        public int Indent { get; set; }

        public Size Measure(Constraints constraints, RenderContext context) => new Size(constraints.MaxWidth, Math.Min(1, constraints.MaxHeight));

        public void Render(Region region, RenderContext context) {
            if (region.Width <= 0 || region.Height <= 0) {
                return;
            }
            Style bar = OnSurface ? context.Style(ThemeToken.Surface) : Style.Plain;
            if (OnSurface) {
                region.Fill(0, 0, region.Width, 1, " ", bar);
            }
            int rightWidth = Right?.Width ?? 0;
            int available = region.Width - Indent - (rightWidth > 0 ? rightWidth + 2 : 0) - 1;
            if (Toast != null) {
                StyledLine toast = Toast.Line(context, bar);
                region.Write(Indent, 0, TextWrapper.Truncate(toast, Math.Max(1, available), context.Ellipsis));
            } else {
                region.Write(Indent, 0, Build(context, bar, available));
            }
            if (Right != null && rightWidth > 0 && region.Width - 1 - rightWidth > Indent) {
                region.Write(region.Width - 1 - rightWidth, 0, Right.WithBaseStyle(bar));
            }
        }

        private StyledLine Build(RenderContext context, Style bar, int available) {
            List<KeyBinding> shown = new List<KeyBinding>();
            KeyBinding? help = null;
            foreach (KeyBinding b in _bindings) {
                if (!b.InFooter || string.IsNullOrEmpty(b.Label)) {
                    continue;
                }
                if (b.Order >= HelpOrder) {
                    help = b;
                    continue;
                }
                shown.Add(b);
            }
            shown.Sort((a, b) => a.Order.CompareTo(b.Order));
            Style key = OnSurface ? context.Style(ThemeToken.SurfaceAccent) : bar.Combine(context.Style(ThemeToken.Highlight));
            Style label = OnSurface ? bar : context.Style(ThemeToken.Muted);
            List<TextSpan> spans = new List<TextSpan>();
            int used = 0;
            int helpWidth = help == null ? 0 : Width(help) + 2;
            bool dropped = false;
            foreach (KeyBinding b in shown) {
                int w = Width(b) + (spans.Count > 0 ? 2 : 0);
                if (used + w + (help != null ? helpWidth : 0) > available) {
                    dropped = true;
                    break;
                }
                if (spans.Count > 0) {
                    spans.Add(new TextSpan("  ", label));
                }
                spans.Add(new TextSpan(b.DisplayKeys(), key));
                spans.Add(new TextSpan(" " + b.Label, label));
                used += w;
            }
            if (help != null && (dropped || used + helpWidth <= available)) {
                if (spans.Count > 0) {
                    spans.Add(new TextSpan("  ", label));
                }
                spans.Add(new TextSpan(help.DisplayKeys(), key));
                spans.Add(new TextSpan(" " + help.Label, label));
            }
            return new StyledLine(spans);
        }

        private static int Width(KeyBinding b) => DisplayWidth.Measure(b.DisplayKeys()) + 1 + DisplayWidth.Measure(b.Label);

        public static bool Dispatch(IReadOnlyList<KeyBinding> bindings, Input.InputEvent input) {
            foreach (KeyBinding b in bindings) {
                if (b.Chords.Count > 0 && b.Matches(input)) {
                    b.Action();
                    return true;
                }
            }
            return false;
        }
    }
}
