using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;

namespace ConsoleMonster.Localization {
    /// <summary>
    /// The built-in translations of <see cref="WidgetLabels"/> and <see cref="Interactive.PromptLabels"/>: one embedded
    /// <c>Labels.&lt;language&gt;.txt</c> per language with <c>Widget.Name = text</c> and <c>Prompt.Name = text</c> lines.
    /// </summary>
    public static class LabelCatalog {
        private static readonly ConcurrentDictionary<string, object> Cache = new ConcurrentDictionary<string, object>(StringComparer.Ordinal);
        private static readonly ConcurrentDictionary<string, IReadOnlyDictionary<string, string>> Files = new ConcurrentDictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.Ordinal);

        /// <summary>The languages with built-in translations besides English (two-letter codes).</summary>
        public static IReadOnlyList<string> Languages { get; } = new[] { "pl", "de", "es", "fr", "it" };

        /// <summary>The translations for a language (two-letter code), or an empty table when it has none.</summary>
        /// <param name="language">The two-letter language code, for example <c>pl</c>.</param>
        /// <returns>Keys such as <c>Widget.Showing</c> to text.</returns>
        public static IReadOnlyDictionary<string, string> Entries(string language) {
            return Files.GetOrAdd(language ?? string.Empty, Load);
        }

        internal static T Get<T>(CultureInfo language, string prefix, Func<T> create, PropertyInfo[] properties) where T : class {
            string code = language.TwoLetterISOLanguageName;
            return (T)Cache.GetOrAdd(prefix + code, _ => Apply(create(), properties, prefix, Entries(code)));
        }

        // Sets every public string property that has a translation; the rest keep their English defaults.
        private static object Apply(object labels, PropertyInfo[] properties, string prefix, IReadOnlyDictionary<string, string> entries) {
            if (entries.Count == 0) {
                return labels;
            }
            foreach (PropertyInfo property in properties) {
                if (property.PropertyType == typeof(string) && property.CanWrite && entries.TryGetValue(prefix + property.Name, out string? text)) {
                    property.SetValue(labels, text, null);
                }
            }
            return labels;
        }

        private static IReadOnlyDictionary<string, string> Load(string code) {
            Dictionary<string, string> entries = new Dictionary<string, string>(StringComparer.Ordinal);
            Stream? stream = typeof(LabelCatalog).Assembly.GetManifestResourceStream("ConsoleMonster.Localization.Labels." + code + ".txt");
            if (stream == null) {
                return entries;
            }
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8)) {
                string? line;
                while ((line = reader.ReadLine()) != null) {
                    if (line.Length == 0 || line[0] == '#') {
                        continue;
                    }
                    int equals = line.IndexOf('=');
                    if (equals <= 0) {
                        continue;
                    }
                    entries[line.Substring(0, equals).Trim()] = line.Substring(equals + 1).Trim().Replace("\\n", "\n");
                }
            }
            return entries;
        }
    }

    /// <summary>The language of built-in labels when a profile does not name one.</summary>
    public static class LabelLanguage {
        /// <summary>
        /// <c>CONSOLEMONSTER_LANGUAGE</c> (a culture name such as <c>pl</c> or <c>de-DE</c>) when set, otherwise the current
        /// UI culture (<c>$PSUICulture</c> in PowerShell).
        /// </summary>
        public static CultureInfo Default {
            get {
                string? name = Environment.GetEnvironmentVariable("CONSOLEMONSTER_LANGUAGE");
                if (!string.IsNullOrWhiteSpace(name)) {
                    try {
                        return CultureInfo.GetCultureInfo(name!.Trim());
                    } catch (CultureNotFoundException) {
                        // An unknown name: fall through to the UI culture.
                    }
                }
                return CultureInfo.CurrentUICulture;
            }
        }
    }
}
