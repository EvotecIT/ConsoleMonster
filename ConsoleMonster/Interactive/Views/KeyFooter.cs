using System;
using System.Collections.Generic;
using System.Linq;
using ConsoleMonster.Canvas;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;

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
        public Toast? Toast { get; set; }

        public StyledLine? Right { get; set; }

        public int Indent { get; set; } = 1;

        public Size Measure(Constraints constraints, RenderContext context) => new Size(constraints.MaxWidth, Math.Min(1, constraints.MaxHeight));

        // The footer's look lives in FooterBar (shared with dashboards and runs); this only picks the hints to show.
        public void Render(Region region, RenderContext context) {
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
            FooterBar bar = new FooterBar { Indent = Indent, Right = Right, KeepLast = help != null };
            foreach (KeyBinding b in shown.OrderBy(binding => binding.Order)) {
                bar.Hints.Add(new KeyHint(b.DisplayKeys(), b.Label));
            }
            if (help != null) {
                bar.Hints.Add(new KeyHint(help.DisplayKeys(), help.Label));
            }
            if (Toast != null) {
                bar.Message = Toast.Line(context, Style.Plain);
            }
            bar.Render(region, context);
        }
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
