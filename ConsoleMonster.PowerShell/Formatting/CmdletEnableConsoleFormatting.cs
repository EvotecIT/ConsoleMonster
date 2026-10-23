using System;
using System.Management.Automation;
using ConsoleMonster.Views;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Shows the default output of common types through ConsoleMonster for this session (opt-in).</para>
    /// <para type="description">Files and folders (Get-ChildItem), processes (Get-Process), services (Get-Service), commands (Get-Command), modules (Get-Module), event log records (Get-WinEvent), IP addresses (Get-NetIPAddress), volumes (Get-Volume) and dictionaries (@{ }), plus any type registered with Register-ConsoleView, are shown as ConsoleMonster tables and records when they reach the console. It works by defining an Out-Default function for the session that hands objects to Out-ConsoleDefault; nothing else changes: objects stay the same, pipelines into other commands, Format-* and Out-String keep PowerShell's behaviour, and redirected output, transcripts' plain text, jobs and remoting are not affected. Disable-ConsoleFormatting removes the function. FormatCommands separately opts Format-Table, Format-List and Format-Wide into host-only rendering; it is excluded from All. A custom Out-Default is kept unless you pass -Force when enabling a feature that needs the output function. Streams and ReadLine alone only change native color preferences and work beside a custom Out-Default.</para>
    /// </summary>
    /// <example>
    ///   <para>Turn it on for the session</para>
    ///   <code>Enable-ConsoleFormatting; Get-ChildItem; Get-Process | Select-Object -First 10</code>
    /// </example>
    /// <example>
    ///   <para>The lines to keep it on in every session (printed, never written to the profile)</para>
    ///   <code>Enable-ConsoleFormatting -ProfileSnippet</code>
    /// </example>
    /// <example>
    ///   <para>Default output, the error view, native stream/progress and ReadLine colours</para>
    ///   <code>Enable-ConsoleFormatting -Feature All</code>
    /// </example>
    /// <example>
    ///   <para>Use the active theme for native stream messages, keeping PowerShell's prefixes and stream behaviour</para>
    ///   <code>Set-ConsoleTheme Light; Enable-ConsoleFormatting -Feature Streams; Write-Warning 'Disk space is low'; Write-Verbose 'Checking backup' -Verbose</code>
    /// </example>
    /// <example>
    ///   <para>Theme the already loaded editor and native progress without replacing commands</para>
    ///   <code>Set-ConsoleTheme Dark; Enable-ConsoleFormatting -Feature ReadLine, Streams; Write-Progress -Activity 'Backup' -Status 'Copying archive.zip' -PercentComplete 42; Write-Progress -Activity 'Backup' -Completed</code>
    /// </example>
    [Cmdlet(VerbsLifecycle.Enable, "ConsoleFormatting")]
    public sealed class CmdletEnableConsoleFormatting : PSCmdlet {
        /// <summary><para type="description">What to turn on: Output (the default: registered and built-in types as ConsoleMonster views), ErrorView (errors that reach the console in ConsoleMonster's error view, following $ErrorView: DetailedView adds the stack, CategoryView keeps PowerShell's), Progress (Write-Progress drawn as ConsoleMonster's progress area; Write-Progress becomes an alias of Write-ConsoleProgress), Streams (native stream and progress colours), ReadLine (syntax, selection and available inline prediction colours for already loaded PSReadLine), or All (Output, ErrorView, Streams and ReadLine). Streams sets PSStyle.Formatting Warning, Verbose, Debug and Error on PowerShell 7.2+, and PSStyle.Progress.Style, or available Host.PrivateData warning, verbose, debug and progress foreground/background colours on older hosts. Native progress keeps its View and layout. ReadLine never imports PSReadLine and does nothing when it is absent; unsupported colour properties are skipped. PowerShell keeps its prefixes, records, preferences, redirection and transcript behaviour. Streams and ReadLine never replace Write-* commands or install an Out-Default function. Progress is the separate legacy alias feature, excluded from All; All keeps Write-Progress native and styles it through Streams. Re-enable Streams or ReadLine after changing themes; Shared native properties follow the most recently applied installation. Disabling its owner restores the latest surviving installation; the last owner restores the first original values, including later user edits. Use Write-ConsoleStatus or Write-ConsoleLog for rich script messages. FormatCommands explicitly opts Format-Table, Format-List and Format-Wide into direct-host-only rendering, preserving native captures, jobs, remoting and transcripts; it is excluded from All. Features add up across calls; Disable-ConsoleFormatting turns them off.</para></summary>
        [Parameter]
        public ConsoleFormattingFeature[]? Feature { get; set; }

        /// <summary><para type="description">Replaces an existing Out-Default function or Write-Progress alias that is not ConsoleMonster's (Disable-ConsoleFormatting puts it back).</para></summary>
        [Parameter]
        public SwitchParameter Force { get; set; }

        /// <summary><para type="description">Only takes over the types registered with Register-ConsoleView, not the built-in ones (files, processes, services, dictionaries). The built-in views still pick the columns of Format-ConsoleTable and the other commands.</para></summary>
        [Parameter]
        public SwitchParameter NoBuiltIn { get; set; }

        /// <summary><para type="description">Prints the lines to add to your PowerShell profile so every session starts with ConsoleMonster's formatting, with the options you passed, and changes nothing: the profile is never edited and formatting is not enabled. Pipe them to Add-Content $PROFILE yourself if you want them.</para></summary>
        [Parameter]
        public SwitchParameter ProfileSnippet { get; set; }

        /// <summary><para type="description">Includes script and .NET stack frames in the opt-in Errors layout. Used only with -Feature Errors; the default layout folds inner exceptions and hides stacks.</para></summary>
        [Parameter]
        public SwitchParameter Detailed { get; set; }

        /// <inheritdoc />
        protected override void EndProcessing() {
            ConsoleFormattingFeature wanted = ConsoleFormattingFeature.None;
            foreach (ConsoleFormattingFeature feature in Feature ?? new[] { ConsoleFormattingFeature.Output }) {
                wanted |= feature;
            }
            if (ProfileSnippet.IsPresent) {
                foreach (string line in ConsoleFormatting.ProfileSnippet(Convert.ToString(SessionState.PSVariable.GetValue("PROFILE"), System.Globalization.CultureInfo.InvariantCulture), NoBuiltIn.IsPresent, Force.IsPresent, wanted)) {
                    WriteObject(Detailed.IsPresent && line.StartsWith("Enable-ConsoleFormatting", StringComparison.Ordinal) ? line + " -Detailed" : line);
                }
                return;
            }
            if (wanted == ConsoleFormattingFeature.None) {
                return;
            }
            const ConsoleFormattingFeature cosmetic = ConsoleFormattingFeature.Streams | ConsoleFormattingFeature.ReadLine;
            if ((wanted & ~cosmetic) == ConsoleFormattingFeature.None) {
                ApplyNativeColors(wanted);
                ConsoleFormatting.Features |= wanted;
                ConsoleFormatting.RestoreCommandsWithModule(MyInvocation.MyCommand.Module);
                return;
            }
            ConsoleModuleViews.Discover(this);
            ScriptBlock? existing = FormatCommandInstallation.GlobalFunction(SessionState, "Out-Default");
            bool ours = existing != null && existing.ToString().IndexOf(ConsoleFormatting.Marker, StringComparison.OrdinalIgnoreCase) >= 0;
            if (existing != null && !ours) {
                if (!Force.IsPresent) {
                    WriteWarning("An Out-Default function already exists in this session; it was kept. Use -Force to replace it.");
                    return;
                }
                ConsoleFormatting.SavedOutDefault = existing;
            }
            if ((wanted & ConsoleFormattingFeature.Progress) != 0 && !InstallProgressAlias()) {
                wanted &= ~ConsoleFormattingFeature.Progress;
                if (wanted == ConsoleFormattingFeature.None) {
                    return;
                }
            }
            if ((wanted & ConsoleFormattingFeature.FormatCommands) != 0 && !FormatCommandInstallation.Install(this, Force.IsPresent)) {
                wanted &= ~ConsoleFormattingFeature.FormatCommands;
                if (wanted == ConsoleFormattingFeature.None) {
                    return;
                }
            }
            if ((wanted & ConsoleFormattingFeature.Help) != 0 && !FormatCommandInstallation.InstallHelp(this, Force.IsPresent)) {
                wanted &= ~ConsoleFormattingFeature.Help;
                if (wanted == ConsoleFormattingFeature.None) { return; }
            }
            if ((wanted & ConsoleFormattingFeature.Output) != 0) {
                ConsoleFormatting.TakeOverBuiltIns = !NoBuiltIn.IsPresent;
            }
            ApplyNativeColors(wanted);
            ConsoleFormatting.Features |= wanted;
            if ((wanted & ConsoleFormattingFeature.Errors) != 0) {
                FormattingSessionState.Current.DetailedErrors = Detailed.IsPresent;
            }
            if (!ours) {
                SessionState.InvokeProvider.Item.Set("function:global:Out-Default", ScriptBlock.Create(ConsoleFormatting.ShimScript));
            }
            ConsoleFormatting.RestoreCommandsWithModule(MyInvocation.MyCommand.Module);
        }

        private void ApplyNativeColors(ConsoleFormattingFeature wanted) {
            FormattingSessionState state = FormattingSessionState.Current;
            if ((wanted & ConsoleFormattingFeature.Streams) != 0) { state.Streams.Apply(this); }
            if ((wanted & ConsoleFormattingFeature.ReadLine) != 0) { state.ReadLine.Apply(this); }
        }

        // Write-Progress as a global alias of Write-ConsoleProgress: an alias wins over the cmdlet everywhere, also inside
        // modules, and the cmdlet reads $ProgressPreference in its caller's scope as Write-Progress does.
        private bool InstallProgressAlias() {
            if (InvokeCommand.GetCommand("Write-Progress", CommandTypes.Function) != null && !Force.IsPresent) {
                // The alias would win over the user's own Write-Progress function everywhere.
                WriteWarning("A Write-Progress function exists in this session; the Progress feature would hide it and was not turned on. Use -Force to turn it on anyway.");
                return false;
            }
            AliasInfo? alias = InvokeCommand.GetCommand("Write-Progress", CommandTypes.Alias) as AliasInfo;
            if (alias != null && !string.Equals(alias.Definition, ConsoleFormatting.ProgressCommand, StringComparison.OrdinalIgnoreCase)) {
                if (!Force.IsPresent) {
                    WriteWarning("A Write-Progress alias already exists in this session; it was kept and the Progress feature was not turned on. Use -Force to replace it.");
                    return false;
                }
                ConsoleFormatting.SavedWriteProgressAlias = alias.Definition;
            }
            InvokeCommand.InvokeScript("Microsoft.PowerShell.Utility\\Set-Alias -Name Write-Progress -Value " + ConsoleFormatting.ProgressCommand + " -Scope Global -Force");
            SessionProgress.Enabled = true;
            return true;
        }
    }

    /// <summary>
    /// <para type="synopsis">Turns off the ConsoleMonster default formatting (Enable-ConsoleFormatting), all of it or some features.</para>
    /// <para type="description">Without -Feature everything Enable-ConsoleFormatting turned on is turned off and the session is as it was: the Out-Default function goes (or the user's own function replaced with -Force comes back), the Write-Progress alias goes (or the user's own alias comes back), and $ErrorView was never changed. With -Feature only those parts are turned off. Streams and ReadLine release their native colour settings. Shared properties keep the latest surviving installation; the last owner restores the first exact baseline, including subsequent edits to owned properties. The Out-Default function stays while any feature that uses it is on; Streams and ReadLine alone need no function. Registrations stay for the next Enable-ConsoleFormatting.</para>
    /// </summary>
    /// <example>
    ///   <para>Back to PowerShell's formatting</para>
    ///   <code>Disable-ConsoleFormatting</code>
    /// </example>
    /// <example>
    ///   <para>Keep the views, go back to PowerShell's progress</para>
    ///   <code>Disable-ConsoleFormatting -Feature Progress</code>
    /// </example>
    [Cmdlet(VerbsLifecycle.Disable, "ConsoleFormatting")]
    public sealed class CmdletDisableConsoleFormatting : PSCmdlet {
        /// <summary><para type="description">What to turn off: Output, ErrorView, Progress, Streams, ReadLine, FormatCommands or All (Output, ErrorView, Streams and ReadLine). Streams and ReadLine restore exactly the saved native colour properties, even if they were edited while enabled; other properties stay untouched. Without -Feature every enabled feature is removed, including explicit Progress and FormatCommands.</para></summary>
        [Parameter]
        public ConsoleFormattingFeature[]? Feature { get; set; }

        /// <inheritdoc />
        protected override void EndProcessing() {
            ConsoleFormattingFeature off = ConsoleFormattingFeature.None;
            foreach (ConsoleFormattingFeature feature in Feature ?? new[] { ConsoleFormattingFeature.All | ConsoleFormattingFeature.Progress | ConsoleFormattingFeature.FormatCommands }) {
                off |= feature;
            }
            if ((off & ConsoleFormattingFeature.Progress) != 0) {
                ConsoleFormatting.RemoveProgressAlias(InvokeCommand);
            }
            if ((off & ConsoleFormattingFeature.FormatCommands) != 0) {
                FormatCommandInstallation.Remove(FormattingSessionState.Current);
            }
            if ((off & ConsoleFormattingFeature.Help) != 0) {
                FormatCommandInstallation.RemoveHelp(FormattingSessionState.Current);
            }
            if ((off & ConsoleFormattingFeature.Streams) != 0) {
                FormattingSessionState.Current.Streams.Restore();
            }
            if ((off & ConsoleFormattingFeature.ReadLine) != 0) {
                FormattingSessionState.Current.ReadLine.Restore();
            }
            ConsoleFormatting.Features &= ~off;
            if ((ConsoleFormatting.Features & ~(ConsoleFormattingFeature.Streams | ConsoleFormattingFeature.Help, ConsoleFormattingFeature.Errors, ConsoleFormattingFeature.ReadLine)) != ConsoleFormattingFeature.None) {
                return;
            }
            ScriptBlock? existing = FormatCommandInstallation.GlobalFunction(SessionState, "Out-Default");
            if (existing != null && existing.ToString().IndexOf(ConsoleFormatting.Marker, StringComparison.OrdinalIgnoreCase) >= 0) {
                if (ConsoleFormatting.SavedOutDefault != null) {
                    SessionState.InvokeProvider.Item.Set("function:global:Out-Default", ConsoleFormatting.SavedOutDefault);
                } else {
                    FormatCommandInstallation.RemoveGlobalFunction(MyInvocation.MyCommand.Module, "Out-Default");
                }
            }
            ConsoleFormatting.SavedOutDefault = null;
        }
    }

    /// <summary>
    /// <para type="synopsis">Registers how a type is shown by Enable-ConsoleFormatting and Out-ConsoleDefault.</para>
    /// <para type="description">Objects whose type names include -TypeName are shown in the given view when they reach the console. -Property picks the columns as in Format-ConsoleTable (names, calculated properties with Presenter options), -GroupBy groups rows under headings, and types with the same -Group are shown together (files and folders). -Remove deletes a registration. Modules can register their own types when they load, or ship a data-only ModuleName.consoleviews.psd1 file without a dependency on ConsoleMonster. Use -Path to import a view file explicitly.</para>
    /// </summary>
    /// <example>
    ///   <para>Show a module's result objects as a table</para>
    ///   <code>Register-ConsoleView -TypeName 'Contoso.Server' -View Table -Property Name, @{ Name = 'State'; Expression = { $_.State }; Presenter = 'StatusWord' }, Uptime</code>
    /// </example>
    [Cmdlet(VerbsLifecycle.Register, "ConsoleView", DefaultParameterSetName = "Register")]
    public sealed class CmdletRegisterConsoleView : PSCmdlet {
        /// <summary><para type="description">The type names (as in PSObject.TypeNames).</para></summary>
        [Parameter(Position = 0, Mandatory = true, ParameterSetName = "Register")]
        [Parameter(Position = 0, Mandatory = true, ParameterSetName = "Remove")]
        public string[] TypeName { get; set; } = Array.Empty<string>();

        /// <summary><para type="description">Imports a data-only .consoleviews.psd1 file. Modules can ship a same-named file beside their manifest without depending on ConsoleMonster.</para></summary>
        [Parameter(Position = 0, Mandatory = true, ParameterSetName = "File")]
        public string[] Path { get; set; } = Array.Empty<string>();

        /// <summary><para type="description">The view: Table (default), Cards, Tree, Inspector or Chart.</para></summary>
        [Parameter(ParameterSetName = "Register")]
        public ConsoleViewKind View { get; set; } = ConsoleViewKind.Table;

        /// <summary><para type="description">The properties, as in Format-ConsoleTable.</para></summary>
        [Parameter(ParameterSetName = "Register")]
        public object[]? Property { get; set; }

        /// <summary><para type="description">A property name or script block that groups rows under headings.</para></summary>
        [Parameter(ParameterSetName = "Register")]
        public object? GroupBy { get; set; }

        /// <summary><para type="description">A script block for each object's title ($_ is the object): a shared title heads the view, different titles group the rows.</para></summary>
        [Parameter(ParameterSetName = "Register")]
        public ScriptBlock? Title { get; set; }

        /// <summary><para type="description">A group name: consecutive objects of types in the same group are shown in one view.</para></summary>
        [Parameter(ParameterSetName = "Register")]
        public string? Group { get; set; }

        /// <summary><para type="description">
        /// A script block that turns each object of the type into what is shown instead ($_ is the object), for example an
        /// ordered dictionary of the fields worth showing. It applies in every command (Format-ConsoleTable, -List, -Tree,
        /// Show-Console, Show-ConsoleGrid, Compare-ConsoleObject, Out-ConsoleDefault), also to deserialized objects whose type
        /// names match. Given alone (without -View, -Property, -GroupBy, -Title or -Group), only the shaper is registered.
        /// </para></summary>
        [Parameter(ParameterSetName = "Register")]
        public ScriptBlock? Shaper { get; set; }

        /// <summary><para type="description">Removes the registrations instead.</para></summary>
        [Parameter(Mandatory = true, ParameterSetName = "Remove")]
        public SwitchParameter Remove { get; set; }

        /// <inheritdoc />
        protected override void EndProcessing() {
            if (Path.Length > 0) {
                foreach (string path in Path) { ConsoleModuleViews.Import(this, SessionState.Path.GetUnresolvedProviderPathFromPSPath(path), false); }
                return;
            }
            bool viewGiven = MyInvocation.BoundParameters.ContainsKey(nameof(View)) || Property != null || GroupBy != null || Title != null || Group != null;
            foreach (string type in TypeName) {
                if (Remove.IsPresent) {
                    ConsoleFormatting.Unregister(type);
                    Values.ValueShaper.Unregister(type);
                    continue;
                }
                if (Shaper != null) {
                    Binding.PSShaping.RegisterShaper(type, Shaper);
                }
                ConsoleFormatting.Register(new ConsoleViewRegistration(type, View, Property, GroupBy is PSObject ps ? ps.BaseObject : GroupBy, Group ?? TypeName[0], Title), onlyIfMissing: Shaper != null && !viewGiven);
            }
        }
    }
}
