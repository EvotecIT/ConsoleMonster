using System;
using System.Management.Automation;
using ConsoleMonster.Views;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Shows the default output of common types through ConsoleMonster for this session (opt-in).</para>
    /// <para type="description">Files and folders (Get-ChildItem), processes (Get-Process) and services (Get-Service), plus any type registered with Register-ConsoleView, are shown as ConsoleMonster tables when they reach the console. It works by defining an Out-Default function for the session that hands objects to Out-ConsoleDefault; nothing else changes: objects stay the same, pipelines into other commands, Format-* and Out-String keep PowerShell's behaviour, and redirected output, transcripts' plain text, jobs and remoting are not affected. Disable-ConsoleFormatting removes the function. If you already have your own Out-Default function, it is kept and nothing is enabled unless you pass -Force.</para>
    /// <example>
    ///   <para>Turn it on for the session</para>
    ///   <code>Enable-ConsoleFormatting; Get-ChildItem; Get-Process | Select-Object -First 10</code>
    /// </example>
    /// </summary>
    [Cmdlet(VerbsLifecycle.Enable, "ConsoleFormatting")]
    public sealed class CmdletEnableConsoleFormatting : PSCmdlet {
        /// <summary><para type="description">Replaces an existing Out-Default function that is not ConsoleMonster's.</para></summary>
        [Parameter]
        public SwitchParameter Force { get; set; }

        /// <summary><para type="description">Only uses the types registered with Register-ConsoleView, not the built-in ones (files, processes, services).</para></summary>
        [Parameter]
        public SwitchParameter NoBuiltIn { get; set; }

        /// <inheritdoc />
        protected override void EndProcessing() {
            FunctionInfo? existing = InvokeCommand.GetCommand("Out-Default", CommandTypes.Function) as FunctionInfo;
            if (existing != null && existing.Definition.IndexOf(ConsoleFormatting.Marker, StringComparison.OrdinalIgnoreCase) < 0 && !Force.IsPresent) {
                WriteWarning("An Out-Default function already exists in this session; it was kept. Use -Force to replace it.");
                return;
            }
            if (!NoBuiltIn.IsPresent) {
                ConsoleFormatting.RegisterBuiltIns();
            }
            SessionState.InvokeProvider.Item.Set("function:global:Out-Default", ScriptBlock.Create(ConsoleFormatting.ShimScript));
        }
    }

    /// <summary>
    /// <para type="synopsis">Turns off the ConsoleMonster default formatting (Enable-ConsoleFormatting).</para>
    /// <para type="description">Removes the session's Out-Default function when it is ConsoleMonster's, so PowerShell's own Out-Default is used again. Registrations stay for the next Enable-ConsoleFormatting.</para>
    /// <example>
    ///   <para>Back to PowerShell's formatting</para>
    ///   <code>Disable-ConsoleFormatting</code>
    /// </example>
    /// </summary>
    [Cmdlet(VerbsLifecycle.Disable, "ConsoleFormatting")]
    public sealed class CmdletDisableConsoleFormatting : PSCmdlet {
        /// <inheritdoc />
        protected override void EndProcessing() {
            FunctionInfo? existing = InvokeCommand.GetCommand("Out-Default", CommandTypes.Function) as FunctionInfo;
            if (existing != null && existing.Definition.IndexOf(ConsoleFormatting.Marker, StringComparison.OrdinalIgnoreCase) >= 0) {
                InvokeCommand.InvokeScript("Microsoft.PowerShell.Management\\Remove-Item -Path 'Function:\\Out-Default' -Force");
            }
        }
    }

    /// <summary>
    /// <para type="synopsis">Registers how a type is shown by Enable-ConsoleFormatting and Out-ConsoleDefault.</para>
    /// <para type="description">Objects whose type names include -TypeName are shown in the given view when they reach the console. -Property picks the columns as in Format-ConsoleTable (names, calculated properties with Presenter options), -GroupBy groups rows under headings, and types with the same -Group are shown together (files and folders). -Remove deletes a registration. Modules can register their own types when they load.</para>
    /// <example>
    ///   <para>Show a module's result objects as a table</para>
    ///   <code>Register-ConsoleView -TypeName 'Contoso.Server' -View Table -Property Name, @{ Name = 'State'; Expression = { $_.State }; Presenter = 'StatusWord' }, Uptime</code>
    /// </example>
    /// </summary>
    [Cmdlet(VerbsLifecycle.Register, "ConsoleView", DefaultParameterSetName = "Register")]
    public sealed class CmdletRegisterConsoleView : PSCmdlet {
        /// <summary><para type="description">The type names (as in PSObject.TypeNames).</para></summary>
        [Parameter(Position = 0, Mandatory = true)]
        public string[] TypeName { get; set; } = Array.Empty<string>();

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

        /// <summary><para type="description">Removes the registrations instead.</para></summary>
        [Parameter(Mandatory = true, ParameterSetName = "Remove")]
        public SwitchParameter Remove { get; set; }

        /// <inheritdoc />
        protected override void EndProcessing() {
            foreach (string type in TypeName) {
                if (Remove.IsPresent) {
                    ConsoleFormatting.Unregister(type);
                } else {
                    ConsoleFormatting.Register(new ConsoleViewRegistration(type, View, Property, GroupBy is PSObject ps ? ps.BaseObject : GroupBy, Group ?? TypeName[0], Title));
                }
            }
        }
    }
}
