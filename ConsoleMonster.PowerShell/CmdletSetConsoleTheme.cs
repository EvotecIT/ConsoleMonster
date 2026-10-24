using System;
using System.IO;
using System.Management.Automation;
using ConsoleMonster.Styling;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Selects the theme used by every ConsoleMonster widget.</para>
    /// <para type="description">Select a built-in preset or load a validated JSON/PSD1 theme. Dark suits dark terminals, Light suits light backgrounds, HighContrast uses saturated primaries and ColorBlindSafe uses blue/orange status contrast. Files use the built-in token names. PSD1 files are imported as restricted data and never executed. Invalid files leave the current theme unchanged.</para>
    /// </summary>
    /// <example>
    ///   <para>Use the light theme</para>
    ///   <code>Set-ConsoleTheme -Theme Light</code>
    /// </example>
    /// <example>
    ///   <para>Load a team theme exported as JSON or a PowerShell data file.</para>
    ///   <code>Set-ConsoleTheme -Path ./team-theme.psd1</code>
    /// </example>
    [Cmdlet(VerbsCommon.Set, "ConsoleTheme", DefaultParameterSetName = "Name")]
    public sealed class CmdletSetConsoleTheme : PSCmdlet {
        /// <summary><para type="description">The built-in theme: Dark, Light, HighContrast or ColorBlindSafe.</para></summary>
        [Parameter(Position = 0, Mandatory = true, ParameterSetName = "Name")]
        [ValidateSet("Dark", "Light", "HighContrast", "ColorBlindSafe")]
        public string Theme { get; set; } = "Dark";

        /// <summary><para type="description">A literal .json or .psd1 theme file path. Relative paths use the current PowerShell location. All values are validated before applying the theme.</para></summary>
        [Parameter(Mandatory = true, ParameterSetName = "Path")]
        [ValidateNotNullOrEmpty]
        public string Path { get; set; } = string.Empty;

        /// <inheritdoc />
        protected override void ProcessRecord() {
            if (ParameterSetName == "Name") {
                Terminal.Theme = Styling.Theme.FromName(Theme);
                return;
            }
            try {
                string path = SessionState.Path.GetUnresolvedProviderPathFromPSPath(Path);
                Theme candidate = ConsoleThemeDataFile.Load(path).ToTheme();
                Terminal.Theme = candidate;
            } catch (Exception ex) when (ex is FormatException || ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException) {
                ThrowTerminatingError(new ErrorRecord(ex, "InvalidConsoleThemeFile", ErrorCategory.InvalidData, Path));
            }
        }
    }
}
