using System;
using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Views;

namespace ConsoleMonster.PowerShell {
    /// <summary>How objects of a registered type are shown by Enable-ConsoleFormatting (Register-ConsoleView).</summary>
    public sealed class ConsoleViewRegistration {
        internal ConsoleViewRegistration(string typeName, ConsoleViewKind view, object[]? property, object? groupBy, string group, ScriptBlock? title = null) {
            TypeName = typeName;
            View = view;
            Property = property;
            GroupBy = groupBy;
            Group = group;
            Title = title;
        }

        /// <summary>
        /// A script block that gives each object's title ($_ is the object): when all objects of a view share one, it is the
        /// view's title, otherwise rows are grouped by it. Null for no title.
        /// </summary>
        public ScriptBlock? Title { get; }

        /// <summary>The type name (as in PSObject.TypeNames).</summary>
        public string TypeName { get; }

        /// <summary>The view.</summary>
        public ConsoleViewKind View { get; }

        /// <summary>The properties, as in Format-ConsoleTable, or null for the default display set.</summary>
        public object[]? Property { get; }

        /// <summary>A property name or script block that groups rows under headings, or null.</summary>
        public object? GroupBy { get; }

        /// <summary>Types with the same group are shown together in one view (files and folders, for example).</summary>
        public string Group { get; }
    }

    /// <summary>
    /// The opt-in default formatting: an Out-Default function (defined by Enable-ConsoleFormatting) hands the objects that
    /// reach the host to Out-ConsoleDefault, which shows registered types through ConsoleMonster and passes everything else
    /// to PowerShell's own Out-Default. Only output that would be displayed on the console is affected.
    /// </summary>
    internal static class ConsoleFormatting {
        private static readonly object Gate = new object();
        private static readonly Dictionary<string, ConsoleViewRegistration> Views = new Dictionary<string, ConsoleViewRegistration>(StringComparer.OrdinalIgnoreCase);

        internal const string Marker = "ConsoleMonster\\Out-ConsoleDefault";

        // The Out-Default shim: everything goes through Out-ConsoleDefault while ConsoleMonster is loaded, and straight to
        // PowerShell's Out-Default if the module was removed.
        internal const string ShimScript = @"
    [CmdletBinding()]
    param([switch] $Transcript, [Parameter(ValueFromPipeline = $true)] [psobject] $InputObject)
    begin {
        $cmTarget = if (Get-Command -Name 'ConsoleMonster\Out-ConsoleDefault' -CommandType Cmdlet -ErrorAction Ignore) { { ConsoleMonster\Out-ConsoleDefault -Transcript:$Transcript } } else { { Microsoft.PowerShell.Core\Out-Default -Transcript:$Transcript } }
        $cmPipeline = $cmTarget.GetSteppablePipeline($MyInvocation.CommandOrigin)
        $cmPipeline.Begin($PSCmdlet)
    }
    process { $cmPipeline.Process($InputObject) }
    end { $cmPipeline.End() }
";

        internal static IReadOnlyCollection<ConsoleViewRegistration> Registrations {
            get {
                lock (Gate) {
                    return new List<ConsoleViewRegistration>(Views.Values);
                }
            }
        }

        internal static void Register(ConsoleViewRegistration registration) {
            lock (Gate) {
                Views[registration.TypeName] = registration;
            }
        }

        internal static bool Unregister(string typeName) {
            lock (Gate) {
                return Views.Remove(typeName);
            }
        }

        internal static void RegisterBuiltIns() {
            ScriptBlock directory = ScriptBlock.Create("$p = [string] $_.PSParentPath; 'Directory: ' + ($p -replace '^Microsoft\\.PowerShell\\.Core\\\\FileSystem::', '')");
            object[] files = {
                "Mode",
                Hashtable("LastWriteTime", ScriptBlock.Create("$_.LastWriteTime"), "Date"),
                Hashtable("Length", ScriptBlock.Create("if ($_ -is [System.IO.FileInfo]) { $_.Length }"), "Bytes"),
                "Name"
            };
            foreach (string type in new[] { "System.IO.FileInfo", "System.IO.DirectoryInfo" }) {
                if (Find(type) == null) {
                    Register(new ConsoleViewRegistration(type, ConsoleViewKind.Table, files, null, "FileSystem", directory));
                }
            }
            if (Find("System.Diagnostics.Process") == null) {
                Register(new ConsoleViewRegistration("System.Diagnostics.Process", ConsoleViewKind.Table, new object[] {
                    "Id", "ProcessName",
                    Hashtable("CPU (s)", ScriptBlock.Create("if ($null -ne $_.CPU) { [math]::Round($_.CPU, 1) }"), null),
                    Hashtable("Memory", ScriptBlock.Create("$_.WorkingSet64"), "Bytes"),
                    "Handles"
                }, null, "System.Diagnostics.Process"));
            }
            if (Find("System.ServiceProcess.ServiceController") == null) {
                Register(new ConsoleViewRegistration("System.ServiceProcess.ServiceController", ConsoleViewKind.Table, new object[] {
                    Hashtable("Status", ScriptBlock.Create("[string] $_.Status"), "StatusWord"),
                    "Name", "DisplayName"
                }, null, "System.ServiceProcess.ServiceController"));
            }
        }

        // The registration for an object: the first of its type names that has one.
        internal static ConsoleViewRegistration? Find(PSObject item) {
            lock (Gate) {
                foreach (string type in item.TypeNames) {
                    string name = type.StartsWith("Deserialized.", StringComparison.Ordinal) ? string.Empty : type;
                    if (name.Length > 0 && Views.TryGetValue(name, out ConsoleViewRegistration? registration)) {
                        return registration;
                    }
                }
            }
            return null;
        }

        private static ConsoleViewRegistration? Find(string typeName) {
            lock (Gate) {
                return Views.TryGetValue(typeName, out ConsoleViewRegistration? registration) ? registration : null;
            }
        }

        private static System.Collections.Hashtable Hashtable(string name, ScriptBlock expression, string? presenter) {
            System.Collections.Hashtable table = new System.Collections.Hashtable(StringComparer.OrdinalIgnoreCase) { ["Name"] = name, ["Expression"] = expression };
            if (presenter != null) {
                table["Presenter"] = presenter;
            }
            return table;
        }
    }
}
