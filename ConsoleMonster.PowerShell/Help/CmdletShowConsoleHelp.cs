using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Threading;
using ConsoleMonster.Help;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>Shows a command's help: synopsis, highlighted syntax, a parameter table and highlighted examples.</summary>
    /// <remarks><para>Reads the help PowerShell has for the command (Get-Help -Full): cmdlet help files, comment-based help of functions and scripts, or the help PowerShell generates when there is none. It shows the name and synopsis, the syntax of each parameter set (parameter names, value types and optional parts told apart), the description, a parameter table (type, required, position, pipeline input, default, description), the examples with highlighted PowerShell code, inputs, outputs, notes and related links. On an interactive terminal, help longer than the screen opens in the document viewer: tabs for Overview, Parameters, Examples and Related, an outline, folding sections, find (/), Ctrl+K to go to a section or parameter, and related commands that open their own help with back and forward (Alt+Left/Right). -NoPager writes it instead. Piped on (| more, | Out-Host -Paging, | Out-File) it writes plain lines, so pagers and files get readable text. About topics use the shared Markdown reader, retaining literal HTML and safe link handling. Works on Windows PowerShell 5.1 and PowerShell 7.</para></remarks>
    /// <example>
    ///   <para>A cmdlet</para>
    ///   <code>Show-ConsoleHelp Get-ChildItem</code>
    /// </example>
    /// <example>
    ///   <para>Through a pager</para>
    ///   <code>Show-ConsoleHelp Show-ConsoleTable -AsWidget | Export-ConsoleFrame -Format Text</code>
    /// </example>
    [Cmdlet(VerbsCommon.Show, "ConsoleHelp", HelpUri = "https://consolemonster.dev/powershell/commands/show-consolehelp/")]
    [OutputType(typeof(string))]
    public sealed class CmdletShowConsoleHelp : PSCmdlet {
        /// <summary><para type="description">The command (a cmdlet, function, alias or script path), or an about topic.</para></summary>
        [Parameter(Position = 0, ValueFromPipeline = true)]
        [ValidateNotNullOrEmpty]
        public PSObject? Name { get; set; }

        /// <summary><para type="description">The width in columns (default: the console's width).</para></summary>
        [Parameter]
        [ValidateRange(0, 1000)]
        [PSDefaultValue(Help = "0")]
        public int Width { get; set; }

        /// <summary><para type="description">Leaves out the examples.</para></summary>
        [Parameter]
        [PSDefaultValue(Help = "$false")]
        public SwitchParameter NoExamples { get; set; }

        /// <summary><para type="description">Returns the view widget without drawing or acquiring input.</para></summary>
        [Parameter]
        [PSDefaultValue(Help = "$false")]
        public SwitchParameter AsWidget { get; set; }

        /// <summary><para type="description">Leaves out the detailed parameter reference.</para></summary>
        [Parameter] [PSDefaultValue(Help = "$false")]
        public SwitchParameter NoParameters { get; set; }

        /// <summary><para type="description">Writes the help to the console instead of opening the document viewer, also on an interactive terminal.</para></summary>
        [Parameter] [PSDefaultValue(Help = "$false")]
        public SwitchParameter NoPager { get; set; }

        private CancellationTokenSource? _viewing;
        private bool _stopped;

        /// <inheritdoc />
        protected override void ProcessRecord() {
            object? value = PSText.Unwrap(Name);
            string? name = value is CommandInfo command ? command.Name : value as string;
            if (string.IsNullOrWhiteSpace(name)) {
                ThrowTerminatingError(new ErrorRecord(new ArgumentException("Show-ConsoleHelp needs a command name or command object. Use Show-Console to display other pipeline objects."), "ExpectedCommandName", ErrorCategory.InvalidArgument, Name));
                return;
            }
            PSOutputExport.Validate(this);
            if (!ConsoleHostHelp.TryCreate(name!, InvokeCommand, out IWidget? widget, out ErrorRecord? error, NoExamples.IsPresent, NoParameters.IsPresent)) {
                WriteError(error!);
                return;
            }
            if (AsWidget.IsPresent) { WriteObject(widget); return; }
            if (!NoPager.IsPresent && FormatCommandRouter.IsDirectHostOutput(this)) {
                using (CancellationTokenSource stop = new CancellationTokenSource()) {
                    _viewing = stop;
                    try {
                        if (_stopped) { stop.Cancel(); }
                        if (HelpPager.Show(this, widget!, stop.Token, Width > 0 ? Width : (int?)null)) { return; }
                    } catch (OperationCanceledException) when (_stopped) {
                        return;
                    } finally { _viewing = null; }
                }
            }
            PSFormatting.Emit(this, widget!, Width, null, false);
        }

        /// <inheritdoc />
        protected override void StopProcessing() { _stopped = true; try { _viewing?.Cancel(); } catch (ObjectDisposedException) { } base.StopProcessing(); }
    }
}
