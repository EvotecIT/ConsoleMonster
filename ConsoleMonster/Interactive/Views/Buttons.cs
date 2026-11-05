using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Interactive {
    /// <summary>
    /// A row of buttons: the focused one on the focus background (in brackets where colour is unreliable). A blocked button
    /// (Run while problems remain) is drawn dimmed, also when focused, so it reads as unavailable but stays reachable.
    /// </summary>
    internal static class Buttons {
        public static List<TextSpan> Spans(IReadOnlyList<string> captions, int focus, RenderContext context, bool active = true, ThemeToken? focusToken = null, int blocked = -1) {
            InteractiveGlyphs g = InteractiveGlyphs.For(context);
            bool brackets = g.BracketFocus || (!context.HasColor || context.Profile.IsAccessible);
            List<TextSpan> spans = new List<TextSpan>();
            for (int i = 0; i < captions.Count; i++) {
                if (i > 0) {
                    spans.Add(new TextSpan(" ", Style.Plain));
                }
                bool focused = active && i == focus;
                bool dimmed = i == blocked;
                if (focused) {
                    Style style;
                    if ((!context.HasColor || context.Profile.IsAccessible)) {
                        style = dimmed ? Style.Plain : Style.Plain.With(TextDecoration.Bold);
                    } else if (dimmed) {
                        style = context.Style(focusToken ?? ThemeToken.Focus).WithColor(context.Style(ThemeToken.Muted).Color);
                    } else {
                        style = context.Style(focusToken ?? ThemeToken.Focus).With(TextDecoration.Bold);
                    }
                    spans.Add(new TextSpan(brackets ? "[" + captions[i] + "]" : " " + captions[i] + " ", style));
                } else {
                    Style style = dimmed ? context.Style(ThemeToken.Muted).With(TextDecoration.Dim) : context.Style(i == focus ? ThemeToken.Text : ThemeToken.Muted);
                    spans.Add(new TextSpan(" " + captions[i] + " ", style));
                }
            }
            return spans;
        }

        /// <summary>
        /// Makes a row drawn by <see cref="Spans"/> clickable: <paramref name="x"/> is where it starts in
        /// <paramref name="row"/>, and a click on a caption runs <paramref name="click"/> with its index, as its key does.
        /// </summary>
        public static void Record(Canvas.Rect row, int x, IReadOnlyList<string> captions, System.Action<int> click, string id) {
            for (int i = 0; i < captions.Count; i++) {
                int width = DisplayWidth.Measure(captions[i]) + 2;
                int index = i;
                PointerTargets.RecordAction(new Canvas.Rect(row.X + x, row.Y, width, 1), () => click(index), id + ":" + i.ToString(System.Globalization.CultureInfo.InvariantCulture));
                x += width + 1;
            }
        }
    }

    /// <summary>A row of buttons starting at its left edge; a click on a caption runs it, as its key does.</summary>
    internal sealed class ButtonRow : Layout.IWidget {
        private readonly IReadOnlyList<string> _captions;
        private readonly System.Func<int> _focus;
        private readonly System.Action<int> _click;
        private readonly string _id;

        internal ButtonRow(IReadOnlyList<string> captions, System.Func<int> focus, System.Action<int> click, string id) {
            _captions = captions; _focus = focus; _click = click; _id = id;
        }

        public Layout.Size Measure(Layout.Constraints constraints, RenderContext context) => new Layout.Size(constraints.MaxWidth, System.Math.Min(1, constraints.MaxHeight));

        public void Render(Region region, RenderContext context) {
            if (region.Width <= 0 || region.Height <= 0) { return; }
            StyledLine line = new StyledLine(Buttons.Spans(_captions, _focus(), context));
            region.Write(0, 0, line, region.Width);
            if (line.Width <= region.Width) { Buttons.Record(new Canvas.Rect(region.Bounds.X, region.Bounds.Y, region.Width, 1), 0, _captions, _click, _id); }
        }
    }
}