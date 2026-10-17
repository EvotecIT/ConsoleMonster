using System;
using System.Collections.Generic;
using System.Globalization;

namespace ConsoleMonster.Styling {
    /// <summary>
    /// Colour names accepted in markup and style specifications: the 256 xterm names of the Spectre.Console markup dialect
    /// (<c>green</c> is index 2, <c>lime</c> index 10, <c>cadetblue</c> index 72, <c>grey50</c> index 244). A name used
    /// more than once in the xterm list takes a <c>_1</c>, <c>_2</c> suffix from its second use on (<c>blue3</c> is 19,
    /// <c>blue3_1</c> is 20), as Spectre names them. A few extras are accepted too: ConsoleColor names that are not xterm
    /// names (<c>gray</c>, <c>darkgray</c>, <c>darkyellow</c>, <c>magenta</c>, <c>cyan</c>) with the PowerShell mapping,
    /// <c>gray</c> spellings of the grey names, <c>orange</c>, <c>pink</c>, and <c>grey0</c>-<c>grey100</c> values that
    /// are not xterm names as a neutral RGB grey of that lightness.
    /// </summary>
    internal static class ColorNames {
        // The xterm 256-colour names in index order (duplicates are numbered when the table is built).
        private static readonly string[] Xterm = {
            "black", "maroon", "green", "olive", "navy", "purple", "teal", "silver",
            "grey", "red", "lime", "yellow", "blue", "fuchsia", "aqua", "white",
            "grey0", "navyblue", "darkblue", "blue3", "blue3", "blue1", "darkgreen", "deepskyblue4",
            "deepskyblue4", "deepskyblue4", "dodgerblue3", "dodgerblue2", "green4", "springgreen4", "turquoise4", "deepskyblue3",
            "deepskyblue3", "dodgerblue1", "green3", "springgreen3", "darkcyan", "lightseagreen", "deepskyblue2", "deepskyblue1",
            "green3", "springgreen3", "springgreen2", "cyan3", "darkturquoise", "turquoise2", "green1", "springgreen2",
            "springgreen1", "mediumspringgreen", "cyan2", "cyan1", "darkred", "deeppink4", "purple4", "purple4",
            "purple3", "blueviolet", "orange4", "grey37", "mediumpurple4", "slateblue3", "slateblue3", "royalblue1",
            "chartreuse4", "darkseagreen4", "paleturquoise4", "steelblue", "steelblue3", "cornflowerblue", "chartreuse3", "darkseagreen4",
            "cadetblue", "cadetblue", "skyblue3", "steelblue1", "chartreuse3", "palegreen3", "seagreen3", "aquamarine3",
            "mediumturquoise", "steelblue1", "chartreuse2", "seagreen2", "seagreen1", "seagreen1", "aquamarine1", "darkslategray2",
            "darkred", "deeppink4", "darkmagenta", "darkmagenta", "darkviolet", "purple", "orange4", "lightpink4",
            "plum4", "mediumpurple3", "mediumpurple3", "slateblue1", "yellow4", "wheat4", "grey53", "lightslategrey",
            "mediumpurple", "lightslateblue", "yellow4", "darkolivegreen3", "darkseagreen", "lightskyblue3", "lightskyblue3", "skyblue2",
            "chartreuse2", "darkolivegreen3", "palegreen3", "darkseagreen3", "darkslategray3", "skyblue1", "chartreuse1", "lightgreen",
            "lightgreen", "palegreen1", "aquamarine1", "darkslategray1", "red3", "deeppink4", "mediumvioletred", "magenta3",
            "darkviolet", "purple", "darkorange3", "indianred", "hotpink3", "mediumorchid3", "mediumorchid", "mediumpurple2",
            "darkgoldenrod", "lightsalmon3", "rosybrown", "grey63", "mediumpurple2", "mediumpurple1", "gold3", "darkkhaki",
            "navajowhite3", "grey69", "lightsteelblue3", "lightsteelblue", "yellow3", "darkolivegreen3", "darkseagreen3", "darkseagreen2",
            "lightcyan3", "lightskyblue1", "greenyellow", "darkolivegreen2", "palegreen1", "darkseagreen2", "darkseagreen1", "paleturquoise1",
            "red3", "deeppink3", "deeppink3", "magenta3", "magenta3", "magenta2", "darkorange3", "indianred",
            "hotpink3", "hotpink2", "orchid", "mediumorchid1", "orange3", "lightsalmon3", "lightpink3", "pink3",
            "plum3", "violet", "gold3", "lightgoldenrod3", "tan", "mistyrose3", "thistle3", "plum2",
            "yellow3", "khaki3", "lightgoldenrod2", "lightyellow3", "grey84", "lightsteelblue1", "yellow2", "darkolivegreen1",
            "darkolivegreen1", "darkseagreen1", "honeydew2", "lightcyan1", "red1", "deeppink2", "deeppink1", "deeppink1",
            "magenta2", "magenta1", "orangered1", "indianred1", "indianred1", "hotpink", "hotpink", "mediumorchid1",
            "darkorange", "salmon1", "lightcoral", "palevioletred1", "orchid2", "orchid1", "orange1", "sandybrown",
            "lightsalmon1", "lightpink1", "pink1", "plum1", "gold1", "lightgoldenrod2", "lightgoldenrod2", "navajowhite1",
            "mistyrose1", "thistle1", "yellow1", "lightgoldenrod1", "khaki1", "wheat1", "cornsilk1", "grey100",
            "grey3", "grey7", "grey11", "grey15", "grey19", "grey23", "grey27", "grey30",
            "grey35", "grey39", "grey42", "grey46", "grey50", "grey54", "grey58", "grey62",
            "grey66", "grey70", "grey74", "grey78", "grey82", "grey85", "grey89", "grey93"
        };

        private static readonly Dictionary<string, Color> Names = Build();

        public static bool TryGet(string lowerName, out Color color) {
            if (Names.TryGetValue(lowerName, out color)) {
                return true;
            }
            string? digits = null;
            if (lowerName.StartsWith("grey", StringComparison.Ordinal) || lowerName.StartsWith("gray", StringComparison.Ordinal)) {
                digits = lowerName.Substring(4);
            }
            if (digits != null && digits.Length > 0 && int.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out int percent) && percent >= 0 && percent <= 100) {
                byte level = (byte)Math.Round(percent * 2.55);
                color = Color.FromRgb(level, level, level);
                return true;
            }
            color = Color.Default;
            return false;
        }

        private static Dictionary<string, Color> Build() {
            Dictionary<string, Color> names = new Dictionary<string, Color>(StringComparer.Ordinal);
            Dictionary<string, int> uses = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < Xterm.Length; i++) {
                string name = Xterm[i];
                uses.TryGetValue(name, out int count);
                uses[name] = count + 1;
                string key = count == 0 ? name : name + "_" + count.ToString(CultureInfo.InvariantCulture);
                names[key] = i < 16 ? Color.FromAnsi16(i) : Color.FromAnsi256(i);
                if (key.StartsWith("grey", StringComparison.Ordinal)) {
                    names["gray" + key.Substring(4)] = names[key];
                }
            }
            // ConsoleColor names that are not xterm names keep the PowerShell (Write-Host) mapping.
            names["darkgray"] = Color.FromAnsi16(8);
            names["darkgrey"] = Color.FromAnsi16(8);
            names["darkyellow"] = Color.FromAnsi16(3);
            names["magenta"] = Color.FromAnsi16(13);
            names["cyan"] = Color.FromAnsi16(14);
            names["orange"] = Color.FromRgb(255, 165, 0);
            names["pink"] = Color.FromRgb(255, 175, 215);
            return names;
        }
    }
}
