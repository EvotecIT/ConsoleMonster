using System;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    /// <summary>Hanging list markers and quote bars; every nested block measures at the reduced width.</summary>
    internal sealed class MarkdownPrefix : IWidget {
        private readonly IWidget _content;
        private readonly string? _marker;

        internal MarkdownPrefix(IWidget content, string? marker) {
            _content = content ?? throw new ArgumentNullException(nameof(content));
            _marker = marker;
        }

        private string Marker(RenderContext context) => _marker ?? (context.Glyphs.Tier == GlyphTier.Ascii ? "| " : "│ ");

        private int Indent(int width, RenderContext context) => Math.Min(Math.Max(0, width - 1), DisplayWidth.Measure(Marker(context)));

        public Size Measure(Constraints constraints, RenderContext context) {
            int indent = Indent(constraints.MaxWidth, context);
            Size size = _content.Measure(new Constraints(Math.Max(0, constraints.MaxWidth - indent), constraints.MaxHeight), context);
            return new Size(Math.Min(constraints.MaxWidth, size.Width + indent), Math.Max(1, size.Height));
        }

        public void Render(Region region, RenderContext context) {
            int indent = Indent(region.Width, context);
            _content.Render(region.Sub(indent, 0, region.Width - indent, region.Height), context);
            int count = _marker == null ? region.Height : Math.Min(1, region.Height);
            for (int y = 0; y < count; y++) {
                region.Write(0, y, Marker(context), context.Style(ThemeToken.Muted), indent);
            }
        }
    }
}
