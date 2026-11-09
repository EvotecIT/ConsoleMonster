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
    /// <summary>
    /// <para type="synopsis">Shows ConsoleMonster's settings: the value of each and where it came from.</para>
    /// <para type="description">Lists the theme, colour, glyphs, motion, mouse, accessibility, hyperlinks, language and culture settings with their effective value and source: Session (Set-ConsoleSetting in this session), Environment (a CONSOLEMONSTER_* variable or NO_COLOR), File (the settings file) or Default. Session values beat environment variables, which beat the file, so CI and one-off runs keep working without touching it. The file is settings.json in %APPDATA%\ConsoleMonster on Windows and ~/.config/ConsoleMonster elsewhere; CONSOLEMONSTER_SETTINGS names another file, or none to ignore it. Entries in the file that cannot be used are ignored and reported as warnings.</para>
    /// </summary>
    /// <example>
    ///   <para>Every setting and its source</para>
    ///   <code>Get-ConsoleSetting</code>
    /// </example>
    /// <example>
    ///   <para>One setting</para>
    ///   <code>Get-ConsoleSetting Theme</code>
    /// </example>
    [Cmdlet(VerbsCommon.Get, "ConsoleSetting")]
    [OutputType(typeof(ConsoleSetting))]
    public sealed class CmdletGetConsoleSetting : NoPipelineInputCmdlet {
        /// <summary><para type="description">The settings to show; all of them when omitted.</para></summary>
        [Parameter(Position = 0)]
        [ArgumentCompleter(typeof(ConsoleSettingNameCompleter))]
        public string[]? Name { get; set; }

        /// <inheritdoc />
        protected override void ProcessWithoutPipelineInput() {
            foreach (string problem in ConsoleSettings.Problems) { WriteWarning("Settings file: " + problem); }
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

    /// <summary>
    /// <para type="synopsis">Changes a ConsoleMonster setting for this session, and with -Save for every session.</para>
    /// <para type="description">Sets Theme, Color, Glyphs, Motion, Mouse, Accessible, Hyperlinks, Language or Culture. The value applies at once to everything ConsoleMonster draws in this session, above any CONSOLEMONSTER_* environment variable. -Save also writes it to the settings file, so new sessions and other tools built on ConsoleMonster use it too; nothing needs to be added to your profile. Theme takes a built-in theme or the path of a .json or .psd1 theme file (see Export-ConsoleTheme). Show-ConsoleSettings chooses settings with a live preview.</para>
    /// </summary>
    /// <example>
    ///   <para>Use the colour-blind-safe theme from now on</para>
    ///   <code>Set-ConsoleSetting Theme ColorBlindSafe -Save</code>
    /// </example>
    /// <example>
    ///   <para>Keep animations still and leave the mouse to the terminal, for this session only</para>
    ///   <code>Set-ConsoleSetting Motion Reduced; Set-ConsoleSetting Mouse Off</code>
    /// </example>
    /// <example>
    ///   <para>Use a team theme file in every session</para>
    ///   <code># Requires an existing team-theme.json file, for example from Export-ConsoleTheme.
    /// Set-ConsoleSetting Theme ./team-theme.json -Save</code>
    /// </example>
    [Cmdlet(VerbsCommon.Set, "ConsoleSetting", SupportsShouldProcess = true)]
    public sealed class CmdletSetConsoleSetting : NoPipelineInputCmdlet {
        /// <summary><para type="description">The setting: Theme, Color, Glyphs, Motion, Mouse, Accessible, Hyperlinks, Language or Culture.</para></summary>
        [Parameter(Position = 0, Mandatory = true)]
        [ArgumentCompleter(typeof(ConsoleSettingNameCompleter))]
        public string Name { get; set; } = string.Empty;

        /// <summary><para type="description">The value. Get-ConsoleSetting lists the values each setting takes; Theme also takes a theme file path, Language and Culture a culture name such as pl or de-DE.</para></summary>
        [Parameter(Position = 1, Mandatory = true)]
        [ArgumentCompleter(typeof(ConsoleSettingValueCompleter))]
        public string Value { get; set; } = string.Empty;

        /// <summary><para type="description">Also writes the value to the settings file, for every new session.</para></summary>
        [Parameter]
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
            if (ConsoleSettings.Get(Name) is ConsoleSetting setting && setting.Source != ConsoleSettingSource.Session) {
                WriteWarning(Name + " is still " + setting.Value + " from " + setting.Variable + ".");
            }
        }
    }

    /// <summary>
    /// <para type="synopsis">Undoes Set-ConsoleSetting for this session, and with -Save removes the value from the settings file.</para>
    /// <para type="description">Without -Name every setting is reset. Afterwards the next source applies: an environment variable, the settings file, or detection.</para>
    /// </summary>
    /// <example>
    ///   <para>Go back to the saved theme</para>
    ///   <code>Reset-ConsoleSetting Theme</code>
    /// </example>
    /// <example>
    ///   <para>Remove every saved setting</para>
    ///   <code>Reset-ConsoleSetting -Save</code>
    /// </example>
    [Cmdlet(VerbsCommon.Reset, "ConsoleSetting", SupportsShouldProcess = true)]
    public sealed class CmdletResetConsoleSetting : NoPipelineInputCmdlet {
        /// <summary><para type="description">The settings to reset; all of them when omitted.</para></summary>
        [Parameter(Position = 0)]
        [ArgumentCompleter(typeof(ConsoleSettingNameCompleter))]
        public string[]? Name { get; set; }

        /// <summary><para type="description">Also removes the values from the settings file.</para></summary>
        [Parameter]
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

    /// <summary>
    /// <para type="synopsis">Chooses ConsoleMonster's settings in the terminal with a live preview.</para>
    /// <para type="description">A full-screen picker: each setting on the left, and on the right a preview with a table, statuses, progress, a chart and an error drawn as the highlighted choice would draw them, so themes and glyph sets can be compared before choosing. Save keeps the choices for every session (Set-ConsoleSetting -Save), "Use for this session" applies them now only, and Escape or Cancel changes nothing. A setting that an environment variable sets is shown locked with the variable's name. In a non-interactive session it lists the settings and how to change them instead.</para>
    /// </summary>
    /// <example>
    ///   <para>Choose with the preview, then list what applies now and where each value came from</para>
    ///   <code>Show-ConsoleSettings; Get-ConsoleSetting</code>
    /// </example>
    [Cmdlet(VerbsCommon.Show, "ConsoleSettings")]
    [OutputType(typeof(ConsoleSetting))]
    public sealed class CmdletShowConsoleSettings : NoPipelineInputCmdlet {
        private readonly object _stopGate = new object();
        private CancellationTokenSource? _stop;
        private bool _stopping;

        /// <inheritdoc />
        protected override void EndProcessing() {
            foreach (string problem in ConsoleSettings.Problems) { WriteWarning("Settings file: " + problem); }
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
            try {
                return WordCompletion.Complete(ConsoleSettings.Get(name).Values, wordToComplete);
            } catch (ArgumentException) {
                return Array.Empty<CompletionResult>();
            }
        }
    }
}
