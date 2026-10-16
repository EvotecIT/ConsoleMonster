using System;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Tests.Text {
    internal sealed class NamedTheme : ITheme {
        public string Name => "Named theme";
        public Style Resolve(ThemeToken token, ColorDepth depth) => Style.Plain;
        public bool TryResolve(string name, ColorDepth depth, out Style style) {
            style = new Style(Color.FromRgb(12, 34, 56), decoration: TextDecoration.Italic).Downsample(depth);
            return string.Equals(name, "brand", StringComparison.OrdinalIgnoreCase);
        }
    }
}
