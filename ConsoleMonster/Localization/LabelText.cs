using System.Globalization;

namespace ConsoleMonster.Localization {
    /// <summary>Reads resource-owned diagnostics through the existing language catalog, with English fallback.</summary>
    internal static class LabelText {
        internal static string Get(string key, CultureInfo? language = null) =>
            LabelCatalog.Entries((language ?? LabelLanguage.Default).TwoLetterISOLanguageName).TryGetValue(key, out string? text)
                ? text : LabelCatalog.Entries("en")[key];
    }
}
