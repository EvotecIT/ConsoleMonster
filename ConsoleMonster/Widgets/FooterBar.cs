using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    /// <summary>One key hint in a footer: the keys ("ctrl+c", "↑↓") and what they do ("stop", "move").</summary>
    public readonly struct KeyHint {
        /// <summary>Creates a hint.</summary>
        /// <param name="keys">The keys, lower case ("ctrl+c").</param>
        /// <param name="label">What they do ("stop").</param>
        public KeyHint(string keys, string label) {
            Keys = keys ?? string.Empty;
            Label = label ?? string.Empty;
        }

        /// <summary>The keys.</summary>
        public string Keys { get; }

        /// <summary>What they do.</summary>
        public string Label { get; }
    }

    /// <summary>
    /// The one key-hint footer of every surface (dashboards, menus, forms, prompts, runs, inline areas): a row starting at
    /// column 1 with keys in Accent and labels in Muted, two spaces between hints, optional text on the right (Muted), and a
    /// toast or message replacing the hints while it shows. No band, so it reads the same full screen and inline. Hints
    /// that do not fit are dropped from the end, keeping the last (help) hint when asked. See Docs/Design-System.md.
    /// </summary>
    public sealed class FooterBar : IWidget {
        /// <summary>Creates a footer.</summary>
        /// <param name="hints">The hints, in order.</param>
        public FooterBar(IEnumerable<KeyHint>? hints = null) {
            if (hints != null) {
                Hints.AddRange(hints);
            }
        }

        /// <summary>The hints, in order.</summary>
        public List<KeyHint> Hints { get; } = new List<KeyHint>();

        /// <summary>Keeps the last hint (the key map) when others have to be dropped.</summary>
        public bool KeepLast { get; set; }

        /// <summary>A line shown instead of the hints (a toast), or null.</summary>
        public StyledLine? Message { get; set; }

        /// <summary>Text on the right, or null.</summary>
        public StyledLine? Right { get; set; }

        /// <summary>The first column (default 1).</summary>
        public int Indent { get; set; } = 1;

        /// <inheritdoc />
        public Size Measure(Constraints constraints, RenderContext context) => new Size(constraints.MaxWidth, Math.Min(1, constraints.MaxHeight));

        /// <inheritdoc />
        public void Render(Region region, RenderContext context) {
            if (region.Width <= 0 || region.Height <= 0) {
                return;
            }
            StyledLine? right = Right == null ? null : Right.WithBaseStyle(context.Style(ThemeToken.Muted));
            int rightWidth = right?.Width ?? 0;
            int available = region.Width - Indent - (rightWidth > 0 ? rightWidth + 2 : 0) - 1;
            StyledLine left = Message != null ? TextWrapper.Truncate(Message, Math.Max(1, available), context.Ellipsis) : Line(Hints, context, available, KeepLast);
            region.Write(Indent, 0, left);
            if (right != null && rightWidth > 0 && region.Width - 1 - rightWidth > Indent + left.Width + 1) {
                region.Write(region.Width - 1 - rightWidth, 0, right);
            }
        }

        /// <summary>The hints as one styled line no wider than <paramref name="available"/> cells.</summary>
        /// <param name="hints">The hints.</param>
        /// <param name="context">The render context.</param>
        /// <param name="available">The width.</param>
        /// <param name="keepLast">Keeps the last hint when others are dropped.</param>
        /// <returns>The line.</returns>
        public static StyledLine Line(IReadOnlyList<KeyHint> hints, RenderContext context, int available, bool keepLast = false) {
            Style key = context.Style(ThemeToken.Accent);
            Style label = context.Style(ThemeToken.Muted);
            List<TextSpan> spans = new List<TextSpan>();
            int count = hints.Count;
            KeyHint? last = keepLast && count > 0 ? hints[count - 1] : (KeyHint?)null;
            int lastWidth = last.HasValue ? Width(last.Value) + 2 : 0;
            int used = 0;
            bool dropped = false;
            for (int i = 0; i < (last.HasValue ? count - 1 : count); i++) {
                int w = Width(hints[i]) + (spans.Count > 0 ? 2 : 0);
                if (used + w + lastWidth > available) {
                    dropped = true;
                    break;
                }
                Append(spans, hints[i], key, label);
                used += w;
            }
            if (last.HasValue && (dropped || used + lastWidth <= available)) {
                Append(spans, last.Value, key, label);
            }
            return new StyledLine(spans);
        }

        private static void Append(List<TextSpan> spans, KeyHint hint, Style key, Style label) {
            if (spans.Count > 0) {
                spans.Add(new TextSpan("  ", label));
            }
            spans.Add(new TextSpan(hint.Keys, key));
            if (hint.Label.Length > 0) {
                spans.Add(new TextSpan(" " + hint.Label, label));
            }
        }

        private static int Width(KeyHint hint) => DisplayWidth.Measure(hint.Keys) + (hint.Label.Length > 0 ? 1 + DisplayWidth.Measure(hint.Label) : 0);
    }
}
