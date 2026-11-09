using System.Collections.Generic;
using System.Linq;

namespace ConsoleMonster.Styling {
    // Shared wording for the library's verdicts, the report view and the settings preview.
    internal static class ThemeCheckText {
        internal static string Pair(IEnumerable<ThemeToken> tokens, bool sentence = true) {
            string text = Join(tokens.Select(token => token == ThemeToken.Warn ? "warning" : token.ToString().ToLowerInvariant()));
            return sentence && text.Length > 0 ? char.ToUpperInvariant(text[0]) + text.Substring(1) : text;
        }

        internal static string Join(IEnumerable<string> words) {
            string[] items = words.ToArray();
            return items.Length < 2 ? string.Join(string.Empty, items)
                : string.Join(", ", items.Take(items.Length - 1)) + " and " + items[items.Length - 1];
        }

        internal static string Conditions(IEnumerable<string?> visions) {
            string[] names = visions.Where(name => name != null).Select(name => name!).Distinct().ToArray();
            bool normal = names.Contains("normal vision");
            string[] simulations = names.Where(name => name != "normal vision").ToArray();
            return (normal ? "in normal vision" : string.Empty)
                + (normal && simulations.Length > 0 ? " and " : string.Empty)
                + (simulations.Length > 0 ? "with " + Join(simulations) : string.Empty);
        }
    }
}
