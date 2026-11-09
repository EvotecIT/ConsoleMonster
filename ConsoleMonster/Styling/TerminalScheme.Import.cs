using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using ConsoleMonster.Serialization;

namespace ConsoleMonster.Styling {
    public sealed partial class TerminalScheme {
        private static readonly string[] WindowsKeys = { "black", "red", "green", "yellow", "blue", "purple", "cyan", "white",
            "brightBlack", "brightRed", "brightGreen", "brightYellow", "brightBlue", "brightPurple", "brightCyan", "brightWhite" };

        /// <summary>Reads a Windows Terminal scheme object, or a settings document containing exactly one schemes entry.</summary>
        /// <remarks>Export a single entry when settings contain several schemes; no scheme is silently selected.</remarks>
        public static TerminalScheme ParseWindowsTerminal(string json) {
            Dictionary<string, object?> data = ImportMap(MiniJson.Read(json, preserveNumberTypes: true));
            if (data.TryGetValue("schemes", out object? schemes)) {
                if (!(schemes is IList list) || list.Count != 1) { throw new FormatException("Import one Windows Terminal schemes entry at a time."); }
                data = ImportMap(list[0]);
            }
            Color[] palette = WindowsKeys.Select(key => ImportColor(data, key)).ToArray();
            return new TerminalScheme(ImportName(data, "name"), palette, ImportColor(data, "foreground"), ImportColor(data, "background"),
                OptionalColor(data, "selectionBackground"), OptionalColor(data, "cursorColor"));
        }

        /// <summary>Reads an XML iTerm2 .itermcolors property list. External entities and DTD resolution are disabled.</summary>
        /// <param name="plist">The XML property list.</param>
        /// <param name="name">The scheme name, usually the file name.</param>
        public static TerminalScheme ParseITermColors(string plist, string name = "Imported iTerm2") {
            XmlReaderSettings settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore, XmlResolver = null, MaxCharactersInDocument = 1024 * 1024 };
            XDocument document;
            using (StringReader text = new StringReader(plist))
            using (XmlReader reader = XmlReader.Create(text, settings)) { document = XDocument.Load(reader); }
            XElement? dictionary = document.Root?.Element("dict");
            if (document.Root?.Name.LocalName != "plist" || dictionary == null) { throw new FormatException("Expected an iTerm2 plist dictionary."); }
            Dictionary<string, XElement> entries = PlistMap(dictionary);
            Color Read(string key) {
                if (!entries.TryGetValue(key, out XElement? value) || value.Name.LocalName != "dict") { throw new FormatException("Missing iTerm2 colour '" + key + "'."); }
                Dictionary<string, XElement> channels = PlistMap(value);
                if (channels.TryGetValue("Color Space", out XElement? space) && space.Value != "sRGB") {
                    throw new FormatException("iTerm2 colours must use sRGB; export the scheme in sRGB before importing.");
                }
                byte Channel(string channel) {
                    if (!channels.TryGetValue(channel + " Component", out XElement? component)
                        || !double.TryParse(component.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double number)
                        || double.IsNaN(number) || number < 0 || number > 1) { throw new FormatException("Invalid iTerm2 " + channel + " component."); }
                    return (byte)Math.Round(number * 255);
                }
                return Color.FromRgb(Channel("Red"), Channel("Green"), Channel("Blue"));
            }
            Color[] palette = Enumerable.Range(0, 16).Select(index => Read("Ansi " + index.ToString(CultureInfo.InvariantCulture) + " Color")).ToArray();
            return new TerminalScheme(name, palette, Read("Foreground Color"), Read("Background Color"),
                entries.ContainsKey("Selection Color") ? Read("Selection Color") : (Color?)null,
                entries.ContainsKey("Cursor Color") ? Read("Cursor Color") : (Color?)null);
        }

        /// <summary>Reads a base16 scheme in flat key: value YAML form, without tags, aliases or nested YAML.</summary>
        public static TerminalScheme ParseBase16(string yaml) {
            Dictionary<string, object?> values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            using (StringReader reader = new StringReader(yaml)) {
                string? line;
                while ((line = reader.ReadLine()) != null) {
                    string text = line.Trim();
                    if (text.Length == 0 || text.StartsWith("#", StringComparison.Ordinal) || text == "---" || text == "...") { continue; }
                    int colon = text.IndexOf(':');
                    if (colon <= 0) { throw new FormatException("Expected flat base16 key: value data."); }
                    string key = text.Substring(0, colon).Trim(), value = text.Substring(colon + 1).Trim();
                    if (value.StartsWith("\"", StringComparison.Ordinal) || value.StartsWith("'", StringComparison.Ordinal)) {
                        char quote = value[0];
                        int end = value.IndexOf(quote, 1);
                        if (end < 0 || (value.Substring(end + 1).Trim().Length > 0 && !value.Substring(end + 1).Trim().StartsWith("#", StringComparison.Ordinal))) { throw new FormatException("Invalid quoted base16 value."); }
                        value = value.Substring(1, end - 1);
                    } else {
                        int comment = value.IndexOf(" #", StringComparison.Ordinal);
                        if (comment >= 0) { value = value.Substring(0, comment).TrimEnd(); }
                    }
                    if (values.ContainsKey(key)) { throw new FormatException("Duplicate base16 key '" + key + "'."); }
                    values.Add(key, value);
                }
            }
            Color B(int index) => ImportColor(values, "base" + index.ToString("X2", CultureInfo.InvariantCulture), bareHex: true);
            // The conventional base16 ANSI mapping; extra base slots fill the bright red/green/yellow/blue/cyan roles.
            int[] map = { 0, 8, 11, 10, 13, 14, 12, 5, 3, 8, 11, 10, 13, 14, 12, 7 };
            // Validate all sixteen base colours, including non-ANSI slots, before importing.
            for (int i = 0; i < 16; i++) { B(i); }
            return new TerminalScheme(ImportName(values, "scheme"), map.Select(B).ToArray(), B(5), B(0));
        }

        /// <summary>Reads a scheme using its .json, .itermcolors, .yaml or .yml extension.</summary>
        public static TerminalScheme Load(string path) {
            string text = File.ReadAllText(path, new System.Text.UTF8Encoding(false, true));
            switch (Path.GetExtension(path).ToLowerInvariant()) {
                case ".json": return ParseWindowsTerminal(text);
                case ".itermcolors": return ParseITermColors(text, Path.GetFileNameWithoutExtension(path));
                case ".yaml": case ".yml": return ParseBase16(text);
                default: throw new FormatException("Expected a Windows Terminal .json, iTerm2 .itermcolors or base16 .yaml/.yml scheme.");
            }
        }

        private static Dictionary<string, XElement> PlistMap(XElement dictionary) {
            XElement[] items = dictionary.Elements().ToArray();
            if (items.Length % 2 != 0) { throw new FormatException("Invalid plist key/value pairs."); }
            Dictionary<string, XElement> result = new Dictionary<string, XElement>(StringComparer.Ordinal);
            for (int i = 0; i < items.Length; i += 2) {
                if (items[i].Name.LocalName != "key" || result.ContainsKey(items[i].Value)) { throw new FormatException("Invalid or duplicate plist key."); }
                result.Add(items[i].Value, items[i + 1]);
            }
            return result;
        }
        private static Dictionary<string, object?> ImportMap(object? value) {
            if (!(value is IDictionary dictionary)) { throw new FormatException("Expected a scheme object."); }
            Dictionary<string, object?> map = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (DictionaryEntry entry in dictionary) {
                if (!(entry.Key is string key) || map.ContainsKey(key)) { throw new FormatException("Invalid or duplicate scheme key."); }
                map.Add(key, entry.Value);
            }
            return map;
        }
        private static string ImportName(Dictionary<string, object?> data, string key) => data.TryGetValue(key, out object? name) && name is string text && !string.IsNullOrWhiteSpace(text)
            ? text : throw new FormatException("A scheme needs '" + key + "'.");
        private static Color ImportColor(Dictionary<string, object?> data, string key, bool bareHex = false) {
            if (!data.TryGetValue(key, out object? value) || !(value is string text)) { throw new FormatException("Missing scheme colour '" + key + "'."); }
            if (bareHex && text.Length == 6) { text = "#" + text; }
            if (text.Length != 7 || !text.StartsWith("#", StringComparison.Ordinal) || !Color.TryParse(text, out Color color) || color.Kind != ColorKind.Rgb) {
                throw new FormatException("Invalid RGB scheme colour '" + key + "'.");
            }
            return color;
        }
        private static Color? OptionalColor(Dictionary<string, object?> data, string key) => data.ContainsKey(key) ? ImportColor(data, key) : (Color?)null;
    }
}
