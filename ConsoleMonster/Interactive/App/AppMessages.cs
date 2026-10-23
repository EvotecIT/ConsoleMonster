using System.Globalization;
using ConsoleMonster.Localization;

namespace ConsoleMonster.Interactive {
    /// <summary>Application messages loaded through the existing localization catalog, with English fallback.</summary>
    internal static class AppMessages {
        internal static string Get(string name, CultureInfo? language = null) {
            return LabelText.Get("App." + name, language);
        }
    }
}
