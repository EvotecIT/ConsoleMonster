using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ConsoleMonster.Serialization;

namespace ConsoleMonster.Styling {
    public sealed partial class Theme {
        private static readonly Lazy<IReadOnlyList<ThemeScheme>> Catalogue = new Lazy<IReadOnlyList<ThemeScheme>>(ReadCatalogue);

        /// <summary>The shipped colour schemes, with explicit names, family, background variant and display labels.</summary>
        /// <remarks>Derive either form with FromScheme(entry.Scheme, accessible). Family names select the detected background;
        /// entry.Name always selects that exact variant. Names with " · accessible" select the accessible derivation.</remarks>
        public static IReadOnlyList<ThemeScheme> Schemes => Catalogue.Value;

        // Recognition does not probe the terminal or load settings: settings validation calls it while holding its lock.
        internal static bool TrySchemeName(string? value, out string name, out bool accessible, out bool family) {
            family = false;
            string text = (value ?? string.Empty).Trim();
            accessible = text.EndsWith(ConsoleSettings.AccessibleThemeSuffix, StringComparison.OrdinalIgnoreCase);
            if (accessible) { text = text.Substring(0, text.Length - ConsoleSettings.AccessibleThemeSuffix.Length); }
            foreach (ThemeScheme entry in Schemes) {
                if (string.Equals(text, entry.Name, StringComparison.OrdinalIgnoreCase)) { name = entry.Name; return true; }
                if (string.Equals(text, entry.Family, StringComparison.OrdinalIgnoreCase)) { name = entry.Family; family = true; return true; }
            }
            name = string.Empty;
            return false;
        }

        private static Theme CatalogueTheme(string name, bool accessible) {
            ThemeScheme? entry = Schemes.FirstOrDefault(item => item.Name == name);
            if (entry == null) {
                ThemeScheme[] family = Schemes.Where(item => item.Family == name).ToArray();
                ThemeBackground background = ConsoleMonster.Terminal.DetectLight(ConsoleMonster.Terminal.Profile) ? ThemeBackground.Light : ThemeBackground.Dark;
                entry = family.FirstOrDefault(item => item.Variant == background) ?? family[0];
            }
            return entry.Derive(accessible);
        }

        private static IReadOnlyList<ThemeScheme> ReadCatalogue() {
            List<ThemeScheme> entries = new List<ThemeScheme>();
            System.Reflection.Assembly assembly = typeof(Theme).Assembly;
            foreach (string resource in assembly.GetManifestResourceNames().Where(name => name.StartsWith("ConsoleMonster.Styling.Schemes.", StringComparison.Ordinal) && name.EndsWith(".json", StringComparison.Ordinal)).OrderBy(name => name, StringComparer.Ordinal)) {
                using (Stream stream = assembly.GetManifestResourceStream(resource)!)
                using (StreamReader reader = new StreamReader(stream)) {
                    IDictionary data = (IDictionary)MiniJson.Read(reader.ReadToEnd())!;
                    string family = (string)data["family"]!;
                    foreach (IDictionary item in (IList)data["schemes"]!) {
                        TerminalScheme source = TerminalScheme.ParseWindowsTerminal(MiniJson.Write(item));
                        if (item["signatureAccent"] is string accent) {
                            source = new TerminalScheme(source.Name, source.Palette, source.Foreground, source.Background,
                                source.Selection, source.Cursor, Color.FromHex(accent));
                        }
                        string name = (string)item["name"]!;
                        if (entries.Any(entry => string.Equals(entry.Name, name, StringComparison.OrdinalIgnoreCase))) { throw new FormatException("Duplicate catalogue scheme: " + name); }
                        ThemeBackground variant = (string)item["variant"]! == "Light" ? ThemeBackground.Light : ThemeBackground.Dark;
                        entries.Add(new ThemeScheme(name, family, variant, (string)item["displayName"]!, source));
                    }
                }
            }
            return entries.AsReadOnly();
        }
    }
}
