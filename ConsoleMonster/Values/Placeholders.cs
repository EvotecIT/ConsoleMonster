using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Values {
    /// <summary>
    /// ConsoleMonster's own placeholders in shaped text (a missing value, a cut list, a failed getter), kept apart from the
    /// caller's data until the glyph tier is known. Unicode noncharacters are reserved for a program's internal use, so they
    /// never collide with real text. Public strings resolve them for the Full tier; cells and text blocks resolve them where
    /// they are drawn.
    /// </summary>
    internal static class Placeholders {
        internal const string Missing = "﷐";
        internal const string Ellipsis = "﷑";
        internal const string Warning = "﷒";

        private static readonly char[] All = { '﷐', '﷑', '﷒' };

        internal static bool In(string? text) => text != null && text.IndexOfAny(All) >= 0;

        /// <summary>The text with each placeholder drawn for the tier: <c>— … ⚠</c> on Full, <c>!</c> for the warning on Basic, <c>- ... !</c> on ASCII.</summary>
        internal static string Resolve(string text, GlyphTier tier) => !In(text) ? text
            : text.Replace(Missing, tier == GlyphTier.Ascii ? "-" : "—")
                .Replace(Ellipsis, tier == GlyphTier.Ascii ? "..." : "…")
                .Replace(Warning, tier == GlyphTier.Full ? "⚠" : "!");

        /// <summary>A cell for shaped text: the text when it has no placeholder, otherwise a markup cell drawn for its tier.</summary>
        /// <param name="text">Shaped text that may hold placeholders.</param>
        /// <param name="style">An optional markup style for the whole cell, such as <c>muted</c>.</param>
        internal static object Cell(string text, string? style = null) {
            if (!In(text) && style == null) { return text; }
            string escaped = Text.Markup.Escape(text);
            return TableCell.WithPlaceholders(style == null ? escaped : "[" + style + "]" + escaped + "[/]");
        }

        /// <summary>A text block for shaped markup: an ordinary block when it has no placeholder, otherwise one drawn for its tier.</summary>
        internal static IWidget Block(string markup) => In(markup) ? new TierText(markup) : new TextBlock(markup);

        private sealed class TierText : IWidget {
            private readonly string _markup;
            internal TierText(string markup) { _markup = markup; }
            private TextBlock For(RenderContext context) => new TextBlock(Resolve(_markup, context.Glyphs.Tier));
            public Size Measure(Constraints constraints, RenderContext context) => For(context).Measure(constraints, context);
            public void Render(Region region, RenderContext context) => For(context).Render(region, context);
        }
    }
}
