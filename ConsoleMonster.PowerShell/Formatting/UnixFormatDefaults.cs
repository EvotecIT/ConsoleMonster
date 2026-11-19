using System;
using System.Collections;
using System.IO;
using System.Management.Automation;
using System.Runtime.InteropServices;

namespace ConsoleMonster.PowerShell {
    /// <summary>Implicit host-only Unix defaults; explicit native properties and views keep their own contracts.</summary>
    internal static class UnixFormatDefaults {
        // A directory entry's own byte count is not its contents' size. Links retain their own size, including directory links.
        internal const string FileSizeScript = "if (-not $_.PSIsContainer -or $_.UnixMode -like 'l*') { $_.UnixStat.Size }";

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
                if (file && field == "Name") {
                    // Names stay literal and keep their kind's colour (FileKinds), as in the registered view.
                    Hashtable name = ConsoleFormatting.FileName();
                    name.Remove("Name");
                    name["Label"] = "Name";
                    result.Columns.Add(name);
                    continue;
                }
                string label = field == "Length" ? "Size" : field == "WorkingSet64" ? "Memory" : field == "CPU" ? "CPU (s)" : field;
                object expression = field == "Threads" ? (object)ScriptBlock.Create("$_.Threads.Count")
                    : field == "Length" ? ScriptBlock.Create(FileSizeScript) : field;
                result.Columns.Add(new Hashtable(StringComparer.OrdinalIgnoreCase) { ["Label"] = label, ["Expression"] = expression });
            }
            return result;
        }
    }
}
