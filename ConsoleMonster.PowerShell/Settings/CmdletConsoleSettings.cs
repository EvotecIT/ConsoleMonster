using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Threading;
using ConsoleMonster.Interactive;
using ConsoleMonster.PowerShell.Binding;

namespace ConsoleMonster.PowerShell {
    /// <summary>Shows ConsoleMonster's settings: the value of each and where it came from.</summary>
    /// <remarks><para>Lists the theme, colour, glyphs, frames (Border), spacing (Density), status markers, motion, mouse, accessibility, hyperlinks, language and culture settings with their configured value or built-in fallback and source: Session (Set-ConsoleSetting in this session), Environment (a CONSOLEMONSTER_* variable or NO_COLOR), File (the settings file) or Default. This describes preference overrides, not the final themed appearance: without a Density override, Comfortable with source Default can still render with Compact spacing from the theme. Session values beat environment variables, which beat the file, so CI and one-off runs keep working without touching it. The file is settings.json in %APPDATA%\ConsoleMonster on Windows and ~/.config/ConsoleMonster elsewhere; CONSOLEMONSTER_SETTINGS names another file, or none to ignore it. Invalid Density environment values and unusable file entries are ignored and reported as warnings.</para></remarks>
    /// <example>
    ///   <para>Every setting and its source</para>
    ///   <code>Get-ConsoleSetting</code>
    /// </example>
    /// <example>
    ///   <para>One setting</para>
    ///   <code>Get-ConsoleSetting Theme</code>
    /// </example>
    [Cmdlet(VerbsCommon.Get, "ConsoleSetting", HelpUri = "https://consolemonster.dev/powershell/commands/get-consolesetting/")]
    [OutputType(typeof(ConsoleSetting))]
    public sealed class CmdletGetConsoleSetting : NoPipelineInputCmdlet {
        /// <summary><para type="description">The settings to show; all of them when omitted.</para></summary>
        [Parameter(Position = 0)]
        [ArgumentCompleter(typeof(ConsoleSettingNameCompleter))]
        public string[]? Name { get; set; }

        /// <inheritdoc />
        protected override void ProcessWithoutPipelineInput() {
            foreach (string problem in ConsoleSettings.Problems) { WriteWarning("Settings: " + problem); }
            if (Name == null) {
                foreach (ConsoleSetting setting in ConsoleSettings.Current) { WriteObject(ConsoleSettingOutput.Wrap(setting)); }
                return;
            }
            foreach (string name in Name) {
                try {
                    WriteObject(ConsoleSettingOutput.Wrap(ConsoleSettings.Get(name)));
                } catch (ArgumentException ex) {
                    WriteError(new ErrorRecord(ex, "UnknownConsoleSetting", ErrorCategory.ObjectNotFound, name));
                }
            }
        }
    }

    /// <summary>Changes a ConsoleMonster setting for this session, and with -Save for every session.</summary>
    /// <remarks><para>Sets Theme, Color, Glyphs, Border, Density, StatusMarkers, Motion, Mouse, Accessible, Hyperlinks, Language or Culture. Border, Density and StatusMarkers override the theme's own style: the frame around panels, tables and inputs, Comfortable or Compact spacing, and Symbols or Words ([OK] [FAIL] [WARN]) for statuses. Compact reduces default padding and gaps; explicit spacing stays as authored. The value applies at once to everything ConsoleMonster draws in this session, above any CONSOLEMONSTER_* environment variable. -Save also writes it to the settings file, so new sessions and other tools built on ConsoleMonster use it too; nothing needs to be added to your profile. Theme takes a built-in theme, a catalogue scheme or paired family, or the path of a .json or .psd1 theme file (see Export-ConsoleTheme). Store an accessible catalogue choice with its suffix, for example Dracula · accessible. Show-ConsoleSetting chooses settings with a live preview.</para></remarks>
    /// <example>
    ///   <para>Use the colour-blind-safe theme from now on</para>
    ///   <code>Set-ConsoleSetting Theme ColorBlindSafe -Save</code>
    /// </example>
    /// <example>
    ///   <para>Keep animations still and leave the mouse to the terminal, for this session only</para>
    ///   <code>Set-ConsoleSetting Motion Reduced; Set-ConsoleSetting Mouse Off</code>
    /// </example>
    /// <example>
    ///   <para>Square frames and status words instead of symbols, from now on</para>
    ///   <code>Set-ConsoleSetting Border Single -Save; Set-ConsoleSetting StatusMarkers Words -Save</code>
    /// </example>
    /// <example>
    ///   <para>Use a team theme file in every session</para>
    ///   <code># Requires an existing team-theme.json file, for example from Export-ConsoleTheme.
    /// Set-ConsoleSetting Theme ./team-theme.json -Save</code>
    /// </example>
    [Cmdlet(VerbsCommon.Set, "ConsoleSetting", SupportsShouldProcess = true, HelpUri = "https://consolemonster.dev/powershell/commands/set-consolesetting/")]
    public sealed class CmdletSetConsoleSetting : NoPipelineInputCmdlet {
        /// <summary><para type="description">The setting: Theme, Color, Glyphs, Border, Density, StatusMarkers, Motion, Mouse, Accessible, Hyperlinks, Language or Culture.</para></summary>
        [Parameter(Position = 0, Mandatory = true)]
        [ArgumentCompleter(typeof(ConsoleSettingNameCompleter))]
        public string Name { get; set; } = string.Empty;

        /// <summary><para type="description">The value. Get-ConsoleSetting lists the values each setting takes; Theme also takes a theme file path; catalogue choices can end in · accessible. Language and Culture take a culture name such as pl or de-DE.</para></summary>
        [Parameter(Position = 1, Mandatory = true)]
        [ArgumentCompleter(typeof(ConsoleSettingValueCompleter))]
        public string Value { get; set; } = string.Empty;

        /// <summary><para type="description">Also writes the value to the settings file, for every new session.</para></summary>
        [Parameter]
        [PSDefaultValue(Help = "$false")]
        public SwitchParameter Save { get; set; }

        /// <inheritdoc />
        protected override void ProcessWithoutPipelineInput() {
            string value = Value;
            try {
                if (ConsoleSettingOutput.IsThemeFile(Name, value)) { value = ConsoleSettingOutput.LoadThemeFile(this, value); }
                if (!ShouldProcess(Save.IsPresent ? ConsoleSettings.FilePath ?? "the settings file" : "this session", "Set " + Name + " to " + value)) { return; }
                ConsoleSettings.Set(Name, value, Save.IsPresent);
            } catch (ArgumentException ex) {
                ThrowTerminatingError(new ErrorRecord(ex, "InvalidConsoleSetting", ErrorCategory.InvalidArgument, Value));
            } catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is InvalidOperationException || ex is FormatException) {
                ThrowTerminatingError(new ErrorRecord(ex, "ConsoleSettingNotSaved", ErrorCategory.WriteError, ConsoleSettings.FilePath));
            }
            // The value applies now, but in new sessions the environment variable still wins over the file.
            if (Save.IsPresent && ConsoleSettings.Get(Name) is ConsoleSetting setting) {
                foreach (string variable in setting.Variable.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries)) {
                    if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(variable))) {
                        WriteWarning(variable + " is set, so new sessions keep using it instead of the saved " + setting.Name + ".");
                    }
                }
            }
        }
    }

    /// <summary>Undoes Set-ConsoleSetting for this session, and with -Save removes the value from the settings file.</summary>
    /// <remarks><para>Without -Name every setting is reset. Afterwards the next source applies: an environment variable, the settings file, or detection.</para></remarks>
    /// <example>
    ///   <para>Go back to the saved theme</para>
    ///   <code>Reset-ConsoleSetting Theme</code>
    /// </example>
    /// <example>
    ///   <para>Remove every saved setting</para>
    ///   <code>Reset-ConsoleSetting -Save</code>
    /// </example>
    [Cmdlet(VerbsCommon.Reset, "ConsoleSetting", SupportsShouldProcess = true, HelpUri = "https://consolemonster.dev/powershell/commands/reset-consolesetting/")]
    public sealed class CmdletResetConsoleSetting : NoPipelineInputCmdlet {
        /// <summary><para type="description">The settings to reset; all of them when omitted.</para></summary>
        [Parameter(Position = 0)]
        [ArgumentCompleter(typeof(ConsoleSettingNameCompleter))]
        public string[]? Name { get; set; }

        /// <summary><para type="description">Also removes the values from the settings file.</para></summary>
        [Parameter]
        [PSDefaultValue(Help = "$false")]
        public SwitchParameter Save { get; set; }

        /// <inheritdoc />
        protected override void ProcessWithoutPipelineInput() {
            IEnumerable<string> names = Name ?? (IEnumerable<string>)ConsoleSettings.Names;
            foreach (string name in names) {
                try {
                    if (!ShouldProcess(Save.IsPresent ? ConsoleSettings.FilePath ?? "the settings file" : "this session", "Reset " + name)) { continue; }
                    ConsoleSettings.Reset(name, Save.IsPresent);
                } catch (ArgumentException ex) {
                    WriteError(new ErrorRecord(ex, "UnknownConsoleSetting", ErrorCategory.ObjectNotFound, name));
                } catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is InvalidOperationException) {
                    ThrowTerminatingError(new ErrorRecord(ex, "ConsoleSettingNotSaved", ErrorCategory.WriteError, ConsoleSettings.FilePath));
                }
            }
        }
    }

    /// <summary>Chooses ConsoleMonster's settings in the terminal with a live preview.</summary>
    /// <remarks><para>A full-screen picker: each setting on the left, and on the right a preview with a table, statuses, progress, a chart and an error drawn as the highlighted choice would draw them, so themes and glyph sets can be compared before choosing. Save keeps the choices for every session (Set-ConsoleSetting -Save), "Use now" applies them to this session only, and Escape or Cancel changes nothing. A setting that an environment variable sets is shown locked with the variable's name. In a non-interactive session it lists the settings and how to change them instead.</para></remarks>
    /// <example>
    ///   <para>Choose with the preview, then list what applies now and where each value came from</para>
    ///   <code>Show-ConsoleSetting; Get-ConsoleSetting</code>
    /// </example>
    /// <example>
    /// <para>Choose settings and inspect the resulting theme</para>
    /// <code>Show-ConsoleSetting
    /// Get-ConsoleSetting -Name Theme | Show-ConsoleTable
    /// </code>
    /// </example>
    [Cmdlet(VerbsCommon.Show, "ConsoleSetting", HelpUri = "https://consolemonster.dev/powershell/commands/show-consolesetting/")]
    [OutputType(typeof(ConsoleSetting))]
    public sealed class CmdletShowConsoleSetting : NoPipelineInputCmdlet {
        private readonly object _stopGate = new object();
        private CancellationTokenSource? _stop;
        private bool _stopping;

        /// <inheritdoc />
        protected override void EndProcessing() {
            foreach (string problem in ConsoleSettings.Problems) { WriteWarning("Settings: " + problem); }
            InteractiveOptions interactive = PSInteractive.Options(this, null);
            if (interactive.ResolveMode() != InteractionMode.Rich) {
                foreach (ConsoleSetting setting in ConsoleSettings.Current) { WriteObject(ConsoleSettingOutput.Wrap(setting)); }
                WriteInformation(new InformationRecord("Change a setting with Set-ConsoleSetting <Name> <Value> -Save, for example: Set-ConsoleSetting Theme ColorBlindSafe -Save", MyInvocation.MyCommand.Name), new[] { "PSHOST" });
                return;
            }
            CancellationTokenSource stop;
            lock (_stopGate) {
                stop = _stop = new CancellationTokenSource();
                if (_stopping) { stop.Cancel(); }
            }
            try {
                SettingsPicker.Result result = SettingsPicker.Run(new ConsoleAppOptions { Interactive = interactive }, stop.Token);
                try {
                    foreach (string name in SettingsPicker.Apply(result)) { WriteVerbose(name + " changed."); }
                } catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is InvalidOperationException) {
                    ThrowTerminatingError(new ErrorRecord(ex, "ConsoleSettingNotSaved", ErrorCategory.WriteError, ConsoleSettings.FilePath));
                }
            } catch (Exception ex) when (ex is PromptUnavailableException || ex is PromptCanceledException || ex is Input.InputClosedException) {
                PSInteractive.Fail(this, ex, MyInvocation.MyCommand.Name);
                throw;
            } finally {
                lock (_stopGate) {
                    _stop = null;
                    stop.Dispose();
                }
            }
        }

        /// <inheritdoc />
        protected override void StopProcessing() {
            lock (_stopGate) {
                _stopping = true;
                _stop?.Cancel();
            }
            base.StopProcessing();
        }
    }

    internal static class ConsoleSettingOutput {
        // Name, Value and Source by default; Variable, Values and Description stay on the object.
        internal static PSObject Wrap(ConsoleSetting setting) {
            PSObject result = new PSObject(setting);
            result.Members.Add(new PSMemberSet("PSStandardMembers", new PSMemberInfo[] {
                new PSPropertySet("DefaultDisplayPropertySet", new[] { "Name", "Value", "Source" })
            }));
            return result;
        }

        internal static bool IsThemeFile(string name, string value) =>
            string.Equals(name, "Theme", StringComparison.OrdinalIgnoreCase)
            && (value.EndsWith(".json", StringComparison.OrdinalIgnoreCase) || value.EndsWith(".psd1", StringComparison.OrdinalIgnoreCase));

        // Resolves a theme file against the PowerShell location and checks that it loads, so a bad file is never saved.
        internal static string LoadThemeFile(PSCmdlet cmdlet, string value) {
            string path = cmdlet.SessionState.Path.GetUnresolvedProviderPathFromPSPath(value);
            ConsoleThemeDataFile.Load(path).ToTheme();
            return path;
        }

        internal static void UseThemeFileLoader() {
            ConsoleSettings.ThemeFileLoader = path => ConsoleThemeDataFile.Load(path).ToTheme();
        }
    }

    internal sealed class ConsoleSettingNameCompleter : IArgumentCompleter {
        public IEnumerable<CompletionResult> CompleteArgument(string commandName, string parameterName, string wordToComplete, CommandAst commandAst, IDictionary fakeBoundParameters) =>
            WordCompletion.Complete(ConsoleSettings.Names, wordToComplete);
    }

    internal sealed class ConsoleSettingValueCompleter : IArgumentCompleter {
        public IEnumerable<CompletionResult> CompleteArgument(string commandName, string parameterName, string wordToComplete, CommandAst commandAst, IDictionary fakeBoundParameters) {
            if (!(fakeBoundParameters["Name"] is string name)) { return Array.Empty<CompletionResult>(); }
            if (string.Equals(name, "Theme", StringComparison.OrdinalIgnoreCase)) { return Binding.ThemeNameCompleter.Complete(wordToComplete, includeAuto: true); }
            try {
                return WordCompletion.Complete(ConsoleSettings.Get(name).Values, wordToComplete);
            } catch (ArgumentException) {
                return Array.Empty<CompletionResult>();
            }
        }
    }
}
