using System;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Runspaces;

namespace ConsoleMonster.PowerShell {
    /// <summary>Owns reversible command installations using runtime metadata and shared resource-backed proxy bodies.</summary>
    internal sealed class FormatCommandInstallation {
        internal const string Marker = "ConsoleMonster FormatCommands proxy";
        private readonly SessionState _session;
        private readonly PSModuleInfo _module;
        private readonly Dictionary<string, ScriptBlock?> _saved = new Dictionary<string, ScriptBlock?>();
        private readonly Dictionary<string, ScriptBlock> _installed = new Dictionary<string, ScriptBlock>();
        internal ScriptBlock? NativeHelp { get; private set; }

        private FormatCommandInstallation(SessionState session, PSModuleInfo module) { _session = session; _module = module; }

        internal static bool Install(PSCmdlet cmdlet, bool force) {
            FormattingSessionState state = FormattingSessionState.Current;
            if (state.FormatCommands != null) {
                return true;
            }
            string[] names = { "Format-Table", "Format-List", "Format-Wide" };
            foreach (string name in names) {
                CommandInfo? existing = cmdlet.InvokeCommand.GetCommand(name, CommandTypes.Function | CommandTypes.Alias);
                if (existing is AliasInfo || (existing != null && !force)) {
                    cmdlet.WriteWarning(name + " already has a custom function or alias; FormatCommands was not enabled. Remove the alias or use -Force for a function.");
                    return false;
                }
            }
            state.FormatCommands = InstallCommands(cmdlet, names, "Formatting.FormatCommandProxy.ps1",
                name => cmdlet.InvokeCommand.GetCommand("Microsoft.PowerShell.Utility\\" + name, CommandTypes.Cmdlet));
            return true;
        }

        internal static bool InstallProgress(PSCmdlet cmdlet, bool force) {
            FormattingSessionState state = FormattingSessionState.Current;
            if (state.NativeProgress != null) { return true; }
            CommandInfo? existing = cmdlet.InvokeCommand.GetCommand("Write-Progress", CommandTypes.Function | CommandTypes.Alias);
            if (existing is AliasInfo || (existing != null && !force)) {
                cmdlet.WriteWarning("Write-Progress already has a custom function or alias; Progress was not enabled. Remove the alias or use -Force for a function.");
                return false;
            }
            state.NativeProgress = InstallCommands(cmdlet, new[] { "Write-Progress" }, "Formatting.NativeProgressProxy.ps1",
                name => cmdlet.InvokeCommand.GetCommand("Microsoft.PowerShell.Utility\\" + name, CommandTypes.Cmdlet));
            SessionProgress.Enabled = true;
            return true;
        }

        internal static void RemoveProgress(FormattingSessionState state) {
            state.NativeProgress?.Restore();
            state.NativeProgress = null;
        }

        internal static bool InstallHelp(PSCmdlet cmdlet, bool force) {
            FormattingSessionState state = FormattingSessionState.Current;
            if (state.Help != null) { return true; }
            CommandInfo command = cmdlet.InvokeCommand.GetCommand("help", CommandTypes.All);
            string? definition = null;
            foreach (SessionStateCommandEntry entry in InitialSessionState.CreateDefault2().Commands) {
                if (entry is SessionStateFunctionEntry function && string.Equals(entry.Name, "help", StringComparison.OrdinalIgnoreCase)) {
                    definition = function.Definition;
                    break;
                }
            }
            bool builtIn = command is FunctionInfo existing && definition != null && string.Equals(existing.Definition, definition, StringComparison.Ordinal);
            if (command is AliasInfo || (!(command is FunctionInfo) || (!builtIn && !force))) {
                cmdlet.WriteWarning("help is not the built-in function; Help was not enabled. Remove the alias or use -Force for a function.");
                return false;
            }
            ScriptBlock native = builtIn ? ((FunctionInfo)command).ScriptBlock : ScriptBlock.Create(definition!);
            CommandInfo metadata = builtIn ? command : cmdlet.InvokeCommand.GetCommand("Microsoft.PowerShell.Core\\Get-Help", CommandTypes.Cmdlet);
            state.Help = InstallCommands(cmdlet, new[] { "help" }, "Help.HelpProxy.ps1", name => metadata);
            state.Help.NativeHelp = native;
            return true;
        }

        private static FormatCommandInstallation InstallCommands(PSCmdlet cmdlet, string[] names, string resource, Func<string, CommandInfo> resolve) {
            FormatCommandInstallation installation = new FormatCommandInstallation(cmdlet.SessionState, cmdlet.MyInvocation.MyCommand.Module);
            try {
                string body = ReadResource(resource);
                foreach (string name in names) {
                    ScriptBlock? existing = GlobalFunction(cmdlet.SessionState, name);
                    installation._saved.Add(name, existing);
                    CommandInfo native = resolve(name);
                    CommandMetadata metadata = new CommandMetadata(native);
                    string proxy = ProxyCommand.GetCmdletBindingAttribute(metadata) + "\nparam(\n" + ProxyCommand.GetParamBlock(metadata) + "\n)\n"
                        + body.Replace("__COMMAND__", name);
                    cmdlet.SessionState.InvokeProvider.Item.Set("Function:\\global:" + name, ScriptBlock.Create(proxy));
                    installation._installed[name] = GlobalFunction(cmdlet.SessionState, name)!;
                }
                return installation;
            } catch {
                installation.Restore();
                throw;
            }
        }

        internal static string ReadResource(string resource) {
            using (Stream stream = typeof(FormatCommandInstallation).Assembly.GetManifestResourceStream("ConsoleMonster.PowerShell." + resource)
                ?? throw new InvalidOperationException("The native command resource is missing: " + resource))
            using (StreamReader reader = new StreamReader(stream)) { return reader.ReadToEnd(); }
        }

        internal static void RemoveHelp(FormattingSessionState state) {
            state.Help?.Restore();
            state.Help = null;
        }

        internal static void Remove(FormattingSessionState state) {
            state.FormatCommands?.Restore();
            state.FormatCommands = null;
        }

        private void Restore() {
            foreach (KeyValuePair<string, ScriptBlock?> pair in _saved) {
                // A function the user replaced after enabling belongs to the user now.
                ScriptBlock? function = GlobalFunction(_session, pair.Key);
                // The function drive can return a new ScriptBlock wrapper for the same definition.
                if (function == null || !_installed.TryGetValue(pair.Key, out ScriptBlock? owned) || !string.Equals(function.ToString(), owned.ToString(), StringComparison.Ordinal)) {
                    continue;
                }
                if (pair.Value == null) {
                    RemoveGlobalFunction(_module, pair.Key);
                } else {
                    _session.InvokeProvider.Item.Set("Function:\\global:" + pair.Key, pair.Value);
                }
            }
        }

        // The function drive's content binding understands scopes; Get-Item does not resolve global:name metadata.
        internal static ScriptBlock? GlobalFunction(SessionState session, string name) => session.PSVariable.GetValue("function:global:" + name) as ScriptBlock;

        // Removing a function has no scope-qualified provider path. The defining module's root scope sees the global
        // slots and excludes temporary functions in the caller's scope.
        internal static void RemoveGlobalFunction(PSModuleInfo module, string name) => module.Invoke(
            ScriptBlock.Create("param($name) Microsoft.PowerShell.Management\\Remove-Item -LiteralPath ('Function:' + $name) -Force"), name);
    }
}
