using System;
using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Views;

namespace ConsoleMonster.PowerShell {
    /// <summary>How objects of a registered type are shown by Enable-ConsoleFormatting (Register-ConsoleView).</summary>
    public sealed class ConsoleViewRegistration {
        internal ConsoleViewRegistration(string typeName, ConsoleViewKind view, object[]? property, object? groupBy, string group, ScriptBlock? title = null, bool builtIn = false, bool nativePresentation = false, bool groupHeadersAsRows = false, int groupMinimumRows = 0, bool groupRepeatedOnly = false, bool structuredData = false) {
            TypeName = typeName;
            View = view;
            Property = property;
            GroupBy = groupBy;
            Group = group;
            Title = title;
            IsBuiltIn = builtIn;
            NativePresentation = nativePresentation;
            GroupHeadersAsRows = groupHeadersAsRows;
            GroupMinimumRows = groupMinimumRows;
            GroupRepeatedOnly = groupRepeatedOnly;
            IsStructuredData = structuredData;
        }

        // These registrations also decorate native-selected columns; user property/view overrides stay engine-owned.
        internal bool NativePresentation { get; }

        /// <summary>Uses the table's full-width group section rows.</summary>
        internal bool GroupHeadersAsRows { get; }

        // Built-in grouping stays quiet for small results and single-version module lists.
        internal int GroupMinimumRows { get; }
        internal bool GroupRepeatedOnly { get; }

        // Presentation policy is independent of the caller-controlled batching group.
        internal bool IsStructuredData { get; }

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

        /// <summary>The properties, as in Show-ConsoleTable, or null for the default display set.</summary>
        public object[]? Property { get; }

        /// <summary>A property name or script block that groups rows under headings, or null.</summary>
        public object? GroupBy { get; }

        /// <summary>Types with the same group are shown together in one view (files and folders, for example).</summary>
        public string Group { get; }
    }

    /// <summary>What Enable-ConsoleFormatting turns on; Disable-ConsoleFormatting turns the same parts off.</summary>
    [Flags]
    public enum ConsoleFormattingFeature {
        /// <summary>Nothing.</summary>
        None = 0,

        /// <summary>Registered and built-in types (files, processes, services, dictionaries) shown as ConsoleMonster views (the default).</summary>
        Output = 1,

        /// <summary>Errors that reach the console shown with ConsoleMonster's error view (Show-ConsoleError), following $ErrorView.</summary>
        ErrorView = 2,

        /// <summary>Write-Progress drawn as ConsoleMonster's progress area (Write-Progress uses its native-parameter function proxy).</summary>
        Progress = 4,

        /// <summary>Explicitly opts Format-Table, Format-List and Format-Wide into host-only rendering. Excluded from All.</summary>
        FormatCommands = 8,

        /// <summary>Native stream and progress colours from the active theme; commands, records and progress view stay native.</summary>
        Streams = 16,

        /// <summary>Minimal host-only shell prompt with path, cached git state, status and completed command duration. Excluded from All.</summary>
        Prompt = 512,

        /// <summary>Falls back to Show-ConsoleGrid only when Out-GridView is unavailable; excluded from All.</summary>
        GridView = 256,

        /// <summary>Native command help displayed through HelpView only on a direct local host route. Excluded from All.</summary>
        Help = 32,

        /// <summary>Compact, message-first errors only on a direct, non-recorded host route. Excluded from All.</summary>
        Errors = 64,

        /// <summary>Syntax, selection and inline prediction colours for already loaded PSReadLine; editing behaviour stays native.</summary>
        ReadLine = 128,

        /// <summary>Default output, errors, native stream/progress and ReadLine colours. Progress rendering and FormatCommands require separate explicit opt-ins.</summary>
        All = Output | ErrorView | Streams | ReadLine
    }

    /// <summary>
    /// The opt-in default formatting: an Out-Default function (defined by Enable-ConsoleFormatting) hands the objects that
    /// reach the host to Out-ConsoleDefault, which shows registered types through ConsoleMonster and passes everything else
    /// to PowerShell's own Out-Default. Only output that would be displayed on the console is affected.
    /// </summary>
    internal static partial class ConsoleFormatting {
        private static readonly object Gate = new object();
        private static Dictionary<string, ConsoleViewRegistration> Views => FormattingSessionState.Current.Views;
        private static readonly Dictionary<string, ConsoleViewRegistration> BuiltIns = new Dictionary<string, ConsoleViewRegistration>(StringComparer.OrdinalIgnoreCase);
        private static bool _builtInsReady;

        /// <summary>
        /// Whether Out-ConsoleDefault takes over the built-in types (files, processes, services). Enable-ConsoleFormatting
        /// -NoBuiltIn turns it off; the built-in views still pick the columns of Show-ConsoleTable and the other commands.
        /// </summary>
        internal static bool TakeOverBuiltIns { get => FormattingSessionState.Current.TakeOverBuiltIns; set => FormattingSessionState.Current.TakeOverBuiltIns = value; }

        /// <summary>The features turned on in this session.</summary>
        internal static ConsoleFormattingFeature Features { get => FormattingSessionState.Current.Features; set => FormattingSessionState.Current.Features = value; }

        /// <summary>The user's own Out-Default function replaced with -Force, restored by Disable-ConsoleFormatting.</summary>
        internal static ScriptBlock? SavedOutDefault { get => FormattingSessionState.Current.SavedOutDefault; set => FormattingSessionState.Current.SavedOutDefault = value; }

        internal const string Marker = "ConsoleMonster\\Out-ConsoleDefault";

        /// <summary>Whether a command's output is displayed on a real local console: not redirected, not a job, remoting or another host.</summary>
        internal static bool Displayed(PSCmdlet cmdlet) {
            if (!string.Equals(cmdlet.Host?.Name, "ConsoleHost", StringComparison.Ordinal)) {
                return false;
            }
            try {
                if (Console.IsOutputRedirected) {
                    return false;
                }
            } catch (System.IO.IOException) {
                return false;
            }
            return Terminal.IsConsole(Terminal.Target);
        }

        /// <summary>The profile lines that turn the formatting on in every session, with the given options (printed, never written).</summary>
        internal static IReadOnlyList<string> ProfileSnippet(string? profilePath, bool noBuiltIn, bool force, ConsoleFormattingFeature features = ConsoleFormattingFeature.Output) {
            string options = (features == ConsoleFormattingFeature.Output ? string.Empty : " -Feature " + FeatureList(features))
                + (noBuiltIn ? " -NoBuiltIn" : string.Empty) + (force ? " -Force" : string.Empty);
            return new[] {
                "# ConsoleMonster: nicer default output in every session." + (string.IsNullOrEmpty(profilePath) ? string.Empty : " Add these lines to " + profilePath),
                "Import-Module ConsoleMonster",
                "Enable-ConsoleFormatting" + options
            };
        }

        /// <summary>Restores the owned Write-Progress function and ends the progress area.</summary>
        internal static void RemoveProgress() {
            SessionProgress.EndPipeline();
            SessionProgress.Enabled = false;
            Features &= ~ConsoleFormattingFeature.Progress;
            FormatCommandInstallation.RemoveProgress(FormattingSessionState.Current);
        }

        /// <summary>
        /// Restores commands still owned by this installation when its module is removed, on PowerShell 5.1 and 7.
        /// Captured backups stay usable even if PowerShell's assembly cleanup has already reset the installation state.
        /// </summary>
        internal static void RestoreCommandsWithModule(PSModuleInfo? module) {
            if (module == null) {
                return;
            }
            ScriptBlock? previous = module.OnRemove;
            FormattingSessionState state = FormattingSessionState.Current;
            if (ReferenceEquals(previous, state.ProgressCleanupHook)) {
                // Refresh the saved alias after a disable/re-enable, without chaining our obsolete hook.
                previous = state.PreviousModuleOnRemove;
            }
            // A closure keeps the module's earlier OnRemove (for example type accelerator cleanup) running after ours.
            ScriptBlock factory = ScriptBlock.Create("param($__cmPrevious, $__cmOutDefault, $__cmReset) { $__cmProgressCleanup = $true; "
                + "$__cmFunction = ${function:global:Out-Default}; "
                + "if ($__cmFunction -and $__cmFunction.ToString().IndexOf('" + Marker + "', [System.StringComparison]::OrdinalIgnoreCase) -ge 0) { "
                + "if ($__cmOutDefault) { Microsoft.PowerShell.Management\\Set-Item -Path 'Function:\\global:Out-Default' -Value $__cmOutDefault } "
                + "else { Microsoft.PowerShell.Management\\Remove-Item -Path 'Function:\\Out-Default' -Force } }; "
                + "$__cmReset.Invoke(); "
                + "if ($__cmPrevious) { & $__cmPrevious @args } }.GetNewClosure()");
            System.Collections.ObjectModel.Collection<PSObject> made = factory.Invoke(previous, SavedOutDefault, (Action)(() => { GridViewInstallation.Remove(state); FormatCommandInstallation.RemoveProgress(state); FormatCommandInstallation.Remove(state); FormatCommandInstallation.RemoveHelp(state); state.Reset(); }));
            if (made.Count > 0 && made[0]?.BaseObject is ScriptBlock cleanup) {
                state.PreviousModuleOnRemove = previous;
                state.ProgressCleanupHook = cleanup;
                module.OnRemove = cleanup;
            }
        }

        /// <summary>The features as a -Feature argument: "All" or a comma-separated list.</summary>
        internal static string FeatureList(ConsoleFormattingFeature features) {
            if (features == ConsoleFormattingFeature.All) {
                return "All";
            }
            List<string> names = new List<string>();
            foreach (ConsoleFormattingFeature feature in new[] { ConsoleFormattingFeature.Output, ConsoleFormattingFeature.ErrorView, ConsoleFormattingFeature.Progress, ConsoleFormattingFeature.FormatCommands, ConsoleFormattingFeature.Streams, ConsoleFormattingFeature.Help, ConsoleFormattingFeature.Errors, ConsoleFormattingFeature.ReadLine, ConsoleFormattingFeature.GridView, ConsoleFormattingFeature.Prompt }) {
                if ((features & feature) != 0) {
                    names.Add(feature.ToString());
                }
            }
            return string.Join(", ", names.ToArray());
        }

        // The Out-Default shim: everything goes through Out-ConsoleDefault while ConsoleMonster is loaded, and straight to
        // PowerShell's Out-Default if the module was removed.
        internal static string ShimScript => FormatCommandInstallation.ReadResource("Formatting.OutDefaultProxy.ps1");

        internal static IReadOnlyCollection<ConsoleViewRegistration> Registrations {
            get {
                lock (Gate) {
                    return new List<ConsoleViewRegistration>(Views.Values);
                }
            }
        }

        internal static void Register(ConsoleViewRegistration registration, bool onlyIfMissing = false) {
            lock (Gate) {
                if (!onlyIfMissing || !Views.ContainsKey(registration.TypeName)) {
                    Views[registration.TypeName] = registration;
                }
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
            bool unix = !System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows);
            object[] files = unix ? new object[] {
                "UnixMode", "User",
                Hashtable("LastWriteTime", ScriptBlock.Create("$_.LastWriteTime"), "Date"),
                Hashtable("Size", ScriptBlock.Create(UnixFormatDefaults.FileSizeScript), "Bytes"),
                Hashtable("Name", ScriptBlock.Create(UnixFormatDefaults.FileNameScript), null)
            } : new object[] {
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
                    unix ? (object)Hashtable("Threads", ScriptBlock.Create("$_.Threads.Count"), null) : "Handles"
                }, null, "System.Diagnostics.Process", null, true));
            }
            AddBuiltIn(new ConsoleViewRegistration("System.ServiceProcess.ServiceController", ConsoleViewKind.Table, new object[] {
                Hashtable("Status", ScriptBlock.Create("[string] $_.Status"), "StatusWord"),
                "Name", "DisplayName"
            }, null, "System.ServiceProcess.ServiceController", null, true));
            RegisterEverydayBuiltIns();
            // Get-WinEvent: time, id, the level as a status word, the source and the message's first line.
            AddBuiltIn(new ConsoleViewRegistration("System.Diagnostics.Eventing.Reader.EventLogRecord", ConsoleViewKind.Table, new object[] {
                Hashtable("TimeCreated", ScriptBlock.Create("$_.TimeCreated"), "Date"),
                Hashtable("Id", ScriptBlock.Create("$_.Id"), "Identifier"),
                Hashtable("Level", ScriptBlock.Create("[string] $_.LevelDisplayName"), "StatusWord"),
                "ProviderName",
                Hashtable("Message", ScriptBlock.Create("if ($_.Message) { ($_.Message -split '\r?\n')[0] }"), null)
            }, null, "EventLogRecord", null, true));
            // Windows storage objects keep their existing view. Network registrations are platform-gated in EverydayBuiltInViews.
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
