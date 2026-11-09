using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using ConsoleMonster.Interactive;
using ConsoleMonster.Serialization;
using ConsoleMonster.Styling;

namespace ConsoleMonster {
    /// <summary>Where an effective setting came from, from weakest to strongest.</summary>
    public enum ConsoleSettingSource {
        /// <summary>Detection and the built-in default.</summary>
        Default,
        /// <summary>The user's settings file.</summary>
        File,
        /// <summary>A <c>CONSOLEMONSTER_*</c> environment variable (or <c>NO_COLOR</c>).</summary>
        Environment,
        /// <summary>Set for this process with <see cref="ConsoleSettings.Set"/>.</summary>
        Session
    }

    /// <summary>One effective setting: its value, where it came from and the values it accepts.</summary>
    public sealed class ConsoleSetting {
        internal ConsoleSetting(string name, string value, ConsoleSettingSource source, string variable, IReadOnlyList<string> values, string description) {
            Name = name; Value = value; Source = source; Variable = variable; Values = values; Description = description;
        }

        /// <summary>The value when nothing sets it.</summary>
        public string DefaultValue => Values[0];

        /// <summary>The setting name, as written in the settings file.</summary>
        public string Name { get; }

        /// <summary>The effective value.</summary>
        public string Value { get; }

        /// <summary>Where the value came from.</summary>
        public ConsoleSettingSource Source { get; }

        /// <summary>The environment variable that overrides the settings file.</summary>
        public string Variable { get; }

        /// <summary>The named values it accepts (Theme also takes a theme file path, Language and Culture a culture name).</summary>
        public IReadOnlyList<string> Values { get; }

        /// <summary>What the setting changes.</summary>
        public string Description { get; }

        /// <inheritdoc />
        public override string ToString() => Name + " = " + Value + " (" + Source + ")";
    }

    /// <summary>
    /// The user's ConsoleMonster preferences: theme, colour, glyphs, motion, mouse, language and culture, read from one
    /// settings file so they follow the user into every session and every tool built on ConsoleMonster.
    /// </summary>
    /// <remarks>
    /// Strongest first: an explicit option on the call, this process (<see cref="Set"/>), a <c>CONSOLEMONSTER_*</c>
    /// environment variable, the settings file, then detection. The file is <c>settings.json</c> in
    /// <see cref="ChoiceStore.DefaultRoot"/>; <c>CONSOLEMONSTER_SETTINGS</c> names another file, or <c>none</c> to ignore it.
    /// An invalid entry is ignored and listed in <see cref="Problems"/>, so a bad file never stops a program.
    /// Test and headless terminals can read it too (saved accessibility or language settings reach them); a program or test
    /// that needs fixed output sets <see cref="FilePath"/> to null.
    /// </remarks>
    public static class ConsoleSettings {
        private sealed class Definition {
            internal Definition(string name, string variable, string fallback, string description, string[] values, Func<string, string?>? toVariable = null, Func<string, string?>? fromVariable = null) {
                Name = name; Variable = variable; Fallback = fallback; Description = description; Values = values;
                ToVariable = toVariable ?? (value => string.Equals(value, fallback, StringComparison.OrdinalIgnoreCase) ? null : value.ToLowerInvariant());
                FromVariable = fromVariable ?? (value => Canonical(values, value) ?? value);
            }
            internal string Name { get; }
            internal string Variable { get; }
            internal string Fallback { get; }
            internal string Description { get; }
            internal string[] Values { get; }
            // The environment value that has the setting's effect, or null for "leave it to detection".
            internal Func<string, string?> ToVariable { get; }
            // The setting value an environment variable stands for.
            internal Func<string, string?> FromVariable { get; }
        }

        private static readonly Definition[] Definitions = {
            new Definition("Theme", "CONSOLEMONSTER_THEME", "Auto",
                "The colours: a built-in theme or a .json or .psd1 theme file. Auto follows the terminal background.",
                new[] { "Auto", "Dark", "Light", "HighContrast", "ColorBlindSafe", "ColorBlindSafeLight", "ColorBlindSafeDark" },
                value => {
                    switch (value.ToLowerInvariant()) {
                        case "auto": return null;
                        case "colorblindsafe": return "colorblind";
                        case "colorblindsafelight": return "colorblind-light";
                        case "colorblindsafedark": return "colorblind-dark";
                        default: return IsPath(value) ? null : value.ToLowerInvariant();
                    }
                },
                value => {
                    switch (value.ToLowerInvariant()) {
                        case "colorblind": case "colorblindsafe": case "color-blind-safe": return "ColorBlindSafe";
                        case "colorblind-light": case "colorblindsafe-light": case "color-blind-safe-light": return "ColorBlindSafeLight";
                        case "colorblind-dark": case "colorblindsafe-dark": case "color-blind-safe-dark": return "ColorBlindSafeDark";
                        default: return Canonical(new[] { "Auto", "Dark", "Light", "HighContrast" }, value) ?? value;
                    }
                }),
            new Definition("Color", "CONSOLEMONSTER_COLOR", "Auto",
                "How many colours to use. Auto detects the terminal; None turns colour off.",
                new[] { "Auto", "None", "16", "256", "TrueColor" },
                fromVariable: value => string.Equals(value, "24bit", StringComparison.OrdinalIgnoreCase) ? "TrueColor" : Canonical(new[] { "Auto", "None", "16", "256", "TrueColor" }, value) ?? value),
            new Definition("Glyphs", "CONSOLEMONSTER_GLYPHS", "Auto",
                "Box drawing and symbols: Full, Basic (no emoji-width symbols) or Ascii. Auto detects the font and terminal.",
                new[] { "Auto", "Full", "Basic", "Ascii" }),
            new Definition("Border", "CONSOLEMONSTER_BORDER", "Auto",
                "The frame around panels, tables and inputs. Auto uses the theme's (rounded unless the theme says otherwise).",
                new[] { "Auto", "Rounded", "Single", "Double", "Heavy", "Ascii" }),
            new Definition("Density", "CONSOLEMONSTER_DENSITY", "Comfortable",
                "Spacing chosen by widgets. Compact reduces default padding and gaps; explicit spacing stays as authored.",
                new[] { "Comfortable", "Compact" }),
            new Definition("StatusMarkers", "CONSOLEMONSTER_STATUS_MARKERS", "Auto",
                "How statuses are marked: Symbols (✓ ✗ ▴) or Words ([OK] [FAIL] [WARN]), which suit screen readers. Auto uses the theme's.",
                new[] { "Auto", "Symbols", "Words" }),
            new Definition("Motion", "CONSOLEMONSTER_MOTION", "Full",
                "Reduced keeps spinners, progress and live views still.",
                new[] { "Full", "Reduced" }),
            new Definition("Mouse", "CONSOLEMONSTER_NOMOUSE", "On",
                "Off leaves the mouse to the terminal (selection, scrollback) in apps; every action keeps a key.",
                new[] { "On", "Off" },
                value => string.Equals(value, "Off", StringComparison.OrdinalIgnoreCase) ? "1" : null,
                value => IsTrue(value) ? "Off" : "On"),
            new Definition("Accessible", "CONSOLEMONSTER_ACCESSIBLE", "Off",
                "On uses screen-reader-friendly prompts and apps, and reduced motion.",
                new[] { "Off", "On" },
                value => string.Equals(value, "On", StringComparison.OrdinalIgnoreCase) ? "1" : null,
                value => IsTrue(value) ? "On" : "Off"),
            new Definition("Hyperlinks", "CONSOLEMONSTER_HYPERLINKS", "Auto",
                "Clickable links. Auto uses them in terminals known to support them.",
                new[] { "Auto", "On", "Off" },
                fromVariable: value => IsTrue(value) || string.Equals(value, "on", StringComparison.OrdinalIgnoreCase) ? "On"
                    : value == "0" || string.Equals(value, "false", StringComparison.OrdinalIgnoreCase) || string.Equals(value, "off", StringComparison.OrdinalIgnoreCase) ? "Off" : "Auto"),
            new Definition("Language", "CONSOLEMONSTER_LANGUAGE", "Auto",
                "The language of built-in labels: a culture name such as en, pl or de. Auto follows the UI culture.",
                new[] { "Auto", "en", "de", "es", "fr", "it", "pl" },
                value => string.Equals(value, "Auto", StringComparison.OrdinalIgnoreCase) ? null : value),
            new Definition("Culture", "CONSOLEMONSTER_CULTURE", "Current",
                "How numbers, sizes and dates are written: a culture name such as en-US, Invariant, or Current.",
                new[] { "Current", "Invariant", "en-US", "en-GB", "pl-PL", "de-DE" },
                value => string.Equals(value, "Current", StringComparison.OrdinalIgnoreCase) ? null : value)
        };

        private static readonly object Gate = new object();
        private static readonly Dictionary<string, string> SessionValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static string? _filePath = DefaultFilePath();
        private static bool _loaded;
        private static Dictionary<string, object?> _file = NewFile();
        private static List<string> _problems = new List<string>();
        private static string? _themePath;
        private static ITheme? _themeFromPath;

        /// <summary>The setting names, in the order the settings picker shows them.</summary>
        public static IReadOnlyList<string> Names {
            get {
                string[] names = new string[Definitions.Length];
                for (int i = 0; i < names.Length; i++) { names[i] = Definitions[i].Name; }
                return names;
            }
        }

        /// <summary>
        /// The settings file that is read and saved, or null to ignore it. Defaults to <c>CONSOLEMONSTER_SETTINGS</c>, or
        /// <c>settings.json</c> in <see cref="ChoiceStore.DefaultRoot"/>.
        /// </summary>
        public static string? FilePath {
            get { lock (Gate) { return _filePath; } }
            set {
                lock (Gate) { _filePath = value; _loaded = false; }
                Terminal.ResetSettings();
            }
        }

        /// <summary>The default settings file path, or null when <c>CONSOLEMONSTER_SETTINGS</c> is <c>none</c>.</summary>
        /// <returns>The path.</returns>
        public static string? DefaultFilePath() {
            string? configured = Environment.GetEnvironmentVariable("CONSOLEMONSTER_SETTINGS");
            if (!string.IsNullOrWhiteSpace(configured)) {
                return string.Equals(configured!.Trim(), "none", StringComparison.OrdinalIgnoreCase) ? null : configured.Trim();
            }
            return Path.Combine(ChoiceStore.DefaultRoot(), "settings.json");
        }

        /// <summary>Every setting with its effective value and source.</summary>
        public static IReadOnlyList<ConsoleSetting> Current {
            get {
                List<ConsoleSetting> settings = new List<ConsoleSetting>(Definitions.Length);
                lock (Gate) {
                    EnsureLoaded();
                    foreach (Definition definition in Definitions) { settings.Add(Effective(definition)); }
                }
                return settings;
            }
        }

        /// <summary>Entries in the settings file that were ignored, with the reason.</summary>
        public static IReadOnlyList<string> Problems {
            get { lock (Gate) { EnsureLoaded(); return _problems.ToArray(); } }
        }

        /// <summary>One effective setting.</summary>
        /// <param name="name">The setting name (case-insensitive).</param>
        /// <returns>The setting.</returns>
        /// <exception cref="ArgumentException">There is no such setting.</exception>
        public static ConsoleSetting Get(string name) {
            Definition definition = Find(name);
            lock (Gate) { EnsureLoaded(); return Effective(definition); }
        }

        /// <summary>
        /// Sets a value for this process, above environment variables, or with <paramref name="save"/> writes it to the
        /// settings file (and still applies it now where an environment variable would otherwise win).
        /// </summary>
        /// <param name="name">The setting name.</param>
        /// <param name="value">One of the setting's values (Theme also takes a theme file path; Language and Culture a culture name).</param>
        /// <param name="save">Also write it to the settings file.</param>
        /// <exception cref="ArgumentException">The name or value is not valid.</exception>
        /// <exception cref="IOException">The file could not be written, or holds invalid JSON that saving would lose.</exception>
        public static void Set(string name, string value, bool save = false) {
            Definition definition = Find(name);
            string normalized = Validate(definition, value)
                ?? throw new ArgumentException("'" + value + "' is not a valid " + definition.Name + ". Use one of: " + string.Join(", ", definition.Values) + ".");
            lock (Gate) {
                if (save) { Write(definition.Name, normalized); }
                // A saved value is the file's; this process holds it too only where an environment variable would win.
                if (!save || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(definition.Variable)) || HasEnvironmentOverride(definition)) {
                    SessionValues[definition.Name] = normalized;
                } else {
                    SessionValues.Remove(definition.Name);
                }
            }
            Terminal.ResetSettings(definition.Name == "Theme");
        }

        /// <summary>Removes the value set for this process, and optionally from the file, so the next source applies.</summary>
        /// <param name="name">The setting name, or null for every setting.</param>
        /// <param name="save">Also remove it from the settings file.</param>
        public static void Reset(string? name = null, bool save = false) {
            Definition[] targets = name == null ? Definitions : new[] { Find(name) };
            lock (Gate) {
                foreach (Definition definition in targets) {
                    SessionValues.Remove(definition.Name);
                    if (save) { Write(definition.Name, null); }
                }
            }
            Terminal.ResetSettings(Array.Exists(targets, definition => definition.Name == "Theme"));
        }

        /// <summary>Re-reads the settings file, for example after it was edited by hand.</summary>
        public static void Reload() {
            lock (Gate) { _loaded = false; }
            Terminal.ResetSettings();
        }

        // A section of the file other than the settings above (Formatting, read by the PowerShell module), or null.
        internal static object? Section(string name) {
            lock (Gate) {
                EnsureLoaded();
                return _file.TryGetValue(name, out object? value) ? value : null;
            }
        }

        internal static void SaveSection(string name, object? value) {
            lock (Gate) { Write(name, value); }
        }

        // Loads theme files named by the Theme setting. The PowerShell module replaces it to read .psd1 files as well.
        internal static Func<string, ITheme> ThemeFileLoader { get; set; } = path => ThemeFile.Load(path).ToTheme();

        /// <summary>
        /// The value an environment variable has once settings are applied: a session or file value stands in for it
        /// (session above the variable, file below it). Null when nothing sets it.
        /// </summary>
        internal static string? Variable(string variable) {
            string? environment = Environment.GetEnvironmentVariable(variable);
            Definition? definition = ByVariable(variable);
            if (definition == null) { return environment; }
            lock (Gate) {
                EnsureLoaded();
                if (SessionValues.TryGetValue(definition.Name, out string? session)) { return definition.ToVariable(session); }
                if (!string.IsNullOrEmpty(environment) || HasEnvironmentOverride(definition)) { return environment; }
                return FileValue(definition) is string file ? definition.ToVariable(file) : null;
            }
        }

        // Applies session and file values to a captured environment, so detection sees them as variables.
        internal static void Overlay(IDictionary<string, string> variables) {
            lock (Gate) {
                EnsureLoaded();
                foreach (Definition definition in Definitions) {
                    string? value;
                    if (SessionValues.TryGetValue(definition.Name, out string? session)) {
                        value = definition.ToVariable(session);
                    } else if (variables.TryGetValue(definition.Variable, out string? existing) && !string.IsNullOrEmpty(existing)
                        || definition.Name == "Color" && variables.TryGetValue("NO_COLOR", out string? noColor) && !string.IsNullOrEmpty(noColor)) {
                        continue;
                    } else if (FileValue(definition) is string file) {
                        value = definition.ToVariable(file);
                    } else {
                        continue;
                    }
                    if (value == null) { variables.Remove(definition.Variable); } else { variables[definition.Variable] = value; }
                }
            }
        }

        // The theme from a theme file path in the Theme setting, when that path is what applies; null otherwise.
        internal static ITheme? ThemeFromPath() {
            string? path;
            lock (Gate) {
                EnsureLoaded();
                ConsoleSetting theme = Effective(Definitions[0]);
                path = theme.Source != ConsoleSettingSource.Environment && IsPath(theme.Value) ? theme.Value : null;
                if (path == null) { return null; }
                if (string.Equals(path, _themePath, StringComparison.Ordinal)) { return _themeFromPath; }
            }
            ITheme? loaded = null;
            string? problem = null;
            try {
                loaded = ThemeFileLoader(path);
            } catch (Exception ex) when (!(ex is OutOfMemoryException) && !(ex is StackOverflowException)) {
                // Any loader failure (a bad file, or a .psd1 read where no PowerShell runspace is available) leaves the
                // theme to the other sources: a saved setting never stops a program.
                problem = "Theme: ignored theme file '" + path + "': " + ex.Message;
            }
            lock (Gate) {
                _themePath = path;
                _themeFromPath = loaded;
                if (problem != null && !_problems.Contains(problem)) { _problems.Add(problem); }
                return loaded;
            }
        }

        private static ConsoleSetting Effective(Definition definition) {
            ConsoleSettingSource source = ConsoleSettingSource.Default;
            string value = definition.Fallback;
            string? environment = Environment.GetEnvironmentVariable(definition.Variable);
            if (SessionValues.TryGetValue(definition.Name, out string? session)) {
                value = session;
                source = ConsoleSettingSource.Session;
            } else if (!string.IsNullOrEmpty(environment)) {
                value = definition.FromVariable(environment!.Trim()) ?? environment;
                source = ConsoleSettingSource.Environment;
            } else if (HasEnvironmentOverride(definition)) {
                value = "None";
                source = ConsoleSettingSource.Environment;
            } else if (FileValue(definition) is string file) {
                value = file;
                source = ConsoleSettingSource.File;
            }
            return new ConsoleSetting(definition.Name, value, source, definition.Name == "Color" ? "CONSOLEMONSTER_COLOR, NO_COLOR" : definition.Variable, definition.Values, definition.Description);
        }

        // NO_COLOR is the environment's colour choice too, so a file value never turns colour back on.
        private static bool HasEnvironmentOverride(Definition definition) =>
            definition.Name == "Color" && !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("NO_COLOR"));

        private static string? FileValue(Definition definition) =>
            _file.TryGetValue(definition.Name, out object? value) && value is string text ? text : null;

        private static void EnsureLoaded() {
            if (_loaded) { return; }
            _loaded = true;
            _file = NewFile();
            _problems = new List<string>();
            _themePath = null;
            _themeFromPath = null;
            string? path = _filePath;
            if (path == null || !File.Exists(path)) { return; }
            object? data;
            try {
                data = MiniJson.Read(File.ReadAllText(path, Encoding.UTF8));
            } catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is FormatException) {
                _problems.Add("ignored the settings file '" + path + "': " + ex.Message);
                return;
            }
            if (!(data is Dictionary<string, object?> map)) {
                _problems.Add("ignored the settings file '" + path + "': it must hold one JSON object");
                return;
            }
            foreach (KeyValuePair<string, object?> entry in map) {
                Definition? definition = ByName(entry.Key);
                if (definition == null) {
                    // Sections owned by other parts (Formatting) and keys from newer versions are kept as they are.
                    _file[entry.Key] = entry.Value;
                    continue;
                }
                string? text = entry.Value is bool flag ? (flag ? "On" : "Off") : entry.Value as string;
                string? normalized = text == null ? null : Validate(definition, text);
                if (normalized == null) {
                    _problems.Add(definition.Name + ": ignored invalid value '" + (entry.Value ?? "null") + "'");
                    continue;
                }
                _file[definition.Name] = normalized;
            }
        }

        // Writes one key, keeping every other key and section in the file as it is.
        private static void Write(string key, object? value) {
            string path = _filePath ?? throw new InvalidOperationException("No settings file is in use (ConsoleSettings.FilePath is null or CONSOLEMONSTER_SETTINGS is 'none').");
            Dictionary<string, object?> root = NewFile();
            if (File.Exists(path)) {
                object? data;
                try {
                    data = MiniJson.Read(File.ReadAllText(path, Encoding.UTF8));
                } catch (FormatException ex) {
                    throw new IOException("The settings file '" + path + "' is not valid JSON, and saving would replace it. Fix or remove it first. " + ex.Message, ex);
                }
                if (!(data is Dictionary<string, object?> existing)) {
                    throw new IOException("The settings file '" + path + "' does not hold a JSON object, and saving would replace it. Fix or remove it first.");
                }
                foreach (KeyValuePair<string, object?> entry in existing) { root[entry.Key] = entry.Value; }
            }
            root["SchemaVersion"] = 1;
            if (value == null) { root.Remove(key); } else { root[key] = value; }
            string? folder = Path.GetDirectoryName(Path.GetFullPath(path));
            if (!string.IsNullOrEmpty(folder)) { Directory.CreateDirectory(folder); }
            string temp = path + ".tmp";
            File.WriteAllText(temp, MiniJson.Write(root) + "\n", new UTF8Encoding(false));
            // Replace in one step, so a crash never leaves the user without the file.
            if (File.Exists(path)) { File.Replace(temp, path, null); } else { File.Move(temp, path); }
            _loaded = false;
        }

        private static Dictionary<string, object?> NewFile() => new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        // The canonical value, or null when it is not valid for the setting.
        private static string? Validate(Definition definition, string value) {
            string trimmed = (value ?? string.Empty).Trim();
            if (trimmed.Length == 0) { return null; }
            string? named = Canonical(definition.Values, trimmed);
            if (named != null) { return named; }
            switch (definition.Name) {
                case "Theme":
                    return IsPath(trimmed) ? Path.GetFullPath(trimmed) : null;
                case "Language":
                case "Culture":
                    if (definition.Name == "Culture" && string.Equals(trimmed, "Invariant", StringComparison.OrdinalIgnoreCase)) { return "Invariant"; }
                    try { return CultureInfo.GetCultureInfo(trimmed).Name is string culture && culture.Length > 0 ? culture : null; } catch (CultureNotFoundException) { return null; }
                case "Color":
                    return string.Equals(trimmed, "24bit", StringComparison.OrdinalIgnoreCase) ? "TrueColor" : null;
                default:
                    return null;
            }
        }

        private static string? Canonical(string[] values, string value) {
            foreach (string candidate in values) {
                if (string.Equals(candidate, value, StringComparison.OrdinalIgnoreCase)) { return candidate; }
            }
            return null;
        }

        private static bool IsPath(string value) =>
            value.EndsWith(".json", StringComparison.OrdinalIgnoreCase) || value.EndsWith(".psd1", StringComparison.OrdinalIgnoreCase);

        private static bool IsTrue(string value) => value == "1" || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) || string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase);

        private static Definition Find(string name) =>
            ByName(name) ?? throw new ArgumentException("There is no setting named '" + name + "'. Settings: " + string.Join(", ", Names) + ".");

        private static Definition? ByName(string name) {
            foreach (Definition definition in Definitions) {
                if (string.Equals(definition.Name, name, StringComparison.OrdinalIgnoreCase)) { return definition; }
            }
            return null;
        }

        private static Definition? ByVariable(string variable) {
            foreach (Definition definition in Definitions) {
                if (string.Equals(definition.Variable, variable, StringComparison.OrdinalIgnoreCase)) { return definition; }
            }
            return null;
        }
    }
}
