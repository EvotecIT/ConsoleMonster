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
    ///   <code>Show-ConsoleHelp Format-ConsoleTable | more</code>
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

        /// <inheritdoc />
        protected override void ProcessRecord() {
            CommandInfo? command = null;
            bool wildcard = WildcardPattern.ContainsWildcardCharacters(Name);
            bool topic = Name.StartsWith("about_", StringComparison.OrdinalIgnoreCase) || wildcard;
            if (!topic) {
                try {
                    command = InvokeCommand.GetCommand(Name, CommandTypes.All);
                } catch (Exception ex) when (!(ex is PipelineStoppedException)) {
                    command = null;
                }
            }
            // Patterns and about topics belong to Get-Help. Looking them up as literal commands first invokes
            // missing-command discovery and module analysis before Get-Help's own search.
            if (command is ApplicationInfo) {
                // A program has no PowerShell help; Get-Help would search every help file first.
                WriteError(new ErrorRecord(new ItemNotFoundException("'" + Name + "' is a program; try " + Name + " /? or " + Name + " --help."), "ProgramHasNoHelp", ErrorCategory.ObjectNotFound, Name));
                return;
            }
            if (command == null && !topic) {
                // Get-Help searches every help file before it gives up on an unknown name, which takes many seconds.
                WriteError(new ErrorRecord(new ItemNotFoundException("'" + Name + "' is not a command, function, alias or script here."), "CommandNotFound", ErrorCategory.ObjectNotFound, Name));
                return;
            }
            Collection<PSObject> found;
            try {
                found = InvokeCommand.InvokeScript(true, ScriptBlock.Create("param($__cmName) Microsoft.PowerShell.Core\\Get-Help -Name $__cmName -Full -ErrorAction Stop"), null, Name);
            } catch (RuntimeException ex) {
                // Get-Help's own error, not the -ErrorAction Stop wrapper around it.
                WriteError(ex.ErrorRecord != null && !(ex.ErrorRecord.Exception is ActionPreferenceStopException) ? ex.ErrorRecord : new ErrorRecord(ex.InnerException ?? ex, "HelpNotFound", ErrorCategory.ObjectNotFound, Name));
                return;
            }
            List<PSObject> entries = new List<PSObject>();
            foreach (PSObject item in found) {
                if (item != null) {
                    entries.Add(item);
                }
            }
            if (entries.Count > 1 && command == null) {
                // A wildcard or a topic search: Get-Help gives short entries, shown as a list to pick from.
                PSFormatting.Emit(this, IndexTable(entries), Width, null, AsString.IsPresent);
                return;
            }
            PSObject? help = Matching(entries, command);
            if (help == null) {
                WriteError(new ErrorRecord(new ItemNotFoundException("No help was found for '" + Name + "'."), "HelpNotFound", ErrorCategory.ObjectNotFound, Name));
                return;
            }
            IWidget view;
            if (help.BaseObject is string text) {
                // The optional Markdown adapter owns about-topic reading; HTML and console markup remain literal.
                view = global::ConsoleMonster.Markdown.MarkdownConsole.Parse(text);
            } else {
                view = PSHelp.CreateView(help, command, NoExamples.IsPresent);
            }
            PSFormatting.Emit(this, view, Width, null, AsString.IsPresent);
        }

        // The help object of the command PowerShell resolved (the same name in another module or version comes second).
        private static PSObject? Matching(List<PSObject> entries, CommandInfo? command) {
            if (entries.Count == 0) {
                return null;
            }
            CommandInfo? resolved = command is AliasInfo alias && alias.ResolvedCommand != null ? alias.ResolvedCommand : command;
            if (resolved != null && entries.Count > 1) {
                foreach (PSObject entry in entries) {
                    string? module = Convert.ToString(entry.Properties["ModuleName"]?.Value, System.Globalization.CultureInfo.InvariantCulture);
                    string? name = Convert.ToString(entry.Properties["Name"]?.Value, System.Globalization.CultureInfo.InvariantCulture);
                    if (string.Equals(name, resolved.Name, StringComparison.OrdinalIgnoreCase) && string.Equals(module ?? string.Empty, resolved.ModuleName ?? string.Empty, StringComparison.OrdinalIgnoreCase)) {
                        return entry;
                    }
                }
            }
            return entries[0];
        }

        private static Table IndexTable(List<PSObject> entries) {
            Table table = new Table { RecordViewRows = 0 };
            table.AddColumn(new TableColumn("Name") { IsKey = true });
            table.AddColumn(new TableColumn("Category"));
            table.AddColumn(new TableColumn("Synopsis") { MaxLines = 2 });
            foreach (PSObject entry in entries) {
                table.AddRow(
                    Text.Markup.Escape(Convert.ToString(entry.Properties["Name"]?.Value, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty),
                    "[muted]" + Text.Markup.Escape(Convert.ToString(entry.Properties["Category"]?.Value, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty) + "[/]",
                    Text.Markup.Escape((Convert.ToString(entry.Properties["Synopsis"]?.Value, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty).Trim()));
            }
            return table;
        }
    }
}
