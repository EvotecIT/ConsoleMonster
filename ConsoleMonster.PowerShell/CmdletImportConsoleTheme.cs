using System;
using System.IO;
using System.Management.Automation;
using System.Xml;
using ConsoleMonster.Styling;

namespace ConsoleMonster.PowerShell {
    /// <summary>Imports a terminal colour scheme as a version 3 ConsoleMonster theme.</summary>
    /// <remarks><para>Reads a single Windows Terminal scheme object (or a settings.json with one schemes entry), an XML iTerm2 .itermcolors file, or a flat base16 YAML scheme. The generated JSON embeds the scheme so its roles are derived when loaded. Accessible first chooses status colours from the palette, then adjusts OKLCH lightness where needed. The command writes the file and returns Test-ConsoleTheme's report and verdict lines, including adjusted tokens; failing checks never prevent import. It does not change the active theme or settings.</para></remarks>
    /// <example><para>Import a Windows Terminal scheme and inspect its verdict.</para><code># Requires an existing Windows Terminal scheme JSON file.
    /// Import-ConsoleTheme -Path ./my-scheme.json -OutFile ./my-theme.json</code></example>
    /// <example><para>Import an accessible variant, then use it.</para><code># Requires an existing sRGB iTerm2 .itermcolors file.
    /// Import-ConsoleTheme -Path ./my-scheme.itermcolors -Accessible -OutFile ./my-theme.json
    /// Set-ConsoleTheme -Path ./my-theme.json</code></example>
    [Cmdlet(VerbsData.Import, "ConsoleTheme", SupportsShouldProcess = true, HelpUri = "https://consolemonster.dev/powershell/commands/import-consoletheme/")]
    [OutputType(typeof(ThemeCheckReport))]
    public sealed class CmdletImportConsoleTheme : NoPipelineInputCmdlet {
        /// <summary><para type="description">A literal .json, .itermcolors, .yaml or .yml scheme file. Relative paths use the current PowerShell location.</para></summary>
        [Parameter(Position = 0, Mandatory = true)]
        [ValidateNotNullOrEmpty]
        public string Path { get; set; } = string.Empty;

        /// <summary><para type="description">Chooses the accessible variant and records tokens adjusted for contrast or colour-vision separation.</para></summary>
        [Parameter]
        [PSDefaultValue(Help = "$false")]
        public SwitchParameter Accessible { get; set; }

        /// <summary><para type="description">The destination JSON theme file. Defaults to the source directory and base name with .theme.json. The source file cannot be overwritten.</para></summary>
        [Parameter]
        [ValidateNotNullOrEmpty]
        public string? OutFile { get; set; }

        /// <inheritdoc />
        protected override void ProcessWithoutPipelineInput() {
            try {
                string input = SessionState.Path.GetUnresolvedProviderPathFromPSPath(Path);
                string output = OutFile == null ? System.IO.Path.Combine(System.IO.Path.GetDirectoryName(input)!, System.IO.Path.GetFileNameWithoutExtension(input) + ".theme.json")
                    : SessionState.Path.GetUnresolvedProviderPathFromPSPath(OutFile);
                if (!string.Equals(System.IO.Path.GetExtension(output), ".json", StringComparison.OrdinalIgnoreCase)) { throw new FormatException("Imported themes use the .json extension."); }
                if (string.Equals(input, output, StringComparison.OrdinalIgnoreCase)) { throw new FormatException("OutFile must differ from the source scheme."); }
                Theme theme = Styling.Theme.FromScheme(TerminalScheme.Load(input), Accessible.IsPresent);
                ThemeFile document = ThemeFile.FromTheme(theme);
                ThemeCheckReport report = ThemeCheck.Run(theme);
                if (!ShouldProcess(output, "Write imported ConsoleMonster theme")) { return; }
                document.Save(output);
                PSObject result = new PSObject(report);
                result.Properties.Add(new PSNoteProperty("Path", output));
                WriteObject(result);
            } catch (Exception ex) when (ex is FormatException || ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException || ex is XmlException) {
                ThrowTerminatingError(new ErrorRecord(ex, "ConsoleThemeImportFailed", ErrorCategory.InvalidData, Path));
            }
        }
    }
}
