using System;
using System.IO;
using System.Linq;
using System.Management.Automation;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Styling;

namespace ConsoleMonster.PowerShell {
    /// <summary>Checks a theme's text contrast, status colours, markers and critical fill.</summary>
    /// <remarks><para>Checks the active theme, a named preset, or a JSON/PSD1 file without changing settings. Text needs WCAG contrast of 4.5:1 against its own fill or the page background. Pass, warning and fail need CIELAB distance 25 in normal vision and full-severity Machado protanopia, deuteranopia and tritanopia simulations. Markers must differ without colour. Critical may share Fail's hue when it has a distinct fill. The Terminal preset is measured against the startup palette when the terminal answers OSC 4/10/11. Otherwise the actual colours of palette indexes, terminal-default text and terminal-dependent dimming cannot be measured; Unknown prevents a confirmed pass but is not a failure. The output report has Passed and Results properties and a verdict-first default view showing problems and unknowns. Use -Detailed for every check. Dark and Light are not colour-blind-safe presets.</para></remarks>
    /// <example><para>Check the active theme.</para><code>Test-ConsoleTheme</code></example>
    /// <example><para>Check a team theme in CI; any failure stops the script.</para><code>Test-ConsoleTheme -Path ./team-theme.json -Strict</code></example>
    /// <example><para>Inspect individual measurements as objects.</para><code>(Test-ConsoleTheme -Theme Light -Background '#ffffff').Results | Where-Object Outcome -EQ Fail</code></example>
    /// <example><para>Show every measurement.</para><code>Test-ConsoleTheme -Theme Dark -Detailed</code></example>
    [Cmdlet(VerbsDiagnostic.Test, "ConsoleTheme", DefaultParameterSetName = "Name", HelpUri = "https://consolemonster.dev/powershell/commands/test-consoletheme/")]
    [OutputType(typeof(ThemeCheckReport))]
    public sealed class CmdletTestConsoleTheme : NoPipelineInputCmdlet {
        /// <summary><para type="description">A built-in preset, catalogue scheme or family. Catalogue names accept · accessible. Omit both Theme and Path to check the active theme. Paired families and ColorBlindSafe select the detected background; explicit variant names choose one.</para></summary>
        [Parameter(ParameterSetName = "Name")]
        [Binding.ThemeName, ArgumentCompleter(typeof(Binding.ThemeNameCompleter))]
        public string? Theme { get; set; }

        /// <summary><para type="description">A literal .json or restricted .psd1 theme file. Relative paths use the current PowerShell location.</para></summary>
        [Parameter(Position = 0, Mandatory = true, ParameterSetName = "Path")]
        [ValidateNotNullOrEmpty]
        public string Path { get; set; } = string.Empty;

        /// <summary><para type="description">The page colour, as a name or #rrggbb. Defaults to #0c0c0c for a dark preset and #ffffff for a light preset. A token's own background takes precedence. Palette-index backgrounds remain unknown.</para></summary>
        [Parameter]
        [ValidateNotNullOrEmpty]
        public string? Background { get; set; }

        /// <summary><para type="description">Makes any Fail a terminating ConsoleThemeCheckFailed error with the report as its target. Unknown alone does not throw.</para></summary>
        [Parameter]
        [PSDefaultValue(Help = "$false")]
        public SwitchParameter Strict { get; set; }

        /// <summary><para type="description">Shows every measurement instead of only problems and unknowns. The Results property always contains every check.</para></summary>
        [Parameter]
        [PSDefaultValue(Help = "$false")]
        public SwitchParameter Detailed { get; set; }

        private static Color ParseBackground(string text) {
            if (!Color.TryParse(text, out Color colour)) { throw new FormatException("Invalid background colour '" + text + "'."); }
            // A named colour is an explicit colour choice, rather than knowledge of a live terminal palette.
            if (string.Equals(text, "white", StringComparison.OrdinalIgnoreCase)) { return Color.FromHex("#ffffff"); }
            if (string.Equals(text, "black", StringComparison.OrdinalIgnoreCase)) { return Color.FromHex("#000000"); }
            return colour;
        }

        /// <inheritdoc />
        protected override void ProcessWithoutPipelineInput() {
            ITheme selected = Terminal.Theme;
            ThemeCheckReport report;
            try {
                if (ParameterSetName == "Path") {
                    selected = ConsoleThemeDataFile.Load(SessionState.Path.GetUnresolvedProviderPathFromPSPath(Path)).ToTheme();
                } else if (Theme != null) {
                    selected = string.Equals(Theme, "ColorBlindSafe", StringComparison.OrdinalIgnoreCase)
                        ? Terminal.ResolveTheme(Terminal.Profile.WithThemePreference(ThemePreference.ColorBlind)) : Styling.Theme.FromName(Theme);
                }
                report = ThemeCheck.Run(selected, new ThemeCheckOptions {
                    Background = Background == null ? (Color?)null : ParseBackground(Background),
                    GlyphTier = Terminal.Profile.Glyphs,
                    StatusMarkers = Terminal.Profile.StatusMarkers
                });
            } catch (Exception ex) when (ex is FormatException || ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException) {
                ThrowTerminatingError(new ErrorRecord(ex, "InvalidConsoleThemeCheck", ErrorCategory.InvalidData, ParameterSetName == "Path" ? Path : Background));
                return;
            }
            if (Strict && report.Results.Any(result => result.Outcome == ThemeCheckOutcome.Fail)) {
                ThrowTerminatingError(new ErrorRecord(new InvalidOperationException("The theme has failed accessibility checks. Inspect TargetObject.Results for the measurements."),
                    "ConsoleThemeCheckFailed", ErrorCategory.InvalidData, report));
                return;
            }
            PSObject output = new PSObject(report);
            if (Detailed) { output.TypeNames.Insert(0, ThemeCheckView.DetailedTypeName); }
            WriteObject(output);
        }
    }
}
