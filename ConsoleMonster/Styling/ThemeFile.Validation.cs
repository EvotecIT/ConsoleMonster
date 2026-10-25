using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

namespace ConsoleMonster.Styling {
    public sealed partial class ThemeFile {
        private static ThemeStyle ReadStyle(object? value, string path, ThemeStyle inherited) {
            Dictionary<string, object?> style = Map(value, path, "Foreground", "Background", "Decoration");
            ThemeColor? foreground = style.TryGetValue("Foreground", out object? fg) ? ReadColor(fg, path + ".Foreground") : inherited.Color;
            ThemeColor? background = style.TryGetValue("Background", out object? bg) ? ReadColor(bg, path + ".Background") : inherited.BackgroundColor;
            TextDecoration decoration = style.TryGetValue("Decoration", out object? flags) ? ReadDecoration(flags, path + ".Decoration") : inherited.Decoration;
            return new ThemeStyle(foreground, background, decoration);
        }

        private static ThemeColor? ReadColor(object? value, string path) {
            if (value == null) { return null; }
            if (value is string shorthand) {
                Color color = ReadTerminalColor(shorthand, path);
                return new ThemeColor(color, color.IsDefault ? -1 : color.Downsample(Capabilities.ColorDepth.Ansi16).Index);
            }
            Dictionary<string, object?> data = Map(value, path, "TrueColor", "Ansi16", "Ansi256");
            Color rgb = ReadTerminalColor(RequiredString(Get(data, "TrueColor"), path + ".TrueColor"), path + ".TrueColor");
            int ansi16 = data.TryGetValue("Ansi16", out object? low) ? Integer(low, path + ".Ansi16", -1, 15)
                : rgb.IsDefault ? -1 : rgb.Downsample(Capabilities.ColorDepth.Ansi16).Index;
            int ansi256 = data.TryGetValue("Ansi256", out object? middle) ? Integer(middle, path + ".Ansi256", -1, 255) : -1;
            return new ThemeColor(rgb, ansi16, ansi256);
        }

        private static Color ReadTerminalColor(string text, string path) {
            string value = text.Trim();
            // Color.ToString preserves the distinction between a 16-colour and 256-colour palette entry.
            if (value.StartsWith("ansi16(", StringComparison.OrdinalIgnoreCase) && value.EndsWith(")", StringComparison.Ordinal)) {
                return Color.FromAnsi16(Integer(value.Substring(7, value.Length - 8), path, 0, 15));
            }
            if (!Color.TryParse(value, out Color color)) { throw Error(path, "contains an invalid colour '" + text + "'"); }
            return color;
        }

        private static TextDecoration ReadDecoration(object? value, string path) {
            string text = RequiredString(value, path);
            TextDecoration result = TextDecoration.None;
            foreach (string part in text.Split(',')) {
                string name = part.Trim();
                // Enum.TryParse also accepts numbers and undefined flags; file input accepts only named decorations.
                if (!Enum.TryParse(name, true, out TextDecoration flag) || !Enum.IsDefined(typeof(TextDecoration), flag)
                    || !string.Equals(flag.ToString(), name, StringComparison.OrdinalIgnoreCase)) {
                    throw Error(path, "contains an unknown decoration '" + name + "'");
                }
                result |= flag;
            }
            return result;
        }

        private static Theme Preset(string name) {
            switch (name.Trim().ToLowerInvariant()) {
                case "dark": return Theme.Dark;
                case "light": return Theme.Light;
                case "highcontrast":
                case "high-contrast": return Theme.HighContrast;
                case "colorblindsafe":
                case "color-blind-safe": return Theme.ColorBlindSafe;
                default: throw Error("BaseTheme", "must be Dark, Light, HighContrast or ColorBlindSafe");
            }
        }

        private static Dictionary<string, object?> Map(object? value, string path, params string[] allowed) {
            if (!(value is IDictionary dictionary)) { throw Error(path, "must be an object/data hashtable"); }
            Dictionary<string, object?> map = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (DictionaryEntry pair in dictionary) {
                if (!(pair.Key is string key)) { throw Error(path, "keys must be strings"); }
                if (allowed.Length > 0 && !Array.Exists(allowed, name => string.Equals(name, key, StringComparison.OrdinalIgnoreCase))) {
                    throw Error(path + "." + key, "is an unknown key");
                }
                if (map.ContainsKey(key)) { throw Error(path + "." + key, "is a duplicate key"); }
                map.Add(key, pair.Value);
            }
            return map;
        }

        private static object? Get(Dictionary<string, object?> map, string key) {
            if (!map.TryGetValue(key, out object? value)) { throw Error(key, "is required"); }
            return value;
        }

        private static string RequiredString(object? value, string path) {
            if (!(value is string text) || string.IsNullOrWhiteSpace(text)) { throw Error(path, "must be a nonempty string"); }
            foreach (char c in text) {
                if (char.IsControl(c)) { throw Error(path, "must not contain control characters"); }
            }
            return text;
        }

        private static int Integer(object? value, string path, int minimum, int maximum) {
            string? text = value is string s ? s : value is byte || value is sbyte || value is short || value is ushort
                || value is int || value is uint || value is long || value is ulong ? Convert.ToString(value, CultureInfo.InvariantCulture) : null;
            if (text == null || !int.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int number)
                || number < minimum || number > maximum) {
                throw Error(path, "must be an integer from " + minimum.ToString(CultureInfo.InvariantCulture) + " to " + maximum.ToString(CultureInfo.InvariantCulture));
            }
            return number;
        }

        private static FormatException Error(string path, string message) => new FormatException(path + " " + message + ".");
    }
}
