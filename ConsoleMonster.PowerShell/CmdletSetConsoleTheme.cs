using System;
using System.IO;
using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Styling;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Selects the theme used by every ConsoleMonster widget.</para>
    /// <para type="description">Select a built-in preset or load a validated JSON/PSD1 theme. Dark suits dark terminals, Light suits light backgrounds and HighContrast uses saturated primaries. ColorBlindSafe keeps pass, warning and fail distinct with red-green colour blindness (blue, yellow and underlined rose) and picks its light variant on a light background; ColorBlindSafeLight and ColorBlindSafeDark choose one explicitly. To keep a theme in every session, add the Set-ConsoleTheme line to your profile (Enable-ConsoleFormatting -ProfileSnippet includes it) or set CONSOLEMONSTER_THEME, for example to colorblind. Files use the built-in token names. PSD1 files are imported as restricted data and never executed. Invalid files leave the current theme unchanged.</para>
    /// </summary>
    /// <example>
    /// <para>Use the light theme for this session</para>
    /// <code>Set-ConsoleTheme -Theme Light</code>
    /// </example>
    /// <example>
    /// <para>Use status colours that stay distinct with red-green colour blindness</para>
    /// <code>Set-ConsoleTheme -Theme ColorBlindSafe</code>
    /// </example>
    /// <example>
    /// <para>Load an existing theme exported by Export-ConsoleTheme</para>
    /// <code># Requires an existing team-theme.psd1 file. Changes the active session theme.
    /// Set-ConsoleTheme -Path ./team-theme.psd1</code>
    /// </example>
    [Cmdlet(VerbsCommon.Set, "ConsoleTheme", DefaultParameterSetName = "Name")]
    public sealed class CmdletSetConsoleTheme : NoPipelineInputCmdlet {
        /// <summary><para type="description">The built-in theme: Dark, Light, HighContrast, ColorBlindSafe (light or dark variant from the detected background), ColorBlindSafeLight or ColorBlindSafeDark.</para></summary>
        [Parameter(Position = 0, Mandatory = true, ParameterSetName = "Name")]
        [ValidateSet("Dark", "Light", "HighContrast", "ColorBlindSafe", "ColorBlindSafeLight", "ColorBlindSafeDark")]
        public string Theme { get; set; } = "Dark";

        /// <summary><para type="description">A literal .json or .psd1 theme file path. Relative paths use the current PowerShell location. All values are validated before applying the theme.</para></summary>
        [Parameter(Mandatory = true, ParameterSetName = "Path")]
        [ValidateNotNullOrEmpty]
        public string Path { get; set; } = string.Empty;

        /// <inheritdoc />
        protected override void ProcessWithoutPipelineInput() {
            if (ParameterSetName == "Name") {
                Theme selected = string.Equals(Theme, "ColorBlindSafe", StringComparison.OrdinalIgnoreCase)
                    ? (Styling.Theme)Terminal.ResolveTheme(Terminal.Profile.WithThemePreference(ThemePreference.ColorBlind))
                    : Styling.Theme.FromName(Theme);
                Apply(selected, "Set-ConsoleTheme -Theme " + Theme);
                return;
            }
            try {
                string path = SessionState.Path.GetUnresolvedProviderPathFromPSPath(Path);
                Theme candidate = ConsoleThemeDataFile.Load(path).ToTheme();
                Apply(candidate, "Set-ConsoleTheme -Path '" + path.Replace("'", "''") + "'");
            } catch (Exception ex) when (ex is FormatException || ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException) {
                ThrowTerminatingError(new ErrorRecord(ex, "InvalidConsoleThemeFile", ErrorCategory.InvalidData, Path));
            }
        }

        private static void Apply(Theme theme, string profileLine) {
            Terminal.Theme = theme;
            _selected = new KeyValuePair<ITheme, string>(theme, profileLine);
        }

        // The command that recreates the active theme, for Enable-ConsoleFormatting -ProfileSnippet; null when the
        // theme was not chosen with Set-ConsoleTheme or has since been replaced.
        internal static string? ProfileLine {
            get {
                KeyValuePair<ITheme, string>? selected = _selected;
                return selected.HasValue && ReferenceEquals(selected.Value.Key, Terminal.Theme) ? selected.Value.Value : null;
            }
        }

        private static KeyValuePair<ITheme, string>? _selected;
    }
}
