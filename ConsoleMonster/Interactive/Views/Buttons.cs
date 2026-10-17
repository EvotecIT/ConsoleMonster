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
            bool brackets = g.BracketFocus || !context.HasColor;
            List<TextSpan> spans = new List<TextSpan>();
            for (int i = 0; i < captions.Count; i++) {
                if (i > 0) {
                    spans.Add(new TextSpan(" ", Style.Plain));
                }
                bool focused = active && i == focus;
                bool dimmed = i == blocked;
                if (focused) {
                    Style style;
                    if (!context.HasColor) {
                        style = dimmed ? Style.Plain : Style.Plain.With(TextDecoration.Bold);
                    } else if (dimmed) {
                        style = context.Style(focusToken ?? ThemeToken.Focus).WithForeground(context.Style(ThemeToken.Muted).Foreground);
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
    }
}
