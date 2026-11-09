using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ConsoleMonster.Styling {
    public sealed partial class ThemeFile {
        private static Theme ReadSchemeBasis(object? value, Dictionary<string, object?> root) {
            bool accessible = false;
            if (root.TryGetValue("Variant", out object? variant)) {
                if (!string.Equals(RequiredString(variant, "Variant"), "Accessible", StringComparison.OrdinalIgnoreCase)) { throw Error("Variant", "must be Accessible"); }
                accessible = true;
            }
            if (value is string name) {
                if (!Theme.TrySchemeName(name, out string canonical, out bool suffix, out _) || suffix) { throw Error("BaseScheme", "must name a catalogue scheme or family; use Variant for Accessible"); }
                return Theme.FromName(canonical + (accessible ? ConsoleSettings.AccessibleThemeSuffix : ""));
            }
            Dictionary<string, object?> basis = Map(value, "BaseScheme", "Scheme");
            Dictionary<string, object?> data = Map(Get(basis, "Scheme"), "BaseScheme.Scheme", "Name", "Palette", "Foreground", "Background", "Selection", "Cursor", "SignatureAccent");
            if (!(Get(data, "Palette") is IList palette) || palette.Count != 16) { throw Error("BaseScheme.Scheme.Palette", "needs sixteen RGB colours"); }
            Color Read(object? color, string key) {
                Color rgb = ReadTerminalColor(RequiredString(color, key), key);
                if (rgb.Kind != ColorKind.Rgb) { throw Error(key, "must be an RGB colour"); }
                return rgb;
            }
            Color? Optional(string key) => data.TryGetValue(key, out object? color) ? Read(color, "BaseScheme.Scheme." + key) : (Color?)null;
            Color[] colors = new Color[16];
            for (int i = 0; i < colors.Length; i++) { colors[i] = Read(palette[i], "BaseScheme.Scheme.Palette[" + i + "]"); }
            TerminalScheme scheme = new TerminalScheme(RequiredString(Get(data, "Name"), "BaseScheme.Scheme.Name"), colors,
                Read(Get(data, "Foreground"), "BaseScheme.Scheme.Foreground"), Read(Get(data, "Background"), "BaseScheme.Scheme.Background"),
                Optional("Selection"), Optional("Cursor"), Optional("SignatureAccent"));
            return Theme.FromScheme(scheme, accessible);
        }

        private static Dictionary<string, object?> SchemeData(TerminalScheme scheme) {
            Dictionary<string, object?> data = new Dictionary<string, object?> { ["Name"] = scheme.Name,
                ["Palette"] = scheme.Palette.Select(color => (object?)color.ToString()).ToArray(),
                ["Foreground"] = scheme.Foreground.ToString(), ["Background"] = scheme.Background.ToString() };
            if (scheme.Selection.HasValue) { data["Selection"] = scheme.Selection.Value.ToString(); }
            if (scheme.Cursor.HasValue) { data["Cursor"] = scheme.Cursor.Value.ToString(); }
            if (scheme.SignatureAccent.HasValue) { data["SignatureAccent"] = scheme.SignatureAccent.Value.ToString(); }
            return data;
        }
    }
}
