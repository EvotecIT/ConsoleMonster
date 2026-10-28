using System;
using System.Management.Automation;
using ConsoleMonster.Views;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Enables the everyday console views, native format commands and theme colours.</para>
    /// <para type="description">By default, Output, FormatCommands, Errors, Streams and ReadLine are enabled. Format-Table, Format-List and Format-Wide keep PowerShell's column choices and change only direct console output; pipelines, captures, files, jobs, remoting and transcripts remain native. Use Exclude to remove features from the default set, or Feature to select a minimal set. Prompt, Help, Progress and GridView require explicit Feature selection. CompactErrors opts into the compact error layout. Get-ConsoleFormatting reports what is enabled. Disable-ConsoleFormatting restores the session.</para>
    /// <para type="description">Files, processes, services, structured records, commands, modules, history, IP addresses, volumes, versions, dictionaries and registered types get built-in or registered views. Explicit Show-Console commands remain available when you want to choose the view yourself. Custom Out-Default functions are kept unless Force is specified; Prompt does not replace oh-my-posh or Starship without Force.</para>
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
    ///   <para>Exclude native format command styling from the default set</para>
    ///   <code>Enable-ConsoleFormatting -Exclude FormatCommands</code>
    /// </example>
    /// <example>
    ///   <para>Use the active theme for native stream messages, keeping PowerShell's prefixes and stream behaviour</para>
    ///   <code>Set-ConsoleTheme Light; Enable-ConsoleFormatting -Feature Streams; Write-Warning 'Disk space is low'; Write-Verbose 'Checking backup' -Verbose</code>
    /// </example>
    /// <example>
    ///   <para>Theme the already loaded editor and native progress without replacing commands</para>
    ///   <code>Set-ConsoleTheme Dark; Enable-ConsoleFormatting -Feature ReadLine, Streams; Write-Progress -Activity 'Backup' -Status 'Copying archive.zip' -PercentComplete 42; Write-Progress -Activity 'Backup' -Completed</code>
    /// </example>
    [Cmdlet(VerbsLifecycle.Enable, "ConsoleFormatting", SupportsShouldProcess = true, DefaultParameterSetName = "Feature")]
    public sealed class CmdletEnableConsoleFormatting : PSCmdlet {
        /// <summary><para type="description">Choose only these features: Output, FormatCommands, Errors, Streams, ReadLine, Prompt, Help, Progress or GridView. The default without Feature enables Output, FormatCommands, Errors, Streams and ReadLine. Features add up across calls; use Disable-ConsoleFormatting to restore them. FormatCommands keeps native column selections and changes only direct host output. Streams and ReadLine change native colours without replacing Write-* commands. Progress replaces Write-Progress only when explicitly selected. Help, GridView and Prompt are explicit opt-ins.</para></summary>
        [Parameter(ParameterSetName = "Feature")]
        public ConsoleFormattingFeature[]? Feature { get; set; }

        /// <summary><para type="description">Turns on the default set except these features, and disables any excluded feature already enabled. Cannot be combined with Feature.</para></summary>
        [Parameter(Mandatory = true, ParameterSetName = "Exclude")]
        public ConsoleFormattingFeature[]? Exclude { get; set; }

        /// <summary><para type="description">Uses the compact message-first error layout. Errors must be enabled.</para></summary>
        [Parameter]
        public SwitchParameter CompactErrors { get; set; }

        /// <summary><para type="description">Replaces an existing Out-Default function, Write-Progress alias, or an Out-GridView function/alias when the native grid is unavailable (Disable-ConsoleFormatting puts it back). A working native Out-GridView is always kept. Prompt requires Force to replace oh-my-posh or Starship.</para></summary>
        [Parameter]
        public SwitchParameter Force { get; set; }

        /// <summary><para type="description">Only takes over the types registered with Register-ConsoleView, not the built-in ones (files, processes, services, dictionaries). The built-in views still pick the columns of Show-ConsoleTable and the other commands.</para></summary>
        [Parameter]
        public SwitchParameter NoBuiltIn { get; set; }

        /// <summary><para type="description">Prints the lines to add to your PowerShell profile so every session starts with ConsoleMonster's formatting, with the options you passed, and changes nothing: the profile is never edited and formatting is not enabled. Pipe them to Add-Content $PROFILE yourself if you want them.</para></summary>
        [Parameter]
        public SwitchParameter ProfileSnippet { get; set; }

        /// <summary><para type="description">Includes script and .NET stack frames in the compact error layout. Requires CompactErrors.</para></summary>
        [Parameter]
        public SwitchParameter Detailed { get; set; }

        /// <inheritdoc />
        protected override void EndProcessing() {
            var options = new ConsoleFormattingOptions {
                Feature = Feature, Exclude = Exclude, Force = Force.IsPresent, BuiltIn = !NoBuiltIn.IsPresent,
                Detailed = Detailed.IsPresent, CompactErrors = CompactErrors.IsPresent, ProfileSnippet = ProfileSnippet.IsPresent
            };
            if (ProfileSnippet.IsPresent) { ConsoleHostFormatting.Enable(this, options); return; }
            ConsoleFormattingFeature selected = ConsoleHostFormatting.Selected(options);
            if (selected == ConsoleFormattingFeature.None && Exclude == null) { return; }
            if (!ShouldProcess("ConsoleMonster formatting: " + ConsoleFormatting.FeatureList(selected), "Enable")) { return; }
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
        /// <summary><para type="description">The features to restore. Without Feature, all enabled features are restored. Streams and ReadLine restore the exact saved native colour properties.</para></summary>
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
