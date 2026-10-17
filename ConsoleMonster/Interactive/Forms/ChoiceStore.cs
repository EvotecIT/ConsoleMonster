using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ConsoleMonster.Interactive {
    /// <summary>
    /// Per-app storage of named presets and (opt-in) remembered choices, in <c>%APPDATA%\ConsoleMonster\&lt;app&gt;\</c> on
    /// Windows and <c>$XDG_CONFIG_HOME/ConsoleMonster/&lt;app&gt;/</c> (or <c>~/.config</c>) elsewhere. Values are plain JSON
    /// (strings, booleans and lists); secrets are never stored.
    /// </summary>
    public sealed class ChoiceStore {
        private const string PresetsFile = "presets.json";
        private const string LastFile = "last.json";

        /// <summary>Creates a store for an app.</summary>
        /// <param name="appName">The app name (a folder name; unsafe characters are replaced).</param>
        /// <param name="root">The folder that holds every app's folder; null uses the per-user default.</param>
        public ChoiceStore(string appName, string? root = null) {
            if (string.IsNullOrWhiteSpace(appName)) {
                throw new ArgumentException("An app name is required.", nameof(appName));
            }
            AppName = appName;
            Folder = Path.Combine(root ?? DefaultRoot(), Safe(appName));
        }

        /// <summary>The app name.</summary>
        public string AppName { get; }

        /// <summary>The folder the files live in.</summary>
        public string Folder { get; }

        /// <summary>The per-user root: the roaming AppData folder on Windows, the XDG config folder elsewhere.</summary>
        /// <returns>The folder.</returns>
        public static string DefaultRoot() {
            if (Interop.WindowsConsole.IsWindows) {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ConsoleMonster");
            }
            string? xdg = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
            string config = string.IsNullOrEmpty(xdg) ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config") : xdg!;
            return Path.Combine(config, "ConsoleMonster");
        }

        /// <summary>The preset names saved for a form.</summary>
        /// <param name="form">The form key (its title by default).</param>
        /// <returns>The names, sorted.</returns>
        public IReadOnlyList<string> PresetNames(string form) {
            List<string> names = new List<string>();
            if (Section(Load(PresetsFile), form) is Dictionary<string, object?> presets) {
                names.AddRange(presets.Keys);
            }
            names.Sort(StringComparer.CurrentCultureIgnoreCase);
            return names;
        }

        /// <summary>Loads a preset.</summary>
        /// <param name="form">The form key.</param>
        /// <param name="name">The preset name.</param>
        /// <returns>The values, or null.</returns>
        public IDictionary<string, object?>? LoadPreset(string form, string name) {
            return Section(Load(PresetsFile), form) is Dictionary<string, object?> presets && presets.TryGetValue(name, out object? values)
                ? values as Dictionary<string, object?>
                : null;
        }

        /// <summary>Saves a preset (replacing one with the same name).</summary>
        /// <param name="form">The form key.</param>
        /// <param name="name">The preset name.</param>
        /// <param name="values">The values (strings, booleans, lists of strings).</param>
        public void SavePreset(string form, string name, IDictionary<string, object?> values) {
            Dictionary<string, object?> root = Load(PresetsFile);
            Dictionary<string, object?> forms = Child(root, "forms");
            Dictionary<string, object?> presets = Child(forms, form);
            presets[name] = new Dictionary<string, object?>(values, StringComparer.Ordinal);
            Save(PresetsFile, root);
        }

        /// <summary>Deletes a preset.</summary>
        /// <param name="form">The form key.</param>
        /// <param name="name">The preset name.</param>
        /// <returns>Whether it existed.</returns>
        public bool DeletePreset(string form, string name) {
            Dictionary<string, object?> root = Load(PresetsFile);
            if (Section(root, form) is Dictionary<string, object?> presets && presets.Remove(name)) {
                Save(PresetsFile, root);
                return true;
            }
            return false;
        }

        /// <summary>Loads the remembered choices of a form.</summary>
        /// <param name="form">The form key.</param>
        /// <returns>The values, or null.</returns>
        public IDictionary<string, object?>? LoadLast(string form) => Section(Load(LastFile), form) as Dictionary<string, object?>;

        /// <summary>Remembers the choices of a form.</summary>
        /// <param name="form">The form key.</param>
        /// <param name="values">The values.</param>
        public void SaveLast(string form, IDictionary<string, object?> values) {
            Dictionary<string, object?> root = Load(LastFile);
            Child(root, "forms")[form] = new Dictionary<string, object?>(values, StringComparer.Ordinal);
            Save(LastFile, root);
        }

        private static object? Section(Dictionary<string, object?> root, string form) {
            return root.TryGetValue("forms", out object? forms) && forms is Dictionary<string, object?> map && map.TryGetValue(form, out object? section) ? section : null;
        }

        private static Dictionary<string, object?> Child(Dictionary<string, object?> parent, string key) {
            if (parent.TryGetValue(key, out object? existing) && existing is Dictionary<string, object?> map) {
                return map;
            }
            Dictionary<string, object?> created = new Dictionary<string, object?>(StringComparer.Ordinal);
            parent[key] = created;
            return created;
        }

        private Dictionary<string, object?> Load(string file) {
            string path = Path.Combine(Folder, file);
            try {
                if (File.Exists(path) && MiniJson.Read(File.ReadAllText(path, Encoding.UTF8)) is Dictionary<string, object?> map) {
                    return map;
                }
            } catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is FormatException) {
                // A damaged or unreadable file is treated as empty; saving replaces it.
            }
            return new Dictionary<string, object?>(StringComparer.Ordinal) { ["version"] = "1" };
        }

        private void Save(string file, Dictionary<string, object?> root) {
            Directory.CreateDirectory(Folder);
            string path = Path.Combine(Folder, file);
            string temp = path + ".tmp";
            File.WriteAllText(temp, MiniJson.Write(root) + "\n", new UTF8Encoding(false));
            if (File.Exists(path)) {
                File.Delete(path);
            }
            File.Move(temp, path);
        }

        private static string Safe(string name) {
            StringBuilder sb = new StringBuilder();
            foreach (char c in name) {
                sb.Append(Array.IndexOf(Path.GetInvalidFileNameChars(), c) >= 0 || c == '.' && sb.Length == 0 ? '_' : c);
            }
            return sb.ToString();
        }
    }
}
