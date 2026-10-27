using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    /// <summary>The tone, emphasis and optional literal marker of one table row.</summary>
    public sealed class TableRowRole {
        /// <summary>An optional theme role for every cell in the row.</summary>
        public ThemeToken? Tone { get; set; }

        /// <summary>Mute every cell; an explicit <see cref="Tone"/> takes precedence.</summary>
        public bool Muted { get; set; }

        /// <summary>Apply bold emphasis to every cell.</summary>
        public bool Bold { get; set; }

        /// <summary>A plain text marker for <see cref="RoleTable"/>'s dedicated marker column.</summary>
        public string? Marker { get; set; }

        /// <summary>The ASCII marker; null uses <see cref="Marker"/>.</summary>
        public string? AsciiMarker { get; set; }

        internal Style Style(RenderContext context) {
            ThemeToken? tone = Tone ?? (Muted ? ThemeToken.Muted : (ThemeToken?)null);
            Style style = tone.HasValue ? context.Style(tone.Value) : Styling.Style.Plain;
            return Bold ? style.With(TextDecoration.Bold) : style;
        }

        internal StyledLine Apply(StyledLine line, RenderContext context) {
            List<TextSpan> spans = new List<TextSpan>();
            Style style = Style(context);
            foreach (TextSpan span in line) spans.Add(new TextSpan(span.Text, span.Style.Combine(style)));
            return new StyledLine(spans);
        }
    }
}
