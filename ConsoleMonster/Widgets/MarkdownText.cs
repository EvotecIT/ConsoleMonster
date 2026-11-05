using System;
using System.Collections.Generic;
using System.Text;
using ConsoleMonster.Canvas;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    /// <summary>Resolves inline links against the output capability before the ordinary rich-text wrapping pass.</summary>
    internal sealed class MarkdownText : IWidget {
        private readonly RichText _text;
        private readonly RichStyle _style;

        internal MarkdownText(RichText text, RichStyle style = default) {
            _text = text ?? throw new ArgumentNullException(nameof(text));
            _style = style;
        }

        internal static RichText Resolve(RichText text, RenderContext context) {
            // A single link can contain several styled segments (for example a bold word in its label).
            // Append the destination once after the complete run, rather than once per styled word.
            List<RichSegment> segments = new List<RichSegment>();
            StringBuilder label = new StringBuilder();
            int runStart = 0;
            for (int i = 0; i < text.Segments.Count; i++) {
                RichSegment segment = text.Segments[i];
                string? link = segment.Style.Link;
                if (i == 0 || text.Segments[i - 1].Style.Link != link) runStart = segments.Count;
                // A heading anchor inside the document viewer stays the viewer's to follow; everything else follows the hyperlink rules.
                string? target = Interactive.DocumentLinks.Internal(link) ?? (context.Profile.SupportsHyperlinks ? Rendering.HyperlinkTarget.From(link) : null);
                segments.Add(new RichSegment(segment.Text, segment.Style.WithLink(target), segment.IsMarkup));
                if (link != null) {
                    label.Append(segment.IsMarkup ? Markup.Strip(segment.Text) : segment.Text);
                }
                if (link != null && (i + 1 == text.Segments.Count || text.Segments[i + 1].Style.Link != link)) {
                    string shown = label.ToString();
                    string? printed = Rendering.LinkTargets.PrintedLabel(shown, link, context.Glyphs.Tier);
                    if (printed != null) {
                        RichStyle style = segments[runStart].Style;
                        segments.RemoveRange(runStart, segments.Count - runStart);
                        segments.Add(new RichSegment(printed, style, false));
                    }
                    string? suffix = target == null ? Rendering.LinkTargets.Suffix(shown, link, context.Glyphs.Tier) : null;
                    if (suffix != null) {
                        segments.Add(new RichSegment(suffix, new RichStyle(ThemeToken.Muted), false));
                    }
                    label.Clear();
                }
            }
            return new RichText(segments);
        }

        private RichText Content(RenderContext context) => Resolve(_text.WithBaseStyle(_style), context);

        public Size Measure(Constraints constraints, RenderContext context) => Content(context).Measure(constraints, context);

        public void Render(Region region, RenderContext context) => Content(context).Render(region, context);
    }
}
