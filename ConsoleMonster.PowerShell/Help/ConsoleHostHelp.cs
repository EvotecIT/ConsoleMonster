using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>Reads native runtime help into the same view used by Show-ConsoleHelp, using the caller's owning runspace.</summary>
    public static class ConsoleHostHelp {
        /// <summary>Resolves a native help topic without writing or acquiring terminal input.</summary>
        /// <param name="name">Command, script, about topic or search pattern.</param>
        /// <param name="invokeCommand">Invocation from the owning PowerShell runspace.</param>
        /// <param name="widget">The complete help view when resolution succeeds.</param>
        /// <param name="error">The native or descriptive lookup error when resolution fails.</param>
        /// <param name="noExamples">Suppresses default-on help examples.</param>
        /// <returns>Whether a help view was found.</returns>
        public static bool TryCreate(string name, CommandInvocationIntrinsics invokeCommand, out IWidget? widget, out ErrorRecord? error, bool noExamples = false) {
            if (invokeCommand == null) { throw new ArgumentNullException(nameof(invokeCommand)); }
            if (name == null) { throw new ArgumentNullException(nameof(name)); }
            widget = null;
            error = null;
            CommandInfo? command = null;
            bool wildcard = WildcardPattern.ContainsWildcardCharacters(name);
            bool topic = name.StartsWith("about_", StringComparison.OrdinalIgnoreCase) || wildcard;
            if (!topic) {
                try {
                    command = invokeCommand.GetCommand(name, CommandTypes.All);
                } catch (Exception ex) when (!(ex is PipelineStoppedException)) {
                    command = null;
                }
            }
            // Patterns and about topics belong to Get-Help. Looking them up as literal commands first invokes
            // missing-command discovery and module analysis before Get-Help's own search.
            if (command is ApplicationInfo) {
                // A program has no PowerShell help; Get-Help would search every help file first.
                error = new ErrorRecord(new ItemNotFoundException("'" + name + "' is a program; try " + name + " /? or " + name + " --help."), "ProgramHasNoHelp", ErrorCategory.ObjectNotFound, name);
                return false;
            }
            if (command == null && !topic) {
                // Get-Help searches every help file before it gives up on an unknown name, which takes many seconds.
                error = new ErrorRecord(new ItemNotFoundException("'" + name + "' is not a command, function, alias or script here."), "CommandNotFound", ErrorCategory.ObjectNotFound, name);
                return false;
            }
            Collection<PSObject> found;
            try {
                found = invokeCommand.InvokeScript(true, ScriptBlock.Create("param($__cmName) Microsoft.PowerShell.Core\\Get-Help -name $__cmName -Full -ErrorAction Stop"), null, name);
            } catch (RuntimeException ex) {
                // Get-Help's own error, not the -ErrorAction Stop wrapper around it.
                error = ex.ErrorRecord != null && !(ex.ErrorRecord.Exception is ActionPreferenceStopException) ? ex.ErrorRecord : new ErrorRecord(ex.InnerException ?? ex, "HelpNotFound", ErrorCategory.ObjectNotFound, name);
                return false;
            }
            List<PSObject> entries = new List<PSObject>();
            foreach (PSObject item in found) {
                if (item != null) {
                    entries.Add(item);
                }
            }
            if (entries.Count > 1 && command == null) {
                // A wildcard or a topic search: Get-Help gives short entries, shown as a list to pick from.
                widget = IndexTable(entries);
                return true;
            }
            PSObject? help = Matching(entries, command);
            if (help == null) {
                error = new ErrorRecord(new ItemNotFoundException("No help was found for '" + name + "'."), "HelpNotFound", ErrorCategory.ObjectNotFound, name);
                return false;
            }
            IWidget view;
            if (help.BaseObject is string text) {
                // The optional Markdown adapter owns about-topic reading; HTML and console markup remain literal.
                view = MarkdownExportBridge.Parse(text);
            } else {
                view = PSHelp.CreateView(help, command, noExamples);
            }
            widget = view;
            return true;
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
