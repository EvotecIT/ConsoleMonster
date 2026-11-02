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
            for (int i = 0; i < text.Segments.Count; i++) {
                RichSegment segment = text.Segments[i];
                string? link = segment.Style.Link;
                string? target = context.Profile.SupportsHyperlinks ? Rendering.HyperlinkTarget.From(link) : null;
                segments.Add(new RichSegment(segment.Text, segment.Style.WithLink(target), segment.IsMarkup));
                if (link != null) {
                    label.Append(segment.IsMarkup ? Markup.Strip(segment.Text) : segment.Text);
                }
                if (link != null && (i + 1 == text.Segments.Count || text.Segments[i + 1].Style.Link != link)) {
                    string? suffix = target == null ? Rendering.LinkTargets.Suffix(label.ToString(), link) : null;
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
