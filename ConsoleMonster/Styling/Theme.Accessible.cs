namespace ConsoleMonster.Styling {
    public sealed partial class Theme {
        private static Theme BuildColorBlindSafe() {
            ThemeColor blue = ThemeColor.Hex("#56b4e9", 14, 74);
            ThemeColor orange = ThemeColor.Hex("#e69f00", 9, 172);
            ThemeColor yellow = ThemeColor.Hex("#f0e442", 11, 221);
            ThemeColor purple = ThemeColor.Hex("#cc79a7", 13, 175);
            ThemeColor teal = ThemeColor.Hex("#5ed5bf", 10, 79);
            ThemeColor foreground = ThemeColor.Hex("#eeeeee", 15, 255);
            ThemeColor muted = ThemeColor.Hex("#b5b5bd", 7, 250);
            return Dark.Rename("ColorBlindSafe")
                .With(ThemeToken.Text, new ThemeStyle(foreground))
                .With(ThemeToken.Muted, new ThemeStyle(muted))
                .With(ThemeToken.Pending, new ThemeStyle(muted))
                .With(ThemeToken.Skip, new ThemeStyle(muted))
                .With(ThemeToken.Border, new ThemeStyle(muted))
                .With(ThemeToken.Accent, new ThemeStyle(blue, null, TextDecoration.Bold))
                .With(ThemeToken.Accent2, new ThemeStyle(purple))
                .With(ThemeToken.Pass, new ThemeStyle(blue, null, TextDecoration.Bold))
                .With(ThemeToken.Fail, new ThemeStyle(orange, null, TextDecoration.Bold | TextDecoration.Underline))
                .With(ThemeToken.Warn, new ThemeStyle(yellow, null, TextDecoration.Bold))
                .With(ThemeToken.Info, new ThemeStyle(blue))
                .With(ThemeToken.Running, new ThemeStyle(teal))
                .With(ThemeToken.Title, new ThemeStyle(foreground, null, TextDecoration.Bold))
                .With(ThemeToken.Header, new ThemeStyle(foreground, null, TextDecoration.Bold | TextDecoration.Underline))
                .With(ThemeToken.Highlight, new ThemeStyle(yellow, null, TextDecoration.Bold))
                .With(ThemeToken.SurfaceAccent, new ThemeStyle(blue, Dark.Get(ThemeToken.SurfaceAccent).BackgroundColor, TextDecoration.Bold))
                .With(ThemeToken.Series1, new ThemeStyle(blue))
                .With(ThemeToken.Series2, new ThemeStyle(orange))
                .With(ThemeToken.Series3, new ThemeStyle(teal))
                .With(ThemeToken.Series4, new ThemeStyle(purple))
                .With(ThemeToken.Series5, new ThemeStyle(yellow))
                .With(ThemeToken.Series6, new ThemeStyle(foreground))
                .With(ThemeToken.Focus, new ThemeStyle(foreground, Dark.Get(ThemeToken.Focus).BackgroundColor, TextDecoration.Bold));
        }
    }
}
