using System;
using System.Collections.Generic;
using System.Linq;
using ConsoleMonster.Serialization;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Sessions {
    // The standard asciicast theme header preserves terminal palette indexes when a recording is exported later.
    internal static class SessionPalette {
        internal static string Capture(string header) {
            if (!(Terminal.Theme is Theme theme) || !theme.FollowsTerminal) { return header; }
            TerminalScheme scheme = theme.ForExport().Scheme!;
            Dictionary<string, object?> data = (Dictionary<string, object?>)MiniJson.Read(header, preserveNumberTypes: true)!;
            data["theme"] = new Dictionary<string, object?> { ["fg"] = scheme.Foreground.ToString(), ["bg"] = scheme.Background.ToString(),
                ["palette"] = string.Join(":", scheme.Palette.Select(color => color.ToString())) };
            ((Dictionary<string, object?>)data["consolemonster"]!)["paletteDecision"] = Terminal.PaletteDecision;
            return MiniJson.Write(data).Replace("\r", "").Replace("\n", "") + "\n";
        }

        internal static Theme? Read(Dictionary<string, object?> header) {
            if (!header.TryGetValue("theme", out object? value) || !(value is Dictionary<string, object?> data)
                || !data.TryGetValue("palette", out object? paletteValue) || !(paletteValue is string palette)) { return null; }
            string[] entries = palette.Split(':');
            if (entries.Length != 16) { return null; }
            Color Rgb(object? text) {
                if (!(text is string hex) || !Color.TryParse(hex, out Color color) || color.Kind != ColorKind.Rgb) { throw new FormatException("Invalid RGB colour in the recording's theme header."); }
                return color;
            }
            if (!data.TryGetValue("fg", out object? foreground) || !data.TryGetValue("bg", out object? background)) { return null; }
            TerminalScheme scheme = new TerminalScheme("Recorded terminal", entries.Select(entry => Rgb(entry)).ToArray(), Rgb(foreground), Rgb(background));
            ThemeAppearance appearance = Theme.FromScheme(scheme).Appearance;
            return Theme.Terminal.WithAppearance(appearance).ResolvePalette(scheme);
        }
    }
}
