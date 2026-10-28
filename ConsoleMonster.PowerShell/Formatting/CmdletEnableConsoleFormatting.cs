using System;
using System.Management.Automation;
using ConsoleMonster.Views;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Shows common types with ConsoleMonster. Explicit Format-* commands stay native unless FormatCommands is enabled.</para>
    /// <para type="description">Three ways to use ConsoleMonster output: Automatic: Enable-ConsoleFormatting makes ordinary output readable for known and registered types. Explicit: pipe objects to Show-ConsoleTable or Show-ConsoleObject for control in scripts. Native Format-Table, Format-List and Format-Wide: opt in with Enable-ConsoleFormatting -Feature FormatCommands to restyle direct console output while keeping PowerShell column choices. You do not need both automatic and explicit formatting. These -Feature options require ConsoleMonster 2.1.0 or later.</para>
    /// <para type="description">Explicit Format-Table, Format-List and Format-Wide remain native unless you opt in with Enable-ConsoleFormatting -Feature FormatCommands. This feature only changes direct console output; pipelines, captures and transcripts stay native.</para>
    /// <para type="description">Files and folders (Get-ChildItem), processes (Get-Process), services (Get-Service), JSON/REST and CSV records (PSCustomObject), members (Get-Member), commands (Get-Command), modules (Get-Module), history (Get-History), PowerShell 7 ping replies, .NET DNS entries and IP addresses, Windows network objects, event log records (Get-WinEvent), volumes (Get-Volume) and dictionaries (@{ }), plus any type registered with Register-ConsoleView, are shown as ConsoleMonster tables and records when they reach the console. Members group by member type with highlighted signatures and muted types; JSON/CSV records show at most 50 rows and 24 fields with omission hints and nested summaries; numeric and date-like CSV strings align right without conversion. Commands group by module from 12 rows and show alias targets and command-type chips; modules group repeated names and show versions, shortened paths and exported command counts; history shows id, start time, duration, status and highlighted command text. Windows network views register only on Windows; ping and .NET DNS/IP views work across platforms. It works by defining an Out-Default function for the session that hands objects to Out-ConsoleDefault; objects stay the same, pipelines into other commands, Format-* and Out-String keep PowerShell's behaviour, and redirected output, transcripts' plain text, jobs and remoting are not affected. Disable-ConsoleFormatting removes the function. FormatCommands separately opts Format-Table, Format-List and Format-Wide into host-only rendering; it is excluded from All. It preserves native column selection and labels. Explicit properties, calculated expressions, grouping and named views take precedence over built-in presentation defaults. A custom Out-Default is kept unless you pass -Force for a feature that uses it. Streams, ReadLine, GridView and Prompt do not replace Out-Default.</para>
    /// </summary>

    /// <example>
    ///   <para>Inspect members with the built-in view, while keeping an explicit native column selection</para>
    ///   <code>Enable-ConsoleFormatting -Feature Output, FormatCommands; 'synthetic' | Get-Member -Name Length, Substring; Get-Command Get-Date | Format-Table Name, Source</code>
    /// </example>
    /// <example>
    ///   <para>Use the terminal grid where Out-GridView is unavailable; working native commands stay unchanged</para>
    ///   <code>Enable-ConsoleFormatting -Feature GridView; Get-Process | Out-GridView -Title 'Choose processes' -OutputMode Single</code>
    /// </example>
    /// <example>
    ///   <para>Turn it on for the session</para>
    ///   <code>Enable-ConsoleFormatting; Get-ChildItem; Get-Process | Select-Object -First 10</code>
    /// </example>
    /// <example>
    ///   <para>Use a minimal themed shell prompt, separately from default output formatting</para>
    ///   <code>Set-ConsoleTheme -Theme Dark; Enable-ConsoleFormatting -Feature Prompt</code>
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
    [Cmdlet(VerbsLifecycle.Enable, "ConsoleFormatting", SupportsShouldProcess = true)]
    public sealed class CmdletEnableConsoleFormatting : PSCmdlet {
        /// <summary><para type="description">What to turn on: Output (the default: registered and built-in types as ConsoleMonster views), ErrorView (errors that reach the console in ConsoleMonster's error view, following $ErrorView: DetailedView adds the stack, CategoryView keeps PowerShell's), Progress (Write-Progress drawn as ConsoleMonster's progress area; Write-Progress uses a reversible native-parameter function proxy), Streams (native stream and progress colours), ReadLine (syntax, selection and available inline prediction colours for already loaded PSReadLine), or All (Output, ErrorView, Streams and ReadLine). Streams sets PSStyle.Formatting Warning, Verbose, Debug and Error on PowerShell 7.2+, and PSStyle.Progress.Style, or available Host.PrivateData warning, verbose, debug and progress foreground/background colours on older hosts. Native progress keeps its View and layout. ReadLine never imports PSReadLine and does nothing when it is absent; unsupported colour properties are skipped. PowerShell keeps its prefixes, records, preferences, redirection and transcript behaviour. Streams and ReadLine never replace Write-* commands or install an Out-Default function. Progress is the separate native-progress rendering feature, excluded from All; All keeps Write-Progress native and styles it through Streams. Re-enable Streams or ReadLine after changing themes; Shared native properties follow the most recently applied installation. Disabling its owner restores the latest surviving installation; the last owner restores the first original values, including later user edits. Use Show-ConsoleStatusLine or Write-ConsoleLogEntry for rich script messages. FormatCommands explicitly opts Format-Table, Format-List and Format-Wide into direct-host-only rendering, preserving native captures, jobs, remoting and transcripts; it is excluded from All. Help and Errors are separate direct-host opt-ins, excluded from All. Help renders native Get-Help, help and -? with paging. Errors uses the compact view; -Detailed includes stacks. GridView supplies the terminal grid only where Out-GridView is unavailable; a working native command is kept. Prompt adds a minimal shell prompt while preserving native fallback; oh-my-posh and Starship need Force. Features add up across calls; Disable-ConsoleFormatting turns them off.</para></summary>
        [Parameter]
        public ConsoleFormattingFeature[]? Feature { get; set; }

        /// <summary><para type="description">Replaces an existing Out-Default function, Write-Progress alias, or an Out-GridView function/alias when the native grid is unavailable (Disable-ConsoleFormatting puts it back). A working native Out-GridView is always kept. Prompt requires Force to replace oh-my-posh or Starship.</para></summary>
        [Parameter]
        public SwitchParameter Force { get; set; }

        /// <summary><para type="description">Only takes over the types registered with Register-ConsoleView, not the built-in ones (files, processes, services, dictionaries). The built-in views still pick the columns of Show-ConsoleTable and the other commands.</para></summary>
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
            var options = new ConsoleFormattingOptions {
                Feature = Feature, Force = Force.IsPresent, BuiltIn = !NoBuiltIn.IsPresent,
                Detailed = Detailed.IsPresent, ProfileSnippet = ProfileSnippet.IsPresent
            };
            if (ProfileSnippet.IsPresent) { ConsoleHostFormatting.Enable(this, options); return; }
            ConsoleFormattingFeature selected = ConsoleFormattingFeature.None;
            foreach (ConsoleFormattingFeature item in Feature ?? new[] { ConsoleFormattingFeature.Output }) { selected |= item; }
            if (selected == ConsoleFormattingFeature.None || !ShouldProcess("ConsoleMonster formatting: " + ConsoleFormatting.FeatureList(selected), "Enable")) { return; }
            ConsoleHostFormatting.Enable(this, options);
        }




    }

    /// <summary>
    /// <para type="synopsis">Turns off the ConsoleMonster default formatting (Enable-ConsoleFormatting), all of it or some features.</para>
    /// <para type="description">Without -Feature everything Enable-ConsoleFormatting turned on is turned off and the session is as it was: the Out-Default function goes (or the user's own function replaced with -Force comes back), the Write-Progress function is restored, and $ErrorView was never changed. With -Feature only those parts are turned off. Streams and ReadLine release their native colour settings. Shared properties keep the latest surviving installation; the last owner restores the first exact baseline, including subsequent edits to owned properties. The Out-Default function stays while any feature that uses it is on; Streams and ReadLine alone need no function. Registrations stay for the next Enable-ConsoleFormatting.</para>
    /// </summary>
    /// <example>
    ///   <para>Back to PowerShell's formatting</para>
    ///   <code>Disable-ConsoleFormatting</code>
    /// </example>
    /// <example>
    ///   <para>Keep the views, go back to PowerShell's progress</para>
    ///   <code>Disable-ConsoleFormatting -Feature Progress</code>
    /// </example>
    [Cmdlet(VerbsLifecycle.Disable, "ConsoleFormatting", SupportsShouldProcess = true)]
    public sealed class CmdletDisableConsoleFormatting : PSCmdlet {
        /// <summary><para type="description">What to turn off: Output, ErrorView, Progress, Streams, ReadLine, FormatCommands, Help, Errors, GridView, Prompt or All (Output, ErrorView, Streams and ReadLine). Streams and ReadLine restore exactly the saved native colour properties, even if they were edited while enabled; other properties stay untouched. Without -Feature every enabled feature is removed, including explicit Progress, FormatCommands, Help, Errors, GridView and Prompt.</para></summary>
        [Parameter]
        public ConsoleFormattingFeature[]? Feature { get; set; }

        /// <inheritdoc />
        protected override void EndProcessing() {
            ConsoleFormattingFeature selected = ConsoleFormattingFeature.None;
            if (Feature == null) { selected = ConsoleFormatting.Features; }
            else { foreach (ConsoleFormattingFeature item in Feature) { selected |= item; } }
            if (selected == ConsoleFormattingFeature.None || !ShouldProcess("ConsoleMonster formatting: " + ConsoleFormatting.FeatureList(selected), "Disable")) { return; }
            ConsoleHostFormatting.Disable(this, Feature);
        }
    }

    /// <summary>
    /// <para type="synopsis">Registers how a type is shown by Enable-ConsoleFormatting and Out-ConsoleDefault.</para>
    /// <para type="description">Objects whose type names include -TypeName are shown in the given view when they reach the console. -Property picks the columns as in Show-ConsoleTable (names, calculated properties with Presenter options), -GroupBy groups rows under headings, and types with the same -Group are shown together (files and folders). Unregister-ConsoleView removes a registration. Modules can register their own types when they load, or ship a data-only ModuleName.consoleviews.psd1 file without a dependency on ConsoleMonster. Use -Path to import a view file explicitly.</para>
    /// </summary>
    /// <example>
    ///   <para>Show a module's result objects as a table</para>
    ///   <code>Register-ConsoleView -TypeName 'Contoso.Server' -View Table -Property Name, @{ Name = 'State'; Expression = { $_.State }; Presenter = 'StatusWord' }, Uptime</code>
    /// </example>
    [Cmdlet(VerbsLifecycle.Register, "ConsoleView", DefaultParameterSetName = "Register")]
    public sealed class CmdletRegisterConsoleView : PSCmdlet {
        /// <summary><para type="description">The type names (as in PSObject.TypeNames).</para></summary>
        [Parameter(Position = 0, Mandatory = true, ParameterSetName = "Register")]
        public string[] TypeName { get; set; } = Array.Empty<string>();

        /// <summary><para type="description">Imports a data-only .consoleviews.psd1 file. Modules can ship a same-named file beside their manifest without depending on ConsoleMonster.</para></summary>
        [Parameter(Position = 0, Mandatory = true, ParameterSetName = "File")]
        public string[] Path { get; set; } = Array.Empty<string>();

        /// <summary><para type="description">The view: Table (default), Cards, Tree, Inspector or Chart.</para></summary>
        [Parameter(ParameterSetName = "Register")]
        public ConsoleViewKind View { get; set; } = ConsoleViewKind.Table;

        /// <summary><para type="description">The properties, as in Show-ConsoleTable.</para></summary>
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
        /// ordered dictionary of the fields worth showing. It applies in every command (Show-ConsoleTable, -List, -Tree,
        /// Show-Console, Show-ConsoleGrid, Compare-ConsoleObject, Out-ConsoleDefault), also to deserialized objects whose type
        /// names match. Given alone (without -View, -Property, -GroupBy, -Title or -Group), only the shaper is registered.
        /// </para></summary>
        [Parameter(ParameterSetName = "Register")]
        public ScriptBlock? Shaper { get; set; }

        /// <inheritdoc />
        protected override void EndProcessing() {
            if (Path.Length > 0) {
                foreach (string path in Path) { ConsoleHostViews.Import(this, SessionState.Path.GetUnresolvedProviderPathFromPSPath(path)); }
                return;
            }
            bool viewGiven = MyInvocation.BoundParameters.ContainsKey(nameof(View)) || Property != null || GroupBy != null || Title != null || Group != null;
            foreach (string type in TypeName) {
                ConsoleHostViews.Register(new ConsoleViewRegistration(type, View, Property, GroupBy is PSObject ps ? ps.BaseObject : GroupBy, Group ?? TypeName[0], Title), Shaper, onlyIfMissing: Shaper != null && !viewGiven);
            }
        }
    }
}
