using System;
using System.IO;
using System.Management.Automation;
using System.Text.RegularExpressions;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Styling;

namespace ConsoleMonster.PowerShell {
    /// <summary>Selects the theme used by every ConsoleMonster widget.</summary>
    /// <remarks><para>Select a built-in preset or catalogue scheme, follow your terminal with Terminal, or load a validated JSON/PSD1 theme. Catalogue schemes use upstream colours faithfully by default; -Accessible chooses colours that pass ThemeCheck. The saved accessible name is Dracula · accessible, for example. Dark suits dark terminals, Light suits light backgrounds and HighContrast uses saturated primaries. ColorBlindSafe keeps pass, warning and fail distinct with red-green colour blindness (blue, yellow and underlined rose) and picks its light variant when it detects a light background; where the background cannot be detected (for example in Windows Terminal) it uses the dark variant, so choose ColorBlindSafeLight there. ColorBlindSafeLight and ColorBlindSafeDark choose one explicitly. To keep a theme in every session, add -Save: it is written to the settings file, which every session and every tool built on ConsoleMonster reads (Set-ConsoleSetting Theme does the same, and Show-ConsoleSetting previews themes before choosing). CONSOLEMONSTER_THEME, for example colorblind, still overrides the file. Files use the built-in token names. PSD1 files are imported as restricted data and never executed. Invalid files leave the current theme unchanged.</para></remarks>
    /// <example>
    /// <para>Use the light theme for this session</para>
    /// <code>Set-ConsoleTheme -Theme Light</code>
    /// </example>
    /// <example>
    /// <para>Use status colours that stay distinct with red-green colour blindness</para>
    /// <code>Set-ConsoleTheme -Theme ColorBlindSafe</code>
    /// </example>
    /// <example>
    /// <para>Keep the colour-blind-safe theme in every new session</para>
    /// <code>Set-ConsoleTheme -Theme ColorBlindSafe -Save</code>
    /// </example>
    /// <example><para>Keep the accessible Dracula scheme in every session</para><code>Set-ConsoleTheme Dracula -Accessible -Save</code></example>
    /// <example>
    /// <para>Load an existing theme exported by Export-ConsoleTheme</para>
    /// <code># Requires an existing team-theme.psd1 file. Changes the active session theme.
    /// Set-ConsoleTheme -Path ./team-theme.psd1</code>
    /// </example>
    [Cmdlet(VerbsCommon.Set, "ConsoleTheme", DefaultParameterSetName = "Name", HelpUri = "https://consolemonster.dev/powershell/commands/set-consoletheme/")]
    public sealed class CmdletSetConsoleTheme : NoPipelineInputCmdlet {
        /// <summary><para type="description">A built-in preset, Terminal, or a catalogue scheme or family. Theme.Schemes lists the ten palettes. Solarized, Gruvbox, Catppuccin and Tokyo Night select light or dark for the detected background (dark when unknown). Explicit names such as Solarized Light, Catppuccin Latte and Tokyo Night Dark choose one variant. Add · accessible to a catalogue name, or use -Accessible.</para></summary>
        [Parameter(Position = 0, Mandatory = true, ParameterSetName = "Name")]
        [Binding.ThemeName, ArgumentCompleter(typeof(Binding.ThemeNameCompleter))]
        [PSDefaultValue(Help = "Dark")]
        public string Theme { get; set; } = "Dark";

        /// <summary><para type="description">A literal .json or .psd1 theme file path. Relative paths use the current PowerShell location. All values are validated before applying the theme.</para></summary>
        [Parameter(Mandatory = true, ParameterSetName = "Path")]
        [ValidateNotNullOrEmpty]
        public string Path { get; set; } = string.Empty;

        /// <summary><para type="description">Also writes the theme to the settings file, so every new session uses it.</para></summary>
        [Parameter]
        [PSDefaultValue(Help = "$false")]
        public SwitchParameter Save { get; set; }

        /// <summary><para type="description">Uses a catalogue scheme's accessible variant, or re-derives a version 3 BaseScheme file while preserving its explicit token overrides. Built-in presets do not have scheme variants.</para></summary>
        [Parameter]
        [PSDefaultValue(Help = "$false")]
        public SwitchParameter Accessible { get; set; }

        /// <inheritdoc />
        protected override void ProcessWithoutPipelineInput() {
            if (ParameterSetName == "Name") {
                string choice = Theme;
                if (Accessible) {
                    if (!Styling.Theme.TrySchemeName(choice, out string name, out _, out _)) {
                        ThrowTerminatingError(new ErrorRecord(new ArgumentException("-Accessible requires a catalogue scheme or a BaseScheme file."), "ConsoleThemeNeedsScheme", ErrorCategory.InvalidArgument, choice));
                        return;
                    }
                    choice = name + ConsoleSettings.AccessibleThemeSuffix;
                }
                Theme selected = string.Equals(Theme, "ColorBlindSafe", StringComparison.OrdinalIgnoreCase)
                    ? (Styling.Theme)Terminal.ResolveTheme(Terminal.Profile.WithThemePreference(ThemePreference.ColorBlind))
                    : Styling.Theme.FromName(choice);
                Remember(choice);
                Styling.Theme.TrySchemeName(choice, out _, out _, out bool family);
                Apply(selected, "Set-ConsoleTheme -Theme " + (choice.Contains(" ") ? "'" + choice + "'" : choice), family ? choice : null);
                return;
            }
            Theme candidate;
            string path;
            try {
                path = SessionState.Path.GetUnresolvedProviderPathFromPSPath(Path);
                candidate = ConsoleThemeDataFile.Load(path).ToTheme(Accessible.IsPresent);
            } catch (Exception ex) when (ex is FormatException || ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException) {
                ThrowTerminatingError(new ErrorRecord(ex, "InvalidConsoleThemeFile", ErrorCategory.InvalidData, Path));
                return;
            }
            Remember(path + (Accessible ? ConsoleSettings.AccessibleThemeSuffix : ""));
            Apply(candidate, "Set-ConsoleTheme -Path '" + QuotePattern.Replace(path, "$0$0") + "'" + (Accessible ? " -Accessible" : ""));
        }

        // The Theme setting follows the choice, so Get-ConsoleSetting reports it; -Save also writes it to the file.
        private void Remember(string value) {
            try {
                ConsoleSettings.Set("Theme", value, Save.IsPresent);
            } catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is InvalidOperationException) {
                ThrowTerminatingError(new ErrorRecord(ex, "ConsoleSettingNotSaved", ErrorCategory.WriteError, ConsoleSettings.FilePath));
            }
        }

        private static void Apply(Theme theme, string profileLine, string? familyChoice = null) {
            // Remember cleared the assigned theme. A family stays a setting so late background detection can select again.
            if (familyChoice == null) { Terminal.Theme = theme; }
            _selected = new Selection(theme, profileLine, familyChoice);
        }

        // The command that recreates the active theme, for Enable-ConsoleFormatting -ProfileSnippet; null when the
        // theme was not chosen with Set-ConsoleTheme or has since been replaced.
        internal static string? ProfileLine {
            get {
                Selection? selected = _selected;
                if (selected == null) { return null; }
                if (selected.FamilyChoice != null) {
                    if (!string.Equals(ConsoleSettings.Get("Theme").Value, selected.FamilyChoice, StringComparison.OrdinalIgnoreCase)) { return null; }
                    return ReferenceEquals(Styling.Theme.FromName(selected.FamilyChoice), Terminal.Theme) ? selected.ProfileLine : null;
                }
                return ReferenceEquals(selected.Theme, Terminal.Theme) ? selected.ProfileLine : null;
            }
        }

        // One immutable reference, written and read atomically, so parallel runspaces never see half of a selection.
        private static volatile Selection? _selected;

        private sealed class Selection {
            internal Selection(ITheme theme, string profileLine, string? familyChoice) { Theme = theme; ProfileLine = profileLine; FamilyChoice = familyChoice; }
            internal ITheme Theme { get; }
            internal string ProfileLine { get; }
            internal string? FamilyChoice { get; }
        }

        // PowerShell treats the typographic single quotes as quote characters too; each one is escaped by doubling it.
        private static readonly Regex QuotePattern = new Regex("['\u2018\u2019\u201A\u201B]");
    }
}
