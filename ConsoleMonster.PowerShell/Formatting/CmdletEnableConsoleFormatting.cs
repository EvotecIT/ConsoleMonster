using System;
using System.Management.Automation;
using ConsoleMonster.Views;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Shows the default output of common types through ConsoleMonster for this session (opt-in).</para>
    /// <para type="description">Files and folders (Get-ChildItem), processes (Get-Process), services (Get-Service), members (Get-Member), commands (Get-Command), modules (Get-Module), history (Get-History), event log records (Get-WinEvent), IP addresses (Get-NetIPAddress), volumes (Get-Volume) and dictionaries (@{ }), plus any type registered with Register-ConsoleView, are shown as ConsoleMonster tables and records when they reach the console. Members group by member type with highlighted signatures and muted types; commands group by module with command-type chips; modules show version, linked path and exported counts; history shows id, duration, status and highlighted command text. It works by defining an Out-Default function for the session that hands objects to Out-ConsoleDefault; objects stay the same, pipelines into other commands, Format-* and Out-String keep PowerShell's behaviour, and redirected output, transcripts' plain text, jobs and remoting are not affected. Disable-ConsoleFormatting removes the function. FormatCommands separately opts Format-Table, Format-List and Format-Wide into host-only rendering; it is excluded from All. It preserves native column selection and labels. Explicit properties, calculated expressions, grouping and named views take precedence over built-in presentation defaults. If you already have your own Out-Default function, it is kept and nothing is enabled unless you pass -Force.</para>
    /// </summary>
    /// <example>
    ///   <para>Inspect members with the built-in view, while keeping an explicit native column selection</para>
    ///   <code>Enable-ConsoleFormatting -Feature Output, FormatCommands; 'synthetic' | Get-Member -Name Length, Substring; Get-Command Get-Date | Format-Table Name, Source</code>
    /// </example>
    /// <example>
    ///   <para>Turn it on for the session</para>
    ///   <code>Enable-ConsoleFormatting; Get-ChildItem; Get-Process | Select-Object -First 10</code>
    /// </example>
    /// <example>
    ///   <para>The lines to keep it on in every session (printed, never written to the profile)</para>
    ///   <code>Enable-ConsoleFormatting -ProfileSnippet</code>
    /// </example>
    /// <example>
    ///   <para>Default output, the error view, progress and native stream colours</para>
    ///   <code>Enable-ConsoleFormatting -Feature All</code>
    /// </example>
    /// <example>
    ///   <para>Use the active theme for native stream messages, keeping PowerShell's prefixes and stream behaviour</para>
    ///   <code>Set-ConsoleTheme Light; Enable-ConsoleFormatting -Feature Streams; Write-Warning 'Disk space is low'; Write-Verbose 'Checking backup' -Verbose</code>
    /// </example>
    [Cmdlet(VerbsLifecycle.Enable, "ConsoleFormatting")]
    public sealed class CmdletEnableConsoleFormatting : PSCmdlet {
        /// <summary><para type="description">What to turn on: Output (the default: registered and built-in types as ConsoleMonster views), ErrorView (errors that reach the console in ConsoleMonster's error view, following $ErrorView: DetailedView adds the stack, CategoryView keeps PowerShell's), Progress (Write-Progress drawn as ConsoleMonster's progress area; Write-Progress becomes an alias of Write-ConsoleProgress), Streams (native stream colours), or All (Output, ErrorView, Progress and Streams). Streams sets PSStyle.Formatting Warning, Verbose, Debug and Error on PowerShell 7.2+, or available Host.PrivateData warning, verbose and debug foreground/background colours on older hosts. PowerShell keeps its prefixes, records, preferences, redirection and transcript behaviour. No Write-* stream command is replaced. Re-enable Streams after changing themes; Disable restores the original values of only the properties ConsoleMonster set, including later user edits. Use Write-ConsoleStatus or Write-ConsoleLog for rich script messages. FormatCommands explicitly opts Format-Table, Format-List and Format-Wide into direct-host-only rendering, preserving native captures, jobs, remoting and transcripts; it is excluded from All. Features add up across calls; Disable-ConsoleFormatting turns them off.</para></summary>
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

        /// <inheritdoc />
        protected override void EndProcessing() {
            ConsoleFormattingFeature wanted = ConsoleFormattingFeature.None;
            foreach (ConsoleFormattingFeature feature in Feature ?? new[] { ConsoleFormattingFeature.Output }) {
                wanted |= feature;
            }
            if (ProfileSnippet.IsPresent) {
                foreach (string line in ConsoleFormatting.ProfileSnippet(Convert.ToString(SessionState.PSVariable.GetValue("PROFILE"), System.Globalization.CultureInfo.InvariantCulture), NoBuiltIn.IsPresent, Force.IsPresent, wanted)) {
                    WriteObject(line);
                }
                return;
            }
            if (wanted == ConsoleFormattingFeature.None) {
                return;
            }
            if (wanted == ConsoleFormattingFeature.Streams) {
                FormattingSessionState.Current.Streams.Apply(this);
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
            if ((wanted & ConsoleFormattingFeature.Output) != 0) {
                ConsoleFormatting.TakeOverBuiltIns = !NoBuiltIn.IsPresent;
            }
            if ((wanted & ConsoleFormattingFeature.Streams) != 0) {
                FormattingSessionState.Current.Streams.Apply(this);
            }
            ConsoleFormatting.Features |= wanted;
            if (!ours) {
                SessionState.InvokeProvider.Item.Set("function:global:Out-Default", ScriptBlock.Create(ConsoleFormatting.ShimScript));
            }
            ConsoleFormatting.RestoreCommandsWithModule(MyInvocation.MyCommand.Module);
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
    /// <para type="description">Without -Feature everything Enable-ConsoleFormatting turned on is turned off and the session is as it was: the Out-Default function goes (or the user's own function replaced with -Force comes back), the Write-Progress alias goes (or the user's own alias comes back), and $ErrorView was never changed. With -Feature only those parts are turned off. Streams restores the saved native colour settings, including subsequent edits to owned properties. The Out-Default function stays while any feature that uses it is on; Streams alone needs no function. Registrations stay for the next Enable-ConsoleFormatting.</para>
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
        /// <summary><para type="description">What to turn off: Output, ErrorView, Progress, Streams, FormatCommands or All (Output, ErrorView, Progress and Streams). Streams restores exactly the saved native colour properties, even if they were edited while enabled; other properties stay untouched. Without -Feature every enabled feature is removed, including FormatCommands.</para></summary>
        [Parameter]
        public ConsoleFormattingFeature[]? Feature { get; set; }

        /// <inheritdoc />
        protected override void EndProcessing() {
            ConsoleFormattingFeature off = ConsoleFormattingFeature.None;
            foreach (ConsoleFormattingFeature feature in Feature ?? new[] { ConsoleFormattingFeature.All | ConsoleFormattingFeature.FormatCommands }) {
                off |= feature;
            }
            if ((off & ConsoleFormattingFeature.Progress) != 0) {
                ConsoleFormatting.RemoveProgressAlias(InvokeCommand);
            }
            if ((off & ConsoleFormattingFeature.FormatCommands) != 0) {
                FormatCommandInstallation.Remove(FormattingSessionState.Current);
            }
            if ((off & ConsoleFormattingFeature.Streams) != 0) {
                FormattingSessionState.Current.Streams.Restore();
            }
            ConsoleFormatting.Features &= ~off;
            if ((ConsoleFormatting.Features & ~ConsoleFormattingFeature.Streams) != ConsoleFormattingFeature.None) {
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
