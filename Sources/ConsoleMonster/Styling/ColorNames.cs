using System;
using System.Collections.Generic;
using System.Globalization;

namespace ConsoleMonster.Styling {
    /// <summary>
    /// Colour names accepted in markup and style specifications. The 16 base names follow Spectre.Console
    /// (<c>green</c> is index 2, <c>lime</c> is index 10); ConsoleColor names (<c>DarkCyan</c>, <c>Gray</c>) and a set of
    /// common xterm names (<c>orange1</c>, <c>deepskyblue1</c>) are accepted too. <c>grey0</c>-<c>grey100</c> map to a
    /// neutral RGB grey of that lightness.
    /// </summary>
    internal static class ColorNames {
        private static readonly Dictionary<string, Color> Names = Build();

        public static bool TryGet(string lowerName, out Color color) {
            if (Names.TryGetValue(lowerName, out color)) {
                return true;
            }
            string? digits = null;
            if (lowerName.StartsWith("grey", StringComparison.Ordinal) || lowerName.StartsWith("gray", StringComparison.Ordinal)) {
                digits = lowerName.Substring(4);
            }
            if (digits != null && int.TryParse(digits, NumberStyles.Integer, CultureInfo.InvariantCulture, out int percent) && percent >= 0 && percent <= 100) {
                byte level = (byte)Math.Round(percent * 2.55);
                color = Color.FromRgb(level, level, level);
                return true;
            }
            color = Color.Default;
            return false;
        }

        private static Dictionary<string, Color> Build() {
            Dictionary<string, Color> names = new Dictionary<string, Color>(StringComparer.Ordinal);
            string[] base16 = { "black", "maroon", "green", "olive", "navy", "purple", "teal", "silver", "grey", "red", "lime", "yellow", "blue", "fuchsia", "aqua", "white" };
            for (int i = 0; i < base16.Length; i++) {
                names[base16[i]] = Color.FromAnsi16(i);
            }
            names["gray"] = Color.FromAnsi16(8);
            names["darkred"] = Color.FromAnsi16(1);
            names["darkgreen"] = Color.FromAnsi16(2);
            names["darkyellow"] = Color.FromAnsi16(3);
            names["darkblue"] = Color.FromAnsi16(4);
            names["darkmagenta"] = Color.FromAnsi16(5);
            names["darkcyan"] = Color.FromAnsi16(6);
            names["darkgray"] = Color.FromAnsi16(8);
            names["darkgrey"] = Color.FromAnsi16(8);
            names["magenta"] = Color.FromAnsi16(13);
            names["cyan"] = Color.FromAnsi16(14);

            AddXterm(names, "orange1", 214);
            AddXterm(names, "orange3", 172);
            AddXterm(names, "darkorange", 208);
            AddXterm(names, "gold1", 220);
            AddXterm(names, "gold3", 178);
            AddXterm(names, "deepskyblue1", 39);
            AddXterm(names, "deepskyblue3", 32);
            AddXterm(names, "dodgerblue1", 33);
            AddXterm(names, "dodgerblue2", 27);
            AddXterm(names, "steelblue1", 75);
            AddXterm(names, "skyblue1", 117);
            AddXterm(names, "springgreen1", 48);
            AddXterm(names, "springgreen2", 47);
            AddXterm(names, "springgreen3", 41);
            AddXterm(names, "seagreen1", 85);
            AddXterm(names, "chartreuse1", 118);
            AddXterm(names, "chartreuse3", 76);
            AddXterm(names, "greenyellow", 154);
            AddXterm(names, "mediumpurple", 104);
            AddXterm(names, "mediumpurple1", 141);
            AddXterm(names, "purple3", 56);
            AddXterm(names, "hotpink", 205);
            AddXterm(names, "deeppink1", 199);
            AddXterm(names, "indianred", 167);
            AddXterm(names, "indianred1", 203);
            AddXterm(names, "salmon1", 209);
            AddXterm(names, "lightsalmon1", 216);
            AddXterm(names, "khaki1", 228);
            AddXterm(names, "lightgoldenrod1", 227);
            AddXterm(names, "turquoise2", 45);
            AddXterm(names, "cyan1", 51);
            AddXterm(names, "violet", 177);
            AddXterm(names, "plum1", 219);
            AddXterm(names, "wheat1", 229);
            AddXterm(names, "tan", 180);
            AddXterm(names, "lightslategrey", 103);
            AddXterm(names, "lightsteelblue", 147);
            names["orange"] = Color.FromRgb(255, 165, 0);
            names["pink"] = Color.FromRgb(255, 175, 215);
            return names;
        }

        private static void AddXterm(Dictionary<string, Color> names, string name, int index) {
            names[name] = Color.FromAnsi256(index);
        }
    }
}
