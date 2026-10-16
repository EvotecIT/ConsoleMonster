using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ConsoleMonster.Interop;
using ConsoleMonster.Output;

namespace ConsoleMonster.Capabilities {

    /// <summary>An immutable snapshot of the process inputs used by capability detection.</summary>
    public sealed class TerminalEnvironment {

        /// <summary>Creates a snapshot, copying variables with the platform's name comparison.</summary>
        /// <param name="environmentVariables">Environment variables; absent entries behave as empty strings.</param>
        /// <param name="isOutputRedirected">Whether stdout is redirected.</param>
        /// <param name="isWindows">Whether the host is Windows.</param>
        /// <param name="windowsBuild">Windows build number, or zero when unknown.</param>
        /// <param name="isConsoleHandle">Whether GetConsoleMode succeeded for stdout.</param>
        /// <param name="virtualTerminalEnabled">Result of enabling VT, or null when not attempted.</param>
        /// <param name="consoleFontFace">Console font face, if available.</param>
        /// <param name="consoleFontIsRaster">Whether the font is raster.</param>
        /// <param name="windowWidth">Visible columns, or zero when unavailable.</param>
        /// <param name="windowHeight">Visible rows, or zero when unavailable.</param>
        public TerminalEnvironment(IReadOnlyDictionary<string, string>? environmentVariables = null,
            bool isOutputRedirected = false, bool isWindows = false, int windowsBuild = 0,
            bool isConsoleHandle = false, bool? virtualTerminalEnabled = null,
            string? consoleFontFace = null, bool consoleFontIsRaster = false,
            int windowWidth = 0, int windowHeight = 0) {
            Dictionary<string, string> copy = new Dictionary<string, string>(isWindows ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
            if (environmentVariables != null) {
                foreach (KeyValuePair<string, string> entry in environmentVariables) {
                    copy[entry.Key] = entry.Value;
                }
            }
            EnvironmentVariables = new ReadOnlyDictionary<string, string>(copy);
            IsOutputRedirected = isOutputRedirected;
            IsWindows = isWindows;
            WindowsBuild = windowsBuild;
            IsConsoleHandle = isConsoleHandle;
            VirtualTerminalEnabled = virtualTerminalEnabled;
            ConsoleFontFace = consoleFontFace;
            ConsoleFontIsRaster = consoleFontIsRaster;
            WindowWidth = windowWidth;
            WindowHeight = windowHeight;
        }

        /// <summary>Copied environment variables, case-insensitive on Windows.</summary>
        public IReadOnlyDictionary<string, string> EnvironmentVariables { get; }

        /// <summary>Whether stdout is redirected.</summary>
        public bool IsOutputRedirected { get; }

        /// <summary>Whether the host is Windows.</summary>
        public bool IsWindows { get; }

        /// <summary>Windows build, or zero when unknown.</summary>
        public int WindowsBuild { get; }

        /// <summary>Whether GetConsoleMode succeeded for stdout.</summary>
        public bool IsConsoleHandle { get; }

        /// <summary>Result of enabling VT, or null when not attempted.</summary>
        public bool? VirtualTerminalEnabled { get; }

        /// <summary>Console font face, when available.</summary>
        public string? ConsoleFontFace { get; }

        /// <summary>Whether the console font is raster.</summary>
        public bool ConsoleFontIsRaster { get; }

        /// <summary>Visible columns, or zero when unavailable.</summary>
        public int WindowWidth { get; }

        /// <summary>Visible rows, or zero when unavailable.</summary>
        public int WindowHeight { get; }

        /// <summary>Captures real process inputs, enabling Windows VT output without changing code pages.</summary>
        /// <returns>A snapshot for deterministic detection.</returns>
        public static TerminalEnvironment Capture() {
            bool windows = WindowsConsole.IsWindows;
            Dictionary<string, string> variables = new Dictionary<string, string>(windows ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
            foreach (DictionaryEntry entry in Environment.GetEnvironmentVariables()) {
                if (entry.Key is string key && entry.Value is string value) {
                    variables[key] = value;
                }
            }
            bool redirected = true;
            try {
                redirected = Console.IsOutputRedirected;
            } catch (Exception) {
                // Hosts without a console use the conservative stream path.
            }
            IntPtr handle = WindowsConsole.StandardOutput;
            bool console = WindowsConsole.TryGetMode(handle, out uint mode);
            bool? vt = console ? WindowsConsole.TrySetMode(handle, mode | WindowsConsole.EnableVirtualTerminalProcessing) : (bool?)null;
            WindowsConsole.TryGetFont(handle, out string? face, out bool raster);
            TerminalSize.TryGet(out int width, out int height);
            return new TerminalEnvironment(variables, redirected, windows, WindowsConsole.BuildNumber(), console, vt, face, raster, width, height);
        }
    }
}
