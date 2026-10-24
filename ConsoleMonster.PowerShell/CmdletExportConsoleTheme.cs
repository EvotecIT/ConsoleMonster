using System;
using System.IO;
using System.Management.Automation;
using ConsoleMonster.Styling;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Exports the current ConsoleMonster theme as a starting file for a user or team.</para>
    /// <para type="description">Writes every semantic token, foreground/background colour, explicit fallback tier and decoration as JSON or restricted PowerShell data. The current theme is unchanged. The file extension selects the format; the parent directory must already exist. JSON is UTF-8 without a BOM; PSD1 uses a UTF-8 BOM for Windows PowerShell 5.1.</para>
    /// </summary>
    /// <example>
    /// <para>Export the current theme, edit its token definitions and apply it.</para>
    /// <code>Export-ConsoleTheme -Path ./team-theme.json
    /// Set-ConsoleTheme -Path ./team-theme.json</code>
    /// </example>
    /// <example>
    /// <para>Create a PowerShell data file from the accessible preset and return its file information.</para>
    /// <code>Set-ConsoleTheme -Theme ColorBlindSafe
    /// Export-ConsoleTheme -Path ./accessible.psd1 -PassThru</code>
    /// </example>
    [Cmdlet(VerbsData.Export, "ConsoleTheme", SupportsShouldProcess = true)]
    [OutputType(typeof(FileInfo))]
    public sealed class CmdletExportConsoleTheme : PSCmdlet {
        /// <summary><para type="description">The literal .json or .psd1 destination. An existing file is replaced after validation and ShouldProcess approval.</para></summary>
        [Parameter(Position = 0, Mandatory = true)]
        [ValidateNotNullOrEmpty]
        public string Path { get; set; } = string.Empty;

        /// <summary><para type="description">Returns the written file as a FileInfo. Without this switch the command produces no pipeline output.</para></summary>
        [Parameter]
        public SwitchParameter PassThru { get; set; }

        /// <inheritdoc />
        protected override void ProcessRecord() {
            try {
                string path = SessionState.Path.GetUnresolvedProviderPathFromPSPath(Path);
                ConsoleThemeDataFile.CheckExtension(path);
                ThemeFile document = ThemeFile.FromTheme(Terminal.Theme);
                if (!ShouldProcess(path, "Export current ConsoleMonster theme")) { return; }
                ConsoleThemeDataFile.Save(document, path);
                if (PassThru.IsPresent) { WriteObject(new FileInfo(path)); }
            } catch (Exception ex) when (ex is FormatException || ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException) {
                ThrowTerminatingError(new ErrorRecord(ex, "ConsoleThemeExportFailed", ErrorCategory.WriteError, Path));
            }
        }
    }
}
