using System;
using System.IO;
using System.Linq;
using System.Text;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Styling;
using Xunit;

namespace ConsoleMonster.Tests.Glyphs;

public sealed class ConsoleViewFontTests {
    [InstalledFontFact("CascadiaMono.ttf")]
    public void FullTokens_InstalledCascadiaMono_CoversEveryDeclaredGlyph() => Check("CascadiaMono.ttf", GlyphTier.Full);

    [InstalledFontFact("consola.ttf")]
    public void BasicTokens_InstalledConsolas_CoversEveryDeclaredGlyph() => Check("consola.ttf", GlyphTier.Basic);

    [InstalledFontFact("lucon.ttf")]
    public void BasicTokens_InstalledLucidaConsole_CoversEveryDeclaredGlyph() => Check("lucon.ttf", GlyphTier.Basic);

    private static void Check(string font, GlyphTier tier) {
        byte[] bytes = File.ReadAllBytes(FontPath(font));
        GlyphSet set = (GlyphSet)global::ConsoleMonster.Widgets.ConsoleViewStyle.Apply(Terminal.CreateContext(TerminalProfile.Fixed(120, 24, ColorDepth.None, tier))).Glyphs;
        string tokens = string.Concat(Enum.GetValues(typeof(Glyph)).Cast<Glyph>().Select(set.Get))
            + string.Concat(Enum.GetValues(typeof(StatusKind)).Cast<StatusKind>().Select(set.StatusMarker))
            + string.Concat(Enum.GetValues(typeof(Severity)).Cast<Severity>().Select(value => StatusScale.Marker(value, tier)))
            + string.Concat(Enum.GetValues(typeof(GlyphIcon)).Cast<GlyphIcon>().Select(set.Icon))
            + string.Concat(Enumerable.Range(1, 8).Select(set.HorizontalFill));
        int[] missing = tokens.Where(c => !char.IsWhiteSpace(c)).Distinct().Where(c => !Covered(bytes, c)).Select(c => (int)c).ToArray();
        Assert.True(missing.Length == 0, font + " lacks " + string.Join(", ", missing.Select(c => "U+" + c.ToString("X4"))));
    }

    private static string FontPath(string name) => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), name);

    // Read the installed font's Unicode cmap, rather than trusting font fallback in a browser or terminal.
    private static bool Covered(byte[] bytes, int point) {
        int count = U16(bytes, 4);
        int cmap = -1;
        for (int table = 0; table < count; table++) {
            int entry = 12 + 16 * table;
            if (Encoding.ASCII.GetString(bytes, entry, 4) == "cmap") { cmap = (int)U32(bytes, entry + 8); break; }
        }
        Assert.True(cmap >= 0, "The installed TrueType font must contain a cmap.");
        int records = U16(bytes, cmap + 2);
        for (int index = 0; index < records; index++) {
            int record = cmap + 4 + index * 8;
            int platform = U16(bytes, record);
            int encoding = U16(bytes, record + 2);
            if (platform != 0 && !(platform == 3 && (encoding == 1 || encoding == 10))) { continue; }
            int map = cmap + (int)U32(bytes, record + 4);
            int format = U16(bytes, map);
            if (format == 12) {
                uint groups = U32(bytes, map + 12);
                for (int group = 0; group < groups; group++) {
                    int at = map + 16 + 12 * group;
                    uint start = U32(bytes, at);
                    uint end = U32(bytes, at + 4);
                    if (point >= start && point <= end && U32(bytes, at + 8) + point - start != 0) { return true; }
                }
            } else if (format == 4 && point <= ushort.MaxValue) {
                int segments = U16(bytes, map + 6) / 2;
                int endBase = map + 14;
                int startBase = endBase + 2 * segments + 2;
                int deltaBase = startBase + 2 * segments;
                int rangeBase = deltaBase + 2 * segments;
                for (int segment = 0; segment < segments; segment++) {
                    if (point < U16(bytes, startBase + segment * 2) || point > U16(bytes, endBase + segment * 2)) { continue; }
                    int delta = U16(bytes, deltaBase + segment * 2);
                    int range = U16(bytes, rangeBase + segment * 2);
                    int glyph = range == 0 ? point : U16(bytes, rangeBase + segment * 2 + range + 2 * (point - U16(bytes, startBase + segment * 2)));
                    if (glyph != 0 && ((glyph + delta) & 0xffff) != 0) { return true; }
                }
            }
        }
        return false;
    }
    private static int U16(byte[] bytes, int at) => bytes[at] << 8 | bytes[at + 1];
    private static uint U32(byte[] bytes, int at) => (uint)(bytes[at] << 24 | bytes[at + 1] << 16 | bytes[at + 2] << 8 | bytes[at + 3]);

    private sealed class InstalledFontFactAttribute : FactAttribute {
        public InstalledFontFactAttribute(string font) {
            if (!File.Exists(FontPath(font))) { Skip = "The target font is not installed: " + font; }
        }
    }
}
