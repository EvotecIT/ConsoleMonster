namespace ConsoleMonster.Styling {
    public sealed partial class Theme {
        // Pass, fail and warn are chosen so each pair stays apart under simulated protanopia and deuteranopia
        // (blue / rose / yellow on dark, blue / magenta / dark gold on light), and fail also carries an underline.
        private static Theme BuildColorBlindSafe() {
            ThemeColor blue = ThemeColor.Hex("#6cb6ff", 14, 75);
            ThemeColor rose = ThemeColor.Hex("#ee6677", 9, 204);
            ThemeColor orange = ThemeColor.Hex("#e69f00", 9, 172);
            ThemeColor yellow = ThemeColor.Hex("#f0e442", 11, 221);
            ThemeColor purple = ThemeColor.Hex("#cc79a7", 13, 175);
            ThemeColor teal = ThemeColor.Hex("#5ed5bf", 10, 79);
            ThemeColor foreground = ThemeColor.Hex("#eeeeee", 15, 255);
            ThemeColor muted = ThemeColor.Hex("#b5b5bd", 7, 250);
            ThemeColor ink = ThemeColor.Hex("#1a1a1a", 0, 234);
            return Dark.Rename("ColorBlindSafe")
                .With(ThemeToken.Text, new ThemeStyle(foreground))
                .With(ThemeToken.Muted, new ThemeStyle(muted))
                .With(ThemeToken.Pending, new ThemeStyle(muted))
                .With(ThemeToken.Skip, new ThemeStyle(muted))
                .With(ThemeToken.Border, new ThemeStyle(muted))
                .With(ThemeToken.Accent, new ThemeStyle(blue, null, TextDecoration.Bold))
                .With(ThemeToken.Accent2, new ThemeStyle(purple))
                .With(ThemeToken.Pass, new ThemeStyle(blue, null, TextDecoration.Bold))
                .With(ThemeToken.Fail, new ThemeStyle(rose, null, TextDecoration.Bold | TextDecoration.Underline))
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
                .With(ThemeToken.Low, new ThemeStyle(ThemeColor.Hex("#cdbb7c", 11, 179)))
                .With(ThemeToken.Critical, new ThemeStyle(ink, rose, TextDecoration.Bold))
                .With(ThemeToken.Gap, new ThemeStyle(purple))
                .With(ThemeToken.Focus, new ThemeStyle(foreground, Dark.Get(ThemeToken.Focus).BackgroundColor, TextDecoration.Bold));
        }

        private static Theme BuildColorBlindSafeLight() {
            ThemeColor blue = ThemeColor.Hex("#00509d", 4, 25);
            ThemeColor info = ThemeColor.Hex("#0072b2", 4, 31);
            ThemeColor magenta = ThemeColor.Hex("#c2185b", 5, 125);
            ThemeColor gold = ThemeColor.Hex("#806600", 3, 94);
            ThemeColor orange = ThemeColor.Hex("#b35900", 1, 130);
            ThemeColor green = ThemeColor.Hex("#00785a", 2, 29);
            ThemeColor purple = ThemeColor.Hex("#8e2c6e", 13, 89);
            ThemeColor teal = ThemeColor.Hex("#00756a", 6, 29);
            ThemeColor foreground = ThemeColor.Hex("#111827", 0, 234);
            ThemeColor muted = ThemeColor.Hex("#5c636e", 8, 241);
            ThemeColor paper = ThemeColor.Hex("#ffffff", 15, 231);
            return Light.Rename("ColorBlindSafeLight")
                .With(ThemeToken.Text, new ThemeStyle(foreground))
                .With(ThemeToken.Muted, new ThemeStyle(muted))
                .With(ThemeToken.Pending, new ThemeStyle(muted))
                .With(ThemeToken.Skip, new ThemeStyle(muted))
                .With(ThemeToken.Accent, new ThemeStyle(blue, null, TextDecoration.Bold))
                .With(ThemeToken.Accent2, new ThemeStyle(purple))
                .With(ThemeToken.Pass, new ThemeStyle(blue, null, TextDecoration.Bold))
                .With(ThemeToken.Fail, new ThemeStyle(magenta, null, TextDecoration.Bold | TextDecoration.Underline))
                .With(ThemeToken.Warn, new ThemeStyle(gold, null, TextDecoration.Bold))
                .With(ThemeToken.Info, new ThemeStyle(info))
                .With(ThemeToken.Running, new ThemeStyle(teal))
                .With(ThemeToken.Title, new ThemeStyle(foreground, null, TextDecoration.Bold))
                .With(ThemeToken.Header, new ThemeStyle(foreground, null, TextDecoration.Bold | TextDecoration.Underline))
                .With(ThemeToken.Highlight, new ThemeStyle(gold, null, TextDecoration.Bold))
                .With(ThemeToken.SurfaceAccent, new ThemeStyle(blue, Light.Get(ThemeToken.SurfaceAccent).BackgroundColor, TextDecoration.Bold))
                .With(ThemeToken.Series1, new ThemeStyle(info))
                .With(ThemeToken.Series2, new ThemeStyle(orange))
                .With(ThemeToken.Series3, new ThemeStyle(green))
                .With(ThemeToken.Series4, new ThemeStyle(purple))
                .With(ThemeToken.Series5, new ThemeStyle(gold))
                .With(ThemeToken.Series6, new ThemeStyle(ThemeColor.Hex("#333333", 8, 236)))
                .With(ThemeToken.Low, new ThemeStyle(ThemeColor.Hex("#6f6650", 3, 101)))
                .With(ThemeToken.Critical, new ThemeStyle(paper, magenta, TextDecoration.Bold))
                .With(ThemeToken.Gap, new ThemeStyle(purple));
        }
    }
}
