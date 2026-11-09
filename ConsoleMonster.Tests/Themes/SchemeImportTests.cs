using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using ConsoleMonster.Serialization;
using ConsoleMonster.Styling;
using Xunit;

namespace ConsoleMonster.Tests.Themes {
    public sealed class SchemeImportTests {
        [Theory]
        [InlineData("json")]
        [InlineData("itermcolors")]
        [InlineData("yaml")]
        public void LoadsSyntheticNativeSchemeFilesWithoutChangingTheirColours(string format) {
            string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + "." + format);
            try {
                string source = format == "json" ? Windows() : format == "itermcolors" ? ITerm() : Base16();
                File.WriteAllText(path, source);
                CultureInfo previous = CultureInfo.CurrentCulture;
                TerminalScheme scheme;
                try { CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("pl-PL"); scheme = TerminalScheme.Load(path); }
                finally { CultureInfo.CurrentCulture = previous; }
                Assert.Equal(16, scheme.Palette.Count);
                int[] slots = format == "yaml" ? new[] { 0, 8, 11, 10, 13, 14, 12, 5, 3, 8, 11, 10, 13, 14, 12, 7 } : Enumerable.Range(0, 16).ToArray();
                for (int i = 0; i < 16; i++) { Assert.Equal(Color.FromRgb((byte)(slots[i] * 17), 51, 119), scheme.Palette[i]); }
                Assert.Equal(Color.FromHex(format == "yaml" ? "#553377" : "#eeeeee"), scheme.Foreground);
                Assert.Equal(Color.FromHex(format == "yaml" ? "#003377" : "#112233"), scheme.Background);
                Theme theme = ThemeFile.Parse(ThemeFile.FromTheme(Theme.FromScheme(scheme)).ToJson()).ToTheme();
                Assert.Equal(scheme.Palette, theme.Scheme!.Palette);
            } finally { File.Delete(path); }
        }

        [Fact]
        public void WindowsSettingsNeedsOneExplicitSchemeAndDoesNotSilentlyPickTheFirst() {
            Assert.Equal("Synthetic", TerminalScheme.ParseWindowsTerminal("{\"schemes\":[" + Windows() + "]}").Name);
            Assert.Throws<FormatException>(() => TerminalScheme.ParseWindowsTerminal("{\"schemes\":[" + Windows() + "," + Windows() + "]}"));
        }

        [Fact]
        public void Base16ReadsCommentsQuotedHexAndRejectsMissingOrDuplicateSlots() {
            TerminalScheme scheme = TerminalScheme.ParseBase16("# header\n" + Base16() + "author: Someone # metadata\n");
            Assert.Equal("Synthetic", scheme.Name);
            Assert.Throws<FormatException>(() => TerminalScheme.ParseBase16(Base16() + "base00: '000000'\n"));
            Assert.Throws<FormatException>(() => TerminalScheme.ParseBase16(Base16().Replace("base0F: 'ff3377' # slot\n", "")));
        }

        [Fact]
        public void ITermRejectsOutOfRangeAndNonSrgbComponents() {
            string source = ITerm();
            Assert.Throws<FormatException>(() => TerminalScheme.ParseITermColors(source.Replace("<real>0.2</real>", "<real>1.2</real>")));
            Assert.Throws<FormatException>(() => TerminalScheme.ParseITermColors(source.Replace("<dict><key>Red Component", "<dict><key>Color Space</key><string>P3</string><key>Red Component")));
        }

        private static string Windows() {
            string[] keys = { "black", "red", "green", "yellow", "blue", "purple", "cyan", "white", "brightBlack", "brightRed", "brightGreen", "brightYellow", "brightBlue", "brightPurple", "brightCyan", "brightWhite" };
            Dictionary<string, object?> data = new Dictionary<string, object?> { ["name"] = "Synthetic", ["foreground"] = "#eeeeee", ["background"] = "#112233", ["selectionBackground"] = "#334455", ["cursorColor"] = "#ffffff" };
            for (int i = 0; i < 16; i++) { data[keys[i]] = "#" + (i * 17).ToString("x2", CultureInfo.InvariantCulture) + "3377"; }
            return MiniJson.Write(data);
        }
        private static string ITerm() {
            StringBuilder xml = new StringBuilder("<?xml version=\"1.0\"?><!DOCTYPE plist PUBLIC \"-//Apple//DTD PLIST 1.0//EN\" \"http://www.apple.com/DTDs/PropertyList-1.0.dtd\"><plist version=\"1.0\"><dict>");
            void ColorEntry(string key, double r, double g, double b) => xml.Append("<key>" + key + "</key><dict><key>Red Component</key><real>" + r.ToString("R", CultureInfo.InvariantCulture)
                + "</real><key>Green Component</key><real>" + g.ToString("R", CultureInfo.InvariantCulture) + "</real><key>Blue Component</key><real>" + b.ToString("R", CultureInfo.InvariantCulture) + "</real></dict>");
            for (int i = 0; i < 16; i++) { ColorEntry("Ansi " + i + " Color", i / 15.0, .2, 119 / 255.0); }
            ColorEntry("Foreground Color", 238 / 255.0, 238 / 255.0, 238 / 255.0);
            ColorEntry("Background Color", 17 / 255.0, 34 / 255.0, .2);
            return xml.Append("</dict></plist>").ToString();
        }
        private static string Base16() {
            StringBuilder yaml = new StringBuilder("scheme: \"Synthetic\" # name\n");
            for (int i = 0; i < 16; i++) { yaml.Append("base" + i.ToString("X2", CultureInfo.InvariantCulture) + ": '" + (i * 17).ToString("x2", CultureInfo.InvariantCulture) + "3377' # slot\n"); }
            return yaml.ToString();
        }
    }
}
