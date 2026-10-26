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
        /// <param name="available">Whether the command can run in the current context.</param>
        public KeyHint(string keys, string label, bool available = true) {
            Keys = keys ?? string.Empty;
            Label = label ?? string.Empty;
            Available = available;
        }

        /// <summary>The keys.</summary>
        public string Keys { get; }

        /// <summary>What they do.</summary>
        public string Label { get; }

        /// <summary>Whether this command can run in the current context. Unavailable hints remain visible and dimmed.</summary>
        public bool Available { get; }
    }

    /// <summary>One segment on the right of a footer: text with an optional link (OSC 8 where the terminal supports it) and tone.</summary>
    public readonly struct FooterSegment {
        /// <summary>Creates a segment.</summary>
        /// <param name="text">The text.</param>
        /// <param name="link">A link target (http, https, mailto, ftp), or null.</param>
        /// <param name="role">The tone, or null for muted text.</param>
        public FooterSegment(string text, string? link = null, ThemeToken? role = null) {
            Text = text ?? string.Empty;
            Link = link;
            Role = role;
        }

        /// <summary>The text.</summary>
        public string Text { get; }

        /// <summary>The link target, or null.</summary>
        public string? Link { get; }

        /// <summary>The tone, or null for muted text.</summary>
        public ThemeToken? Role { get; }
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

        /// <summary>Reads current registry hints at render time, for example from the focused panel or active dialog.</summary>
        public Func<IEnumerable<KeyHint>>? HintsSource { get; set; }

        /// <summary>Keeps every registry entry in a fixed slot, shortening labels rather than hiding unavailable or trailing commands.</summary>
        public bool FixedSlots { get; set; }

        /// <summary>Keeps the last hint (the key map) when others have to be dropped.</summary>
        public bool KeepLast { get; set; }

        /// <summary>A line shown instead of the hints (a toast), or null.</summary>
        public StyledLine? Message { get; set; }

        /// <summary>Text on the right, or null.</summary>
        public StyledLine? Right { get; set; }

        /// <summary>Segments on the right (after <see cref="Right"/>), separated by a dot: a run id, a report link, a status.</summary>
        public List<FooterSegment> Segments { get; } = new List<FooterSegment>();

        /// <summary>The first column (default 1).</summary>
        public int Indent { get; set; } = 1;

        /// <inheritdoc />
        public Size Measure(Constraints constraints, RenderContext context) => new Size(constraints.MaxWidth, Math.Min(1, constraints.MaxHeight));

        /// <inheritdoc />
        public void Render(Region region, RenderContext context) {
            if (region.Width <= 0 || region.Height <= 0) {
                return;
            }
            // Segments give way from the end, then Right itself, so the hints always keep their room.
            int keep = Segments.Count;
            StyledLine? right = RightLine(context, keep);
            while (right != null && keep > 0 && region.Width - 1 - right.Width <= Indent + 20) {
                right = RightLine(context, --keep);
            }
            int rightWidth = right?.Width ?? 0;
            int available = region.Width - Indent - (rightWidth > 0 ? rightWidth + 2 : 0) - 1;
            IReadOnlyList<KeyHint> hints = HintsSource == null ? Hints : new List<KeyHint>(HintsSource());
            StyledLine left = Message != null ? TextWrapper.Truncate(Message, Math.Max(1, available), context.Ellipsis)
                : FixedSlots ? SlotLine(hints, context, available) : Line(hints, context, available, KeepLast);
            region.Write(Indent, 0, left);
            if (right != null && rightWidth > 0 && region.Width - 1 - rightWidth > Indent + left.Width + 1) {
                region.Write(region.Width - 1 - rightWidth, 0, right);
            }
        }

        // Right and the segments as one muted line; a segment's link is underlined and carried as OSC 8.
        private StyledLine? RightLine(RenderContext context, int segments) {
            Style muted = context.Style(ThemeToken.Muted);
            List<TextSpan> spans = new List<TextSpan>();
            if (Right != null) {
                spans.AddRange(Right.WithBaseStyle(muted));
            }
            string dot = " " + context.Glyphs.Get(Glyphs.Glyph.Dot) + " ";
            for (int i = 0; i < segments && i < Segments.Count; i++) {
                FooterSegment segment = Segments[i];
                if (segment.Text.Length == 0) {
                    continue;
                }
                if (spans.Count > 0) {
                    spans.Add(new TextSpan(dot, muted));
                }
                Style style = segment.Role.HasValue ? context.Style(segment.Role.Value) : muted;
                if (!string.IsNullOrEmpty(segment.Link)) {
                    style = style.With(TextDecoration.Underline).WithLink(segment.Link!);
                }
                spans.Add(new TextSpan(segment.Text, style));
            }
            return spans.Count == 0 ? null : new StyledLine(spans);
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
            if (!hint.Available) { key = label.With(TextDecoration.Dim); label = key; }
            spans.Add(new TextSpan(hint.Keys, key));
            if (hint.Label.Length > 0) {
                spans.Add(new TextSpan(" " + hint.Label, label));
            }
        }

        private static int Width(KeyHint hint) => DisplayWidth.Measure(hint.Keys) + (hint.Label.Length > 0 ? 1 + DisplayWidth.Measure(hint.Label) : 0);

        private static StyledLine SlotLine(IReadOnlyList<KeyHint> hints, RenderContext context, int available) {
            var spans = new List<TextSpan>();
            if (hints.Count == 0 || available <= 0) { return StyledLine.Empty; }
            for (int i = 0; i < hints.Count; i++) {
                int width = available / hints.Count + (i < available % hints.Count ? 1 : 0);
                if (width <= 0) { break; }
                KeyHint hint = hints[i];
                Style key = context.Style(hint.Available ? ThemeToken.Accent : ThemeToken.Muted);
                Style label = context.Style(ThemeToken.Muted);
                if (!hint.Available) { key = key.With(TextDecoration.Dim); label = label.With(TextDecoration.Dim); }
                string keys = TextWrapper.Truncate(hint.Keys, width, string.Empty);
                spans.Add(new TextSpan(keys, key));
                int remaining = Math.Max(0, width - DisplayWidth.Measure(keys));
                string text = remaining > 1 ? TextWrapper.Truncate(" " + hint.Label, remaining - 1, context.Ellipsis) : string.Empty;
                spans.Add(new TextSpan(text.PadRight(remaining), label));
            }
            return new StyledLine(spans);
        }
    }
}
