using System;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    /// <summary>Hanging list markers and quote bars; every nested block measures at the reduced width.</summary>
    internal sealed class MarkdownPrefix : IWidget {
        private readonly IWidget _content;
        private readonly Func<IGlyphSet, string> _marker;
        private readonly bool _everyLine;
        private readonly ThemeToken _role;

        internal MarkdownPrefix(IWidget content, string? marker)
            : this(content, marker == null ? (Func<IGlyphSet, string>)Bar : _ => marker, marker == null, ThemeToken.Muted) { }

        /// <param name="content">The prefixed blocks.</param>
        /// <param name="marker">The marker for the destination glyph tier.</param>
        /// <param name="everyLine">Repeats the marker on every row (a bar) instead of only the first (a list marker).</param>
        /// <param name="role">The marker colour.</param>
        internal MarkdownPrefix(IWidget content, Func<IGlyphSet, string> marker, bool everyLine, ThemeToken role) {
            _content = content ?? throw new ArgumentNullException(nameof(content));
            _marker = marker ?? throw new ArgumentNullException(nameof(marker));
            _everyLine = everyLine;
            _role = role;
        }

        internal static string Bar(IGlyphSet glyphs) => glyphs.Tier == GlyphTier.Ascii ? "| " : "│ ";

        private int Indent(int width, RenderContext context) => Math.Min(Math.Max(0, width - 1), DisplayWidth.Measure(_marker(context.Glyphs)));

        public Size Measure(Constraints constraints, RenderContext context) {
            int indent = Indent(constraints.MaxWidth, context);
            Size size = _content.Measure(new Constraints(Math.Max(0, constraints.MaxWidth - indent), constraints.MaxHeight), context);
            return new Size(Math.Min(constraints.MaxWidth, size.Width + indent), Math.Max(1, size.Height));
        }

        public void Render(Region region, RenderContext context) {
            int indent = Indent(region.Width, context);
            _content.Render(region.Sub(indent, 0, region.Width - indent, region.Height), context);
            string marker = _marker(context.Glyphs);
            int count = _everyLine ? region.Height : Math.Min(1, region.Height);
            for (int y = 0; y < count; y++) {
                region.Write(0, y, marker, context.Style(_role), indent);
            }
        }
    }
}
