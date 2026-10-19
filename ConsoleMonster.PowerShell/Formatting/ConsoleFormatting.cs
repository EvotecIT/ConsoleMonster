using System;
using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Views;

namespace ConsoleMonster.PowerShell {
    /// <summary>How objects of a registered type are shown by Enable-ConsoleFormatting (Register-ConsoleView).</summary>
    public sealed class ConsoleViewRegistration {
        internal ConsoleViewRegistration(string typeName, ConsoleViewKind view, object[]? property, object? groupBy, string group, ScriptBlock? title = null, bool builtIn = false) {
            TypeName = typeName;
            View = view;
            Property = property;
            GroupBy = groupBy;
            Group = group;
            Title = title;
            IsBuiltIn = builtIn;
        }

        /// <summary>Whether this is one of ConsoleMonster's own views (files and folders, processes, services).</summary>
        public bool IsBuiltIn { get; }

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
        private static readonly Dictionary<string, ConsoleViewRegistration> BuiltIns = new Dictionary<string, ConsoleViewRegistration>(StringComparer.OrdinalIgnoreCase);
        private static bool _builtInsReady;

        /// <summary>
        /// Whether Out-ConsoleDefault takes over the built-in types (files, processes, services). Enable-ConsoleFormatting
        /// -NoBuiltIn turns it off; the built-in views still pick the columns of Format-ConsoleTable and the other commands.
        /// </summary>
        internal static bool TakeOverBuiltIns { get; set; } = true;

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
            lock (Gate) {
                if (_builtInsReady) {
                    return;
                }
            }
            ScriptBlock directory = ScriptBlock.Create("$p = [string] $_.PSParentPath; 'Directory: ' + ($p -replace '^Microsoft\\.PowerShell\\.Core\\\\FileSystem::', '')");
            object[] files = {
                "Mode",
                Hashtable("LastWriteTime", ScriptBlock.Create("$_.LastWriteTime"), "Date"),
                Hashtable("Length", ScriptBlock.Create("if ($_ -is [System.IO.FileInfo]) { $_.Length }"), "Bytes"),
                "Name"
            };
            foreach (string type in new[] { "System.IO.FileInfo", "System.IO.DirectoryInfo" }) {
                AddBuiltIn(new ConsoleViewRegistration(type, ConsoleViewKind.Table, files, null, "FileSystem", directory, true));
            }
            {
                AddBuiltIn(new ConsoleViewRegistration("System.Diagnostics.Process", ConsoleViewKind.Table, new object[] {
                    "Id", "ProcessName",
                    Hashtable("CPU (s)", ScriptBlock.Create("if ($null -ne $_.CPU) { [math]::Round($_.CPU, 1) }"), null),
                    Hashtable("Memory", ScriptBlock.Create("$_.WorkingSet64"), "Bytes"),
                    "Handles"
                }, null, "System.Diagnostics.Process", null, true));
            }
            AddBuiltIn(new ConsoleViewRegistration("System.ServiceProcess.ServiceController", ConsoleViewKind.Table, new object[] {
                Hashtable("Status", ScriptBlock.Create("[string] $_.Status"), "StatusWord"),
                "Name", "DisplayName"
            }, null, "System.ServiceProcess.ServiceController", null, true));
            // Ready only once every built-in is in: a concurrent lookup never sees half of them.
            lock (Gate) {
                _builtInsReady = true;
            }
        }

        private static void AddBuiltIn(ConsoleViewRegistration registration) {
            lock (Gate) {
                BuiltIns[registration.TypeName] = registration;
            }
        }

        // The registration for an object: the first of its type names that has one (a user registration before a built-in
        // one for the same type name).
        internal static ConsoleViewRegistration? Find(PSObject item, bool includeBuiltIns = true) {
            if (includeBuiltIns) {
                RegisterBuiltIns();
            }
            lock (Gate) {
                // User registrations for any of the type names (FileSystemInfo included) win over built-in ones.
                foreach (string type in item.TypeNames) {
                    if (!type.StartsWith("Deserialized.", StringComparison.Ordinal) && Views.TryGetValue(type, out ConsoleViewRegistration? registration)) {
                        return registration;
                    }
                }
                if (includeBuiltIns) {
                    foreach (string type in item.TypeNames) {
                        if (!type.StartsWith("Deserialized.", StringComparison.Ordinal) && BuiltIns.TryGetValue(type, out ConsoleViewRegistration? builtIn)) {
                            return builtIn;
                        }
                    }
                }
            }
            return null;
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
