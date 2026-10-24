using System;
using System.Collections;
using System.IO;
using System.Management.Automation;
using System.Runtime.InteropServices;

namespace ConsoleMonster.PowerShell {
    /// <summary>Implicit host-only Unix defaults; explicit native properties and views keep their own contracts.</summary>
    internal static class UnixFormatDefaults {
        // Typed cells keep names literal while preserving Unix directory, link and executable cues.
        internal const string FileNameScript = "$name = [ConsoleMonster.Text.Markup]::Escape($_.Name); "
            + "$tone = if ($_.UnixMode -like 'l*') { 'accent2' } elseif ($_.PSIsContainer) { 'accent' } elseif ($_.UnixMode -match 'x') { 'pass' }; "
            + "if ($tone) { [ConsoleMonster.Widgets.TableCell]::new(('[' + $tone + ']' + $name + '[/]')) } else { [ConsoleMonster.Widgets.TableCell]::new($name) }";

        internal static NativeFormatDefinition? For(PSObject first, string kind) {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) { return null; }
            bool file = first.BaseObject is FileSystemInfo;
            bool process = first.BaseObject is System.Diagnostics.Process;
            if (!file && !process) { return null; }
            NativeFormatDefinition result = new NativeFormatDefinition();
            string[] fields = file
                ? kind == "List" ? new[] { "Name", "FullName", "UnixMode", "User", "Group", "Length", "LastWriteTime", "LinkType", "Target" }
                    : new[] { "UnixMode", "User", "LastWriteTime", "Length", "Name" }
                : new[] { "Id", "ProcessName", "CPU", "WorkingSet64", "Threads" };
            foreach (string field in fields) {
                string label = field == "Length" ? "Size" : field == "WorkingSet64" ? "Memory" : field == "CPU" ? "CPU (s)" : field;
                object expression = file && field == "Name" ? (object)ScriptBlock.Create(FileNameScript) : field == "Threads" ? (object)ScriptBlock.Create("$_.Threads.Count")
                    : field == "Length" ? ScriptBlock.Create("$_.UnixStat.Size") : field;
                result.Columns.Add(new Hashtable(StringComparer.OrdinalIgnoreCase) { ["Label"] = label, ["Expression"] = expression });
            }
            return result;
        }
    }
}
