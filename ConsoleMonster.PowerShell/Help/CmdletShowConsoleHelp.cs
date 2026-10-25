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
        [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        public string Name { get; set; } = string.Empty;

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

        /// <inheritdoc />
        protected override void ProcessRecord() {
            PSOutputExport.Validate(this);
            if (!ConsoleHostHelp.TryCreate(Name, InvokeCommand, out IWidget? widget, out ErrorRecord? error, NoExamples.IsPresent)) {
                WriteError(error!);
                return;
            }
            if (AsWidget.IsPresent) { WriteObject(widget); }
            else { PSFormatting.Emit(this, widget!, Width, null, AsString.IsPresent); }
        }
    }
}
