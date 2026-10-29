using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation;
using ConsoleMonster.Help;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Shows a command's help: synopsis, highlighted syntax, a parameter table and highlighted examples.</para>
    /// <para type="description">Reads the help PowerShell has for the command (Get-Help -Full): cmdlet help files, comment-based help of functions and scripts, or the help PowerShell generates when there is none. It shows the name and synopsis, the syntax of each parameter set (parameter names, value types and optional parts told apart), the description, a parameter table (type, required, position, pipeline input, default, description), the examples with highlighted PowerShell code, inputs, outputs, notes and related links. Piped on (| more, | Out-Host -Paging, | Out-File) it writes plain lines, so pagers and files get readable text. About topics use the shared Markdown reader, retaining literal HTML and safe link handling. Works on Windows PowerShell 5.1 and PowerShell 7.</para>
    /// </summary>
    /// <example>
    ///   <para>A cmdlet</para>
    ///   <code>Show-ConsoleHelp Get-ChildItem</code>
    /// </example>
    /// <example>
    ///   <para>Through a pager</para>
    ///   <code>Show-ConsoleHelp Show-ConsoleTable | more</code>
    /// </example>
    [Cmdlet(VerbsCommon.Show, "ConsoleHelp")]
    [OutputType(typeof(string))]
    public sealed class CmdletShowConsoleHelp : PSCmdlet {
        /// <summary><para type="description">The command (a cmdlet, function, alias or script path), or an about topic.</para></summary>
        [Parameter(Position = 0, ValueFromPipeline = true)]
        [ValidateNotNullOrEmpty]
        public PSObject? Name { get; set; }

        /// <summary><para type="description">The width in columns (default: the console's width).</para></summary>
        [Parameter]
        [ValidateRange(0, 1000)]
        public int Width { get; set; }

        /// <summary><para type="description">Returns the help as one string of plain text instead of drawing it.</para></summary>
        [Parameter]
        public SwitchParameter AsString { get; set; }

        /// <summary><para type="description">Leaves out the examples.</para></summary>
        [Parameter]
        public SwitchParameter NoExamples { get; set; }

        /// <summary><para type="description">Returns the view widget without drawing or acquiring input.</para></summary>
        [Parameter]
        public SwitchParameter AsWidget { get; set; }

        /// <summary><para type="description">Leaves out the detailed parameter reference.</para></summary>
        [Parameter] public SwitchParameter NoParameters { get; set; }

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
            if (AsWidget.IsPresent) { WriteObject(widget); }
            else { PSFormatting.Emit(this, widget!, Width, null, AsString.IsPresent); }
        }
    }
}
