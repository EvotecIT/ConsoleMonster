using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Interactive {
    /// <summary>A row of buttons: the focused one on the focus background (in brackets where colour is unreliable).</summary>
    internal static class Buttons {
        public static List<TextSpan> Spans(IReadOnlyList<string> captions, int focus, RenderContext context, bool active = true, ThemeToken? focusToken = null) {
            InteractiveGlyphs g = InteractiveGlyphs.For(context);
            bool brackets = g.BracketFocus || !context.HasColor;
            List<TextSpan> spans = new List<TextSpan>();
            for (int i = 0; i < captions.Count; i++) {
                if (i > 0) {
                    spans.Add(new TextSpan(" ", Style.Plain));
                }
                bool focused = active && i == focus;
                if (focused) {
                    Style style = context.HasColor ? context.Style(focusToken ?? ThemeToken.Focus).With(TextDecoration.Bold) : Style.Plain.With(TextDecoration.Bold);
                    spans.Add(new TextSpan(brackets ? "[" + captions[i] + "]" : " " + captions[i] + " ", style));
                } else {
                    spans.Add(new TextSpan(" " + captions[i] + " ", context.Style(i == focus ? ThemeToken.Text : ThemeToken.Muted)));
                }
            }
            return spans;
        }
    }
}
