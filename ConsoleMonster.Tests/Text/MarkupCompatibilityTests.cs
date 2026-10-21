using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Rendering;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using Xunit;

namespace ConsoleMonster.Tests.Text {
    /// <summary>
    /// The Spectre.Console markup dialect: its 256 named colours (the xterm colour list, with duplicate names numbered
    /// _1, _2 in index order), links, unknown tags and escaping.
    /// </summary>
    public sealed class MarkupCompatibilityTests {
        // The xterm 256-colour names by index, as Spectre.Console names them (facts, written out independently of the code).
        private const string XtermNames =
            "0 black|1 maroon|2 green|3 olive|4 navy|5 purple|6 teal|7 silver|8 grey|9 red|10 lime|11 yellow|12 blue|13 fuchsia|14 aqua|15 white|" +
            "16 grey0|17 navyblue|18 darkblue|19 blue3|20 blue3_1|21 blue1|22 darkgreen|23 deepskyblue4|24 deepskyblue4_1|25 deepskyblue4_2|" +
            "26 dodgerblue3|27 dodgerblue2|28 green4|29 springgreen4|30 turquoise4|31 deepskyblue3|32 deepskyblue3_1|33 dodgerblue1|34 green3|" +
            "35 springgreen3|36 darkcyan|37 lightseagreen|38 deepskyblue2|39 deepskyblue1|40 green3_1|41 springgreen3_1|42 springgreen2|43 cyan3|" +
            "44 darkturquoise|45 turquoise2|46 green1|47 springgreen2_1|48 springgreen1|49 mediumspringgreen|50 cyan2|51 cyan1|52 darkred|" +
            "53 deeppink4|54 purple4|55 purple4_1|56 purple3|57 blueviolet|58 orange4|59 grey37|60 mediumpurple4|61 slateblue3|62 slateblue3_1|" +
            "63 royalblue1|64 chartreuse4|65 darkseagreen4|66 paleturquoise4|67 steelblue|68 steelblue3|69 cornflowerblue|70 chartreuse3|" +
            "71 darkseagreen4_1|72 cadetblue|73 cadetblue_1|74 skyblue3|75 steelblue1|76 chartreuse3_1|77 palegreen3|78 seagreen3|79 aquamarine3|" +
            "80 mediumturquoise|81 steelblue1_1|82 chartreuse2|83 seagreen2|84 seagreen1|85 seagreen1_1|86 aquamarine1|87 darkslategray2|" +
            "88 darkred_1|89 deeppink4_1|90 darkmagenta|91 darkmagenta_1|92 darkviolet|93 purple_1|94 orange4_1|95 lightpink4|96 plum4|" +
            "97 mediumpurple3|98 mediumpurple3_1|99 slateblue1|100 yellow4|101 wheat4|102 grey53|103 lightslategrey|104 mediumpurple|" +
            "105 lightslateblue|106 yellow4_1|107 darkolivegreen3|108 darkseagreen|109 lightskyblue3|110 lightskyblue3_1|111 skyblue2|" +
            "112 chartreuse2_1|113 darkolivegreen3_1|114 palegreen3_1|115 darkseagreen3|116 darkslategray3|117 skyblue1|118 chartreuse1|" +
            "119 lightgreen|120 lightgreen_1|121 palegreen1|122 aquamarine1_1|123 darkslategray1|124 red3|125 deeppink4_2|126 mediumvioletred|" +
            "127 magenta3|128 darkviolet_1|129 purple_2|130 darkorange3|131 indianred|132 hotpink3|133 mediumorchid3|134 mediumorchid|" +
            "135 mediumpurple2|136 darkgoldenrod|137 lightsalmon3|138 rosybrown|139 grey63|140 mediumpurple2_1|141 mediumpurple1|142 gold3|" +
            "143 darkkhaki|144 navajowhite3|145 grey69|146 lightsteelblue3|147 lightsteelblue|148 yellow3|149 darkolivegreen3_2|" +
            "150 darkseagreen3_1|151 darkseagreen2|152 lightcyan3|153 lightskyblue1|154 greenyellow|155 darkolivegreen2|156 palegreen1_1|" +
            "157 darkseagreen2_1|158 darkseagreen1|159 paleturquoise1|160 red3_1|161 deeppink3|162 deeppink3_1|163 magenta3_1|164 magenta3_2|" +
            "165 magenta2|166 darkorange3_1|167 indianred_1|168 hotpink3_1|169 hotpink2|170 orchid|171 mediumorchid1|172 orange3|" +
            "173 lightsalmon3_1|174 lightpink3|175 pink3|176 plum3|177 violet|178 gold3_1|179 lightgoldenrod3|180 tan|181 mistyrose3|" +
            "182 thistle3|183 plum2|184 yellow3_1|185 khaki3|186 lightgoldenrod2|187 lightyellow3|188 grey84|189 lightsteelblue1|190 yellow2|" +
            "191 darkolivegreen1|192 darkolivegreen1_1|193 darkseagreen1_1|194 honeydew2|195 lightcyan1|196 red1|197 deeppink2|198 deeppink1|" +
            "199 deeppink1_1|200 magenta2_1|201 magenta1|202 orangered1|203 indianred1|204 indianred1_1|205 hotpink|206 hotpink_1|" +
            "207 mediumorchid1_1|208 darkorange|209 salmon1|210 lightcoral|211 palevioletred1|212 orchid2|213 orchid1|214 orange1|" +
            "215 sandybrown|216 lightsalmon1|217 lightpink1|218 pink1|219 plum1|220 gold1|221 lightgoldenrod2_1|222 lightgoldenrod2_2|" +
            "223 navajowhite1|224 mistyrose1|225 thistle1|226 yellow1|227 lightgoldenrod1|228 khaki1|229 wheat1|230 cornsilk1|231 grey100|" +
            "232 grey3|233 grey7|234 grey11|235 grey15|236 grey19|237 grey23|238 grey27|239 grey30|240 grey35|241 grey39|242 grey42|" +
            "243 grey46|244 grey50|245 grey54|246 grey58|247 grey62|248 grey66|249 grey70|250 grey74|251 grey78|252 grey82|253 grey85|" +
            "254 grey89|255 grey93";

        private static IEnumerable<KeyValuePair<int, string>> Names() {
            foreach (string pair in XtermNames.Split('|')) {
                string[] parts = pair.Split(' ');
                yield return new KeyValuePair<int, string>(int.Parse(parts[0], CultureInfo.InvariantCulture), parts[1]);
            }
        }

        private static Color Expected(int index) => index < 16 ? Color.FromAnsi16(index) : Color.FromAnsi256(index);

        [Fact]
        public void Parse_All256XtermNames_MapToTheirIndex() {
            List<KeyValuePair<int, string>> names = Names().ToList();
            Assert.Equal(256, names.Count);
            Assert.Equal(256, names.Select(n => n.Value).Distinct(StringComparer.Ordinal).Count());
            foreach (KeyValuePair<int, string> name in names) {
                Assert.True(Markup.TryParseStyle(name.Value, null, ColorDepth.TrueColor, out Style style), name.Value);
                Assert.Equal(Expected(name.Key), style.Foreground);
                Assert.True(Markup.TryParseStyle("on " + name.Value.ToUpperInvariant(), null, ColorDepth.TrueColor, out Style background), name.Value);
                Assert.Equal(Expected(name.Key), background.Background);
            }
        }

        [Theory]
        [InlineData("cadetblue", 72)]
        [InlineData("mediumpurple2", 135)]
        [InlineData("steelblue", 67)]
        [InlineData("aquamarine1", 86)]
        [InlineData("lightseagreen", 37)]
        [InlineData("lightgoldenrod2", 186)]
        [InlineData("palevioletred1", 211)]
        [InlineData("deepskyblue2", 38)]
        [InlineData("lightskyblue1", 153)]
        [InlineData("slateblue1", 99)]
        [InlineData("plum4", 96)]
        [InlineData("cornsilk1", 230)]
        [InlineData("yellow3", 148)]
        [InlineData("grey50", 244)]
        [InlineData("gray50", 244)]
        public void Parse_NamesUsedByConsumers_RenderInsteadOfPrintingLiterally(string name, int index) {
            TextSpan span = Assert.Single(Markup.Parse("[" + name + "]text[/]").Lines[0]);
            Assert.Equal("text", span.Text);
            Assert.Equal(Color.FromAnsi256(index), span.Style.Foreground);
        }

        [Fact]
        public void Parse_NamedColour_IsMappedPerColourTier() {
            Assert.Equal(Color.FromAnsi256(72), Single("[cadetblue]x[/]", ColorDepth.TrueColor).Style.Foreground);
            Assert.Equal(Color.FromAnsi256(72), Single("[cadetblue]x[/]", ColorDepth.Ansi256).Style.Foreground);
            Assert.Equal(ColorKind.Ansi16, Single("[cadetblue]x[/]", ColorDepth.Ansi16).Style.Foreground.Kind);
            Assert.True(Single("[cadetblue]x[/]", ColorDepth.None).Style.Foreground.IsDefault);
        }

        [Fact]
        public void Parse_ConsoleColorNamesOutsideTheXtermList_KeepThePowerShellMapping() {
            Assert.Equal(Color.FromAnsi16(3), Single("[darkyellow]x[/]", ColorDepth.TrueColor).Style.Foreground);
            Assert.Equal(Color.FromAnsi16(8), Single("[darkgray]x[/]", ColorDepth.TrueColor).Style.Foreground);
            Assert.Equal(Color.FromAnsi16(13), Single("[magenta]x[/]", ColorDepth.TrueColor).Style.Foreground);
            Assert.True(RichColor.TryParse("DarkRed", out RichColor console));
            Assert.Equal(Color.FromAnsi16(1), console.Color);
        }

        // Colliding names: markup follows the 256-colour palette, -Color / ConsoleColor parameters follow Write-Host, and the
        // console. / palette. prefixes ask for either meaning in both places. 16:n is the 16-colour index, 256:n the palette.
        private const string CollisionGolden =
            "name          markup   -Color   [console.name]  -Color palette.name\n" +
            "red           16:9     16:9     16:9            16:9\n" +
            "darkred       256:52   16:1     16:1            256:52\n" +
            "blue          16:12    16:12    16:12           16:12\n" +
            "darkblue      256:18   16:4     16:4            256:18\n" +
            "grey          16:8     16:7     16:7            16:8\n" +
            "gray          16:8     16:7     16:7            16:8\n" +
            "darkgray      16:8     16:8     16:8            16:8\n";

        [Fact]
        public void CollidingNames_FollowThePaletteInMarkupAndWriteHostInColorParameters() {
            StringBuilder table = new StringBuilder("name          markup   -Color   [console.name]  -Color palette.name\n");
            foreach (string name in new[] { "red", "darkred", "blue", "darkblue", "grey", "gray", "darkgray" }) {
                Color markup = Single("[" + name + "]x[/]", ColorDepth.TrueColor).Style.Foreground;
                Color console = Single("[console." + name + "]x[/]", ColorDepth.TrueColor).Style.Foreground;
                Assert.True(RichColor.TryParse(name, out RichColor parameter));
                Assert.True(RichColor.TryParse("palette." + name, out RichColor palette));
                table.Append(name.PadRight(14)).Append(Code(markup).PadRight(9)).Append(Code(parameter.Color).PadRight(9))
                    .Append(Code(console).PadRight(16)).Append(Code(palette.Color)).Append('\n');
            }
            Assert.Equal(CollisionGolden, table.ToString());
        }

        [Fact]
        public void ExplicitForms_WorkInBothPlaces() {
            Assert.Equal(Color.FromRgb(0x87, 0, 0), Single("[#870000]x[/]", ColorDepth.TrueColor).Style.Foreground);
            Assert.True(RichColor.TryParse("#870000", out RichColor hex));
            Assert.Equal(Color.FromRgb(0x87, 0, 0), hex.Color);
            Assert.True(RichColor.TryParse("console.DarkRed", out RichColor console));
            Assert.Equal(Color.FromAnsi16(1), console.Color);
            Assert.Equal(Color.FromAnsi16(1), Single("[on console.darkred]x[/]", ColorDepth.TrueColor).Style.Background);
            Assert.False(Markup.TryParseStyle("console.cadetblue", null, ColorDepth.TrueColor, out Style _));
        }

        private static string Code(Color color) => (color.Kind == ColorKind.Ansi16 ? "16:" : "256:") + color.Index.ToString(CultureInfo.InvariantCulture);

        [Theory]
        [InlineData("[nosuchcolour]text[/]", "text")]
        [InlineData("a [bold nosuch]b[/] c", "a b c")]
        [InlineData("[red][nosuch]a[/]b[/]", "ab")]
        [InlineData("[x] done", "[x] done")]
        [InlineData("[1/3] step", "[1/3] step")]
        [InlineData("[ ] todo", "[ ] todo")]
        [InlineData("[nosuch]open", "[nosuch]open")]
        public void Parse_UnknownTags_AreRemovedWhenClosedAndLiteralOtherwise(string markup, string expected) {
            Assert.Equal(expected, Markup.Strip(markup));
        }

        [Fact]
        public void Parse_UnknownTag_KeepsTheEnclosingStyle() {
            StyledLine line = Markup.Parse("[red]a[nosuch]b[/]c[/]").Lines[0];
            Assert.Equal("abc", line.PlainText);
            Assert.All(line, span => Assert.Equal(Color.FromAnsi16(9), span.Style.Foreground));
        }

        [Theory]
        [InlineData("[nosuchcolour]text[/]")]
        [InlineData("[red]unclosed")]
        [InlineData("[/]extra")]
        [InlineData("a ] b")]
        public void Parse_Strict_ThrowsForInvalidMarkup(string markup) {
            FormatException error = Assert.Throws<FormatException>(() => Markup.Parse(markup, null, ColorDepth.TrueColor, true));
            Assert.StartsWith("Invalid markup:", error.Message);
        }

        [Fact]
        public void Parse_Strict_AcceptsValidMarkup() {
            Assert.Equal("ok [x]", Markup.Parse("[cadetblue]ok[/] [[x]]", null, ColorDepth.TrueColor, true).Lines[0].PlainText);
        }

        [Fact]
        public void LinkTag_WithTarget_SetsTheLink() {
            TextSpan span = Single("[link=https://example.com/docs]docs[/]", ColorDepth.TrueColor);
            Assert.Equal("docs", span.Text);
            Assert.Equal("https://example.com/docs", span.Style.Link);
        }

        [Fact]
        public void LinkTag_WithoutTarget_UsesItsTextAsTheTarget() {
            StyledLine line = Markup.Parse("see [link]https://example.com[/] now").Lines[0];
            Assert.Equal("see https://example.com now", line.PlainText);
            Assert.Equal("https://example.com", line.Single(s => s.Text == "https://example.com").Style.Link);
            Assert.Null(line.First().Style.Link);
            Assert.Null(line.Last().Style.Link);
        }

        [Fact]
        public void LinkTag_WithoutTarget_CombinesWithStyles() {
            TextSpan span = Single("[bold link]https://example.com/a[/]", ColorDepth.TrueColor);
            Assert.Equal("https://example.com/a", span.Style.Link);
            Assert.Equal(TextDecoration.Bold, span.Style.Decoration);
        }

        [Theory]
        [InlineData("[link=https://example.com]x[/]", "https://example.com")]
        [InlineData("[link]mailto:ops@example.com[/]", "mailto:ops@example.com")]
        public void Encode_SafeLink_IsWrittenAsOsc8(string markup, string target) {
            string encoded = Encode(markup);
            Assert.Contains("\u001b]8;;" + target + "\u001b\\", encoded);
            Assert.Contains("\u001b]8;;\u001b\\", encoded);
        }

        [Theory]
        [InlineData("[link=javascript:alert(1)]x[/]")]
        [InlineData("[link]data:text/html,hi[/]")]
        [InlineData("[link=relative/path]x[/]")]
        public void Encode_UnsafeLink_StaysPlainText(string markup) {
            Assert.DoesNotContain("\u001b]8;", Encode(markup));
        }

        [Fact]
        public void Escape_And_EscapeMarkup_MakeTextLiteral() {
            string value = "[red]not a tag[/]";
            Assert.Equal("[[red]]not a tag[[/]]", Markup.Escape(value));
            Assert.Equal(Markup.Escape(value), value.EscapeMarkup());
            Assert.Equal(value, Markup.Strip("[bold]" + value.EscapeMarkup() + "[/]"));
            Assert.Equal("plain", "[pass]plain[/]".RemoveMarkup());
        }

        [Fact]
        public void Interpolate_EscapesArgumentsAfterFormatting() {
            string name = "srv[01]";
            double seconds = 1234.5;
            string markup = Markup.Interpolate(CultureInfo.InvariantCulture, $"[pass]{name}[/] took {seconds:N1}s, {null}end");
            Assert.Equal("[pass]srv[[01]][/] took 1,234.5s, end", markup);
            Assert.Equal("srv[01] took 1,234.5s, end", Markup.Strip(markup));
        }

        private static TextSpan Single(string markup, ColorDepth depth) => Assert.Single(Markup.Parse(markup, null, depth).Lines[0]);

        private static string Encode(string markup) {
            StyledLine line = Markup.Parse(markup).Lines[0];
            CellBuffer buffer = new CellBuffer(line.PlainText.Length + 2, 1);
            int x = 0;
            foreach (TextSpan span in line) {
                x = buffer.Write(x, 0, span.Text, span.Style);
            }
            StringBuilder sb = new StringBuilder();
            Style? current = null;
            CellEncoder.AppendRun(sb, buffer, 0, 0, buffer.Width, ColorDepth.TrueColor, ref current, false, true);
            return sb.ToString();
        }
    }
}
