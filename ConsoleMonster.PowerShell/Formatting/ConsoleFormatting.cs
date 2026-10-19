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

        /// <summary>The profile lines that turn the formatting on in every session, with the given options (printed, never written).</summary>
        internal static IReadOnlyList<string> ProfileSnippet(string? profilePath, bool noBuiltIn, bool force) {
            string options = (noBuiltIn ? " -NoBuiltIn" : string.Empty) + (force ? " -Force" : string.Empty);
            return new[] {
                "# ConsoleMonster: nicer default output in every session." + (string.IsNullOrEmpty(profilePath) ? string.Empty : " Add these lines to " + profilePath),
                "Import-Module ConsoleMonster",
                "Enable-ConsoleFormatting" + options
            };
        }

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
            // Get-Command: every kind of command in one table.
            object[] commands = { Hashtable("CommandType", ScriptBlock.Create("[string] $_.CommandType"), null), Hashtable("Name", ScriptBlock.Create("if ($_.CommandType -eq 'Alias') { $_.DisplayName } else { $_.Name }"), null), Hashtable("Version", ScriptBlock.Create("if ($_.Version -and $_.Version -ne [version] '0.0') { [string] $_.Version }"), null), "Source" };
            foreach (string type in new[] { "System.Management.Automation.CmdletInfo", "System.Management.Automation.FunctionInfo", "System.Management.Automation.FilterInfo", "System.Management.Automation.AliasInfo", "System.Management.Automation.ApplicationInfo", "System.Management.Automation.ExternalScriptInfo", "System.Management.Automation.ConfigurationInfo" }) {
                AddBuiltIn(new ConsoleViewRegistration(type, ConsoleViewKind.Table, commands, null, "Command", null, true));
            }
            // Get-Module: the version and how many commands each module exports.
            AddBuiltIn(new ConsoleViewRegistration("System.Management.Automation.PSModuleInfo", ConsoleViewKind.Table, new object[] {
                Hashtable("ModuleType", ScriptBlock.Create("[string] $_.ModuleType"), null),
                Hashtable("Version", ScriptBlock.Create("[string] $_.Version"), null),
                "Name",
                Hashtable("Commands", ScriptBlock.Create("$_.ExportedCommands.Count"), null)
            }, null, "Module", null, true));
            // Get-WinEvent: time, id, the level as a status word, the source and the message's first line.
            AddBuiltIn(new ConsoleViewRegistration("System.Diagnostics.Eventing.Reader.EventLogRecord", ConsoleViewKind.Table, new object[] {
                Hashtable("TimeCreated", ScriptBlock.Create("$_.TimeCreated"), "Date"),
                Hashtable("Id", ScriptBlock.Create("$_.Id"), "Identifier"),
                Hashtable("Level", ScriptBlock.Create("[string] $_.LevelDisplayName"), "StatusWord"),
                "ProviderName",
                Hashtable("Message", ScriptBlock.Create("if ($_.Message) { ($_.Message -split '\r?\n')[0] }"), null)
            }, null, "EventLogRecord", null, true));
            // Get-NetIPAddress and Get-Volume (CIM objects).
            object[] addresses = { "InterfaceAlias", Hashtable("ifIndex", ScriptBlock.Create("$_.ifIndex"), "Identifier"), "IPAddress", "PrefixLength", Hashtable("AddressFamily", ScriptBlock.Create("[string] $_.AddressFamily"), null), Hashtable("AddressState", ScriptBlock.Create("[string] $_.AddressState"), null) };
            foreach (string type in new[] { "Microsoft.Management.Infrastructure.CimInstance#ROOT/StandardCimv2/MSFT_NetIPAddress", "Microsoft.Management.Infrastructure.CimInstance#MSFT_NetIPAddress" }) {
                AddBuiltIn(new ConsoleViewRegistration(type, ConsoleViewKind.Table, addresses, null, "NetIPAddress", null, true));
            }
            object[] volumes = {
                "DriveLetter", "FriendlyName", "FileSystemType",
                Hashtable("Health", ScriptBlock.Create("[string] $_.HealthStatus"), "StatusWord"),
                Hashtable("Free", ScriptBlock.Create("$_.SizeRemaining"), "Bytes"),
                Hashtable("Size", ScriptBlock.Create("$_.Size"), "Bytes")
            };
            foreach (string type in new[] { "Microsoft.Management.Infrastructure.CimInstance#ROOT/Microsoft/Windows/Storage/MSFT_Volume", "Microsoft.Management.Infrastructure.CimInstance#MSFT_Volume" }) {
                AddBuiltIn(new ConsoleViewRegistration(type, ConsoleViewKind.Table, volumes, null, "Volume", null, true));
            }
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
