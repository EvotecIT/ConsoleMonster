using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Serialization;

namespace ConsoleMonster.Styling {
    /// <summary>
    /// An immutable, validated theme document. JSON and data-dictionary imports share the same validation;
    /// loading never changes <see cref="Terminal.Theme"/>. PowerShell data-file syntax belongs to the PowerShell adapter.
    /// </summary>
    /// <remarks>
    /// Documents contain SchemaVersion (1, 2 or 3), Name, optional BaseTheme, Tokens and, from version 2, an optional Style
    /// (Border, Density, StatusMarkers and Background; omitted fields inherit from BaseTheme). Keys and token names are
    /// case-insensitive. BaseTheme must name a built-in preset; omitted tokens and style fields inherit from it.
    /// Without BaseTheme, omitted values use terminal defaults. An explicit null foreground/background clears inheritance.
    /// Version 3 also supports an embedded BaseScheme, an Accessible Variant and PaletteIndex colours. Preset exports contain complete token definitions; scheme exports retain the source scheme and explicit overrides.
    /// </remarks>
    public sealed partial class ThemeFile {
        private readonly Theme _theme;

        private ThemeFile(Theme theme) { _theme = theme; }

        /// <summary>The name used when the document is converted to a theme.</summary>
        public string Name => _theme.Name;

        /// <summary>Returns the immutable theme represented by this document without changing the active theme.</summary>
        /// <returns>A complete theme, including resolved inheritance.</returns>
        public Theme ToTheme() => _theme;

        /// <summary>Re-derives an embedded scheme accessibly, preserving explicit file overrides. Other themes have no scheme variant.</summary>
        public Theme ToTheme(bool accessible) {
            if (!accessible || _theme.IsAccessible) { return _theme; }
            if (_theme.Scheme == null) { throw new FormatException("An accessible variant needs a BaseScheme theme file."); }
            Dictionary<string, object?> data = Data(_theme);
            data["Variant"] = "Accessible";
            return Import(data).ToTheme();
        }

        /// <summary>Captures a theme for lossless export.</summary>
        /// <param name="theme">A built-in or custom theme. Custom implementations must be representable by ThemeStyle.</param>
        /// <returns>A validated snapshot independent of subsequent changes to a custom theme.</returns>
        /// <exception cref="ArgumentException">A custom theme uses tier-specific decorations or colours that cannot be represented losslessly.</exception>
        /// <exception cref="FormatException">A name, fallback index or decoration is invalid.</exception>
        public static ThemeFile FromTheme(ITheme theme) {
            if (theme == null) { throw new ArgumentNullException(nameof(theme)); }
            if (theme is Theme following && following.FollowsTerminal) { theme = following.ForExport(); }
            Dictionary<ThemeToken, ThemeStyle> styles = new Dictionary<ThemeToken, ThemeStyle>();
            foreach (ThemeToken token in Theme.AllTokens()) {
                styles.Add(token, theme is Theme concrete ? concrete.Get(token) : Capture(theme, token));
            }
            // Apply the same rules as file import, including validation of programmatically created ThemeColor values.
            Theme snapshot = new Theme(theme.Name, styles, (theme as Theme)?.Appearance);
            if (theme is Theme metadata) { snapshot.WithSchemeMetadata(metadata); }
            return Import(Data(snapshot));
        }

        /// <summary>Parses JSON and validates every key, token, colour, fallback and decoration before returning.</summary>
        /// <param name="json">One JSON theme object. Comments, duplicate keys and trailing content are rejected.</param>
        /// <returns>A validated document.</returns>
        /// <exception cref="FormatException">The JSON or theme data is invalid.</exception>
        public static ThemeFile Parse(string json) {
            object? value = MiniJson.Read(json, preserveNumberTypes: true);
            if (!(value is IDictionary data)) { throw Error("Theme", "must be an object"); }
            return Import(data);
        }

        /// <summary>Reads a UTF-8 JSON theme file without changing global state.</summary>
        /// <param name="path">A file-system path. PSD1 must be read by the PowerShell adapter.</param>
        /// <returns>The validated document.</returns>
        /// <exception cref="FormatException">The document is invalid.</exception>
        public static ThemeFile Load(string path) => Parse(File.ReadAllText(path, new UTF8Encoding(false, true)));

        /// <summary>Validates data supplied by an adapter, such as a safely imported PowerShell hashtable.</summary>
        /// <param name="data">SchemaVersion (optional, defaults to 1), Name, optional BaseTheme, and Tokens.</param>
        /// <returns>A complete, validated document; the input dictionary is not retained or modified.</returns>
        /// <exception cref="FormatException">An unknown key, token, preset or invalid value is present.</exception>
        public static ThemeFile Import(IDictionary data) {
            Dictionary<string, object?> root = Map(data, "Theme", "SchemaVersion", "Name", "BaseTheme", "Tokens", "Style", "BaseScheme", "Variant");
            int schema = root.TryGetValue("SchemaVersion", out object? version) ? Integer(version, "SchemaVersion", 1, 3) : 1;
            if (schema < 2 && root.ContainsKey("Style")) { throw Error("Style", "needs SchemaVersion 2"); }
            if (schema < 3 && (root.ContainsKey("BaseScheme") || root.ContainsKey("Variant"))) { throw Error("BaseScheme/Variant", "needs SchemaVersion 3"); }
            if (root.ContainsKey("BaseTheme") && root.ContainsKey("BaseScheme")) { throw Error("BaseScheme", "cannot be combined with BaseTheme"); }
            string name = RequiredString(Get(root, "Name"), "Name");
            Theme? basis = root.TryGetValue("BaseTheme", out object? baseName) ? Preset(RequiredString(baseName, "BaseTheme")) : null;
            if (root.TryGetValue("BaseScheme", out object? schemeData)) { basis = ReadSchemeBasis(schemeData, root); }
            else if (root.ContainsKey("Variant")) { throw Error("Variant", "requires BaseScheme"); }
            Dictionary<string, object?> tokens = root.TryGetValue("Tokens", out object? tokenData) ? Map(tokenData, "Tokens") : schema == 3 ? new Dictionary<string, object?>() : Map(Get(root, "Tokens"), "Tokens");
            Dictionary<ThemeToken, ThemeStyle> styles = new Dictionary<ThemeToken, ThemeStyle>();
            foreach (ThemeToken token in Theme.AllTokens()) { styles.Add(token, basis?.Get(token) ?? default); }
            HashSet<ThemeToken> specified = new HashSet<ThemeToken>();
            foreach (KeyValuePair<string, object?> pair in tokens) {
                if (!Theme.TryParseToken(pair.Key, out ThemeToken token)) { throw Error("Tokens." + pair.Key, "is an unknown theme token"); }
                if (!specified.Add(token)) { throw Error("Tokens." + pair.Key, "is a duplicate theme token"); }
                styles[token] = ReadStyle(pair.Value, "Tokens." + pair.Key, styles[token]);
                if (schema < 3 && (styles[token].Color?.IsPaletteIndex == true || styles[token].BackgroundColor?.IsPaletteIndex == true)) { throw Error("Tokens." + pair.Key, "PaletteIndex needs SchemaVersion 3"); }
            }
            if (schema < 3 && basis?.FollowsTerminal == true) { throw Error("BaseTheme", "Terminal needs SchemaVersion 3"); }
            ThemeAppearance appearance = ReadAppearance(root.TryGetValue("Style", out object? style) ? style : null, basis?.Appearance ?? ThemeAppearance.Default);
            Theme resolved = new Theme(name, styles, appearance);
            if (basis != null) { resolved.WithSchemeMetadata(basis); }
            if (basis?.Scheme != null) {
                foreach (KeyValuePair<string, object?> pair in tokens) {
                    Theme.TryParseToken(pair.Key, out ThemeToken token);
                    Dictionary<string, object?> fields = Map(pair.Value, "Tokens." + pair.Key);
                    SchemeOverrideFields mask = SchemeOverrideFields.None;
                    if (fields.ContainsKey("Foreground")) { mask |= SchemeOverrideFields.Foreground; }
                    if (fields.ContainsKey("Background")) { mask |= SchemeOverrideFields.Background; }
                    if (fields.ContainsKey("Decoration")) { mask |= SchemeOverrideFields.Decoration; }
                    resolved.SetSchemeOverride(token, mask);
                }
            }
            return new ThemeFile(resolved);
        }

        private static ThemeAppearance ReadAppearance(object? value, ThemeAppearance basis) {
            if (value == null) { return basis; }
            Dictionary<string, object?> style = Map(value, "Style", "Border", "StatusMarkers", "Background", "Density");
            Glyphs.BoxStyle border = style.TryGetValue("Border", out object? b) ? Named(b, "Style.Border", Glyphs.BoxStyle.Rounded, Glyphs.BoxStyle.Single, Glyphs.BoxStyle.Double, Glyphs.BoxStyle.Heavy, Glyphs.BoxStyle.Ascii) : basis.Border;
            StatusMarkerStyle markers = style.TryGetValue("StatusMarkers", out object? m) ? Named(m, "Style.StatusMarkers", StatusMarkerStyle.Symbols, StatusMarkerStyle.Words) : basis.StatusMarkers;
            ThemeBackground background = style.TryGetValue("Background", out object? g) ? Named(g, "Style.Background", ThemeBackground.Dark, ThemeBackground.Light) : basis.Background;
            ThemeDensity density = style.TryGetValue("Density", out object? d) ? Named(d, "Style.Density", ThemeDensity.Comfortable, ThemeDensity.Compact) : basis.Density;
            return new ThemeAppearance(density, border, markers, background);
        }

        private static T Named<T>(object? value, string path, params T[] allowed) where T : struct, Enum {
            string text = RequiredString(value, path);
            foreach (T candidate in allowed) {
                if (string.Equals(candidate.ToString(), text, StringComparison.OrdinalIgnoreCase)) { return candidate; }
            }
            throw Error(path, "must be one of " + string.Join(", ", allowed));
        }

        /// <summary>Returns a complete data-only dictionary suitable for an adapter's native serialization.</summary>
        /// <returns>A fresh dictionary; editing it cannot modify this document.</returns>
        public IDictionary<string, object?> ToData() => Data(_theme);

        /// <summary>Writes complete, indented JSON with every token and explicit fallback tier.</summary>
        /// <returns>The JSON document, ending in a newline.</returns>
        public string ToJson() => MiniJson.Write(Data(_theme)) + "\n";

        /// <summary>Writes this document as UTF-8 JSON without a byte order mark.</summary>
        /// <param name="path">The file to create or replace. The parent directory must already exist.</param>
        /// <remarks>Serialization and validation finish before opening the destination. No global theme is changed.</remarks>
        public void Save(string path) => File.WriteAllText(path, ToJson(), new UTF8Encoding(false));

        private static Dictionary<string, object?> Data(Theme theme) {
            Dictionary<string, object?> tokens = new Dictionary<string, object?>();
            foreach (ThemeToken token in Theme.AllTokens()) {
                ThemeStyle style = theme.Get(token);
                SchemeOverrideFields fields = theme.Scheme == null ? SchemeOverrideFields.All : theme.SchemeOverrides(token);
                if (fields == SchemeOverrideFields.None) { continue; }
                Dictionary<string, object?> item = new Dictionary<string, object?>();
                if ((fields & SchemeOverrideFields.Foreground) != 0) { item["Foreground"] = ColorData(style.Color); }
                if ((fields & SchemeOverrideFields.Background) != 0) { item["Background"] = ColorData(style.BackgroundColor); }
                if ((fields & SchemeOverrideFields.Decoration) != 0) { item["Decoration"] = style.Decoration.ToString(); }
                tokens.Add(token.ToString(), item);
            }
            Dictionary<string, object?> data = new Dictionary<string, object?> { ["SchemaVersion"] = 1, ["Name"] = theme.Name, ["Tokens"] = tokens };
            if (!theme.Appearance.Equals(ThemeAppearance.Default)) {
                // Version 1 files stay version 1; a theme with a style of its own needs version 2.
                data["SchemaVersion"] = 2;
                data["Style"] = new Dictionary<string, object?> {
                    ["Border"] = theme.Appearance.Border.ToString(),
                    ["Density"] = theme.Appearance.Density.ToString(),
                    ["StatusMarkers"] = theme.Appearance.StatusMarkers.ToString(),
                    ["Background"] = theme.Appearance.Background.ToString()
                };
            }
            if (theme.Scheme != null) {
                data["SchemaVersion"] = 3;
                data["BaseScheme"] = new Dictionary<string, object?> { ["Scheme"] = SchemeData(theme.Scheme) };
                if (theme.IsAccessible) { data["Variant"] = "Accessible"; }
            }
            foreach (ThemeToken token in Theme.AllTokens()) {
                ThemeStyle style = theme.Get(token);
                if (style.Color?.IsPaletteIndex == true || style.BackgroundColor?.IsPaletteIndex == true) { data["SchemaVersion"] = 3; break; }
            }
            if (theme.FollowsTerminal) { data["SchemaVersion"] = 3; data["BaseTheme"] = "Terminal"; }
            return data;
        }

        private static object? ColorData(ThemeColor? color) {
            if (!color.HasValue) { return null; }
            if (color.Value.IsPaletteIndex) { return new Dictionary<string, object?> { ["PaletteIndex"] = color.Value.Ansi16 }; }
            return new Dictionary<string, object?> { ["TrueColor"] = color.Value.TrueColor.ToString(), ["Ansi16"] = color.Value.Ansi16, ["Ansi256"] = color.Value.Ansi256 };
        }

        private static ThemeStyle Capture(ITheme theme, ThemeToken token) {
            Style rgb = theme.Resolve(token, ColorDepth.TrueColor);
            Style ansi16 = theme.Resolve(token, ColorDepth.Ansi16);
            Style ansi256 = theme.Resolve(token, ColorDepth.Ansi256);
            ThemeStyle captured = new ThemeStyle(CaptureColor(rgb.Color, ansi16.Color, ansi256.Color),
                CaptureColor(rgb.BackgroundColor, ansi16.BackgroundColor, ansi256.BackgroundColor), rgb.Decoration);
            if (!captured.Resolve(ColorDepth.TrueColor).Equals(rgb) || !captured.Resolve(ColorDepth.Ansi16).Equals(ansi16)
                || !captured.Resolve(ColorDepth.Ansi256).Equals(ansi256) || !theme.Resolve(token, ColorDepth.None).Equals(Style.Plain)) {
                throw new ArgumentException("Theme token '" + token + "' cannot be exported losslessly as a ThemeStyle.", nameof(theme));
            }
            return captured;
        }

        private static ThemeColor? CaptureColor(Color rgb, Color ansi16, Color ansi256) {
            if (rgb.IsDefault && ansi16.IsDefault && ansi256.IsDefault) { return null; }
            if (rgb.Kind == ColorKind.Ansi16 && rgb == ansi16 && rgb == ansi256) { return ThemeColor.PaletteIndex(rgb.Index); }
            return new ThemeColor(rgb, ansi16.IsDefault ? -1 : ansi16.Index, ansi256.IsDefault ? -1 : ansi256.Index);
        }
    }
}
