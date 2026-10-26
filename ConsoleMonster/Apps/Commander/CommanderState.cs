using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace ConsoleMonster.Apps.Commander {
    /// <summary>Which panel.</summary>
    public enum CommanderSide {
        /// <summary>The left panel.</summary>
        Left,
        /// <summary>The right panel.</summary>
        Right
    }

    /// <summary>What a panel remembers between runs: its folder, sort and view settings. Never contents or commands.</summary>
    public sealed class PanelState {
        /// <summary>The folder; null opens the home folder.</summary>
        public string? Folder { get; set; }

        /// <summary>The sort column.</summary>
        public PanelSortColumn SortColumn { get; set; } = PanelSortColumn.Name;

        /// <summary>Whether the sort is descending.</summary>
        public bool SortDescending { get; set; }

        /// <summary>Whether hidden files are listed.</summary>
        public bool ShowHidden { get; set; }

        /// <summary>Whether folder sizes are computed in the background. On by default; off suits slow network folders.</summary>
        public bool ComputeFolderSizes { get; set; } = true;
    }

    /// <summary>
    /// The Commander's remembered state, stored per machine (under local application data, never roaming) as a small
    /// <c>key=value</c> file. It records folders and sort settings only: no file contents, names of marked files or
    /// command lines, which can hold secrets.
    /// </summary>
    public sealed class CommanderState {
        /// <summary>The left panel.</summary>
        public PanelState Left { get; set; } = new PanelState();

        /// <summary>The right panel.</summary>
        public PanelState Right { get; set; } = new PanelState();

        /// <summary>The panel that had focus.</summary>
        public CommanderSide ActivePanel { get; set; }

        /// <summary>The per-machine state file: <c>ConsoleMonster/Commander/state.txt</c> under local application data.</summary>
        public static string DefaultPath => Path.Combine(Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ConsoleMonster"), "Commander"), "state.txt");

        /// <summary>Reads state, returning defaults when the file is missing or unreadable; unknown lines are ignored.</summary>
        /// <param name="path">The file; <see cref="DefaultPath"/> when null.</param>
        /// <returns>The state.</returns>
        public static CommanderState Load(string? path = null) {
            CommanderState state = new CommanderState();
            string file = path ?? DefaultPath;
            string[] lines;
            try {
                if (!File.Exists(file)) { return state; }
                lines = File.ReadAllLines(file, Encoding.UTF8);
            } catch (IOException) { return state; } catch (UnauthorizedAccessException) { return state; }
            foreach (string line in lines) {
                int equals = line.IndexOf('=');
                if (line.StartsWith("#", StringComparison.Ordinal) || equals <= 0) { continue; }
                string key = line.Substring(0, equals).Trim();
                string value = line.Substring(equals + 1);
                PanelState? panel = key.StartsWith("left.", StringComparison.Ordinal) ? state.Left : key.StartsWith("right.", StringComparison.Ordinal) ? state.Right : null;
                string name = panel == null ? key : key.Substring(key.IndexOf('.') + 1);
                if (panel == null) {
                    if (name == "active" && TryEnum(value, out CommanderSide side)) { state.ActivePanel = side; }
                    continue;
                }
                switch (name) {
                    case "folder": panel.Folder = value.Length == 0 ? null : value; break;
                    case "sort": if (TryEnum(value, out PanelSortColumn column)) { panel.SortColumn = column; } break;
                    case "descending": panel.SortDescending = value.Trim() == "true"; break;
                    case "hidden": panel.ShowHidden = value.Trim() == "true"; break;
                    case "sizes": panel.ComputeFolderSizes = value.Trim() != "false"; break;
                }
            }
            return state;
        }

        /// <summary>Writes state, replacing the file in one step so a crash never leaves half of it.</summary>
        /// <param name="path">The file; <see cref="DefaultPath"/> when null.</param>
        public void Save(string? path = null) {
            string file = Path.GetFullPath(path ?? DefaultPath);
            string? folder = Path.GetDirectoryName(file);
            if (!string.IsNullOrEmpty(folder)) { Directory.CreateDirectory(folder); }
            StringBuilder text = new StringBuilder("# ConsoleMonster Commander: panel folders and view settings only.\n");
            text.Append("active=").Append(ActivePanel.ToString()).Append('\n');
            foreach (KeyValuePair<string, PanelState> panel in new[] { new KeyValuePair<string, PanelState>("left", Left ?? new PanelState()), new KeyValuePair<string, PanelState>("right", Right ?? new PanelState()) }) {
                // Line breaks can't appear in a folder name we restore from a single line.
                string location = (panel.Value.Folder ?? string.Empty).Replace("\r", string.Empty).Replace("\n", string.Empty);
                text.Append(panel.Key).Append(".folder=").Append(location).Append('\n');
                text.Append(panel.Key).Append(".sort=").Append(panel.Value.SortColumn.ToString()).Append('\n');
                text.Append(panel.Key).Append(".descending=").Append(panel.Value.SortDescending ? "true" : "false").Append('\n');
                text.Append(panel.Key).Append(".hidden=").Append(panel.Value.ShowHidden ? "true" : "false").Append('\n');
                text.Append(panel.Key).Append(".sizes=").Append(panel.Value.ComputeFolderSizes ? "true" : "false").Append('\n');
            }
            string temporary = file + "." + Guid.NewGuid().ToString("N").Substring(0, 8) + ".tmp";
            File.WriteAllText(temporary, text.ToString(), new UTF8Encoding(false));
            try {
                if (File.Exists(file)) { File.Replace(temporary, file, null, true); } else { File.Move(temporary, file); }
            } catch {
                try { File.Delete(temporary); } catch (IOException) { } catch (UnauthorizedAccessException) { }
                throw;
            }
        }

        private static bool TryEnum<T>(string value, out T result) where T : struct {
            bool parsed = Enum.TryParse(value.Trim(), true, out result) && Enum.IsDefined(typeof(T), result);
            if (!parsed) { result = default; }
            return parsed && !int.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out _);
        }
    }
}
