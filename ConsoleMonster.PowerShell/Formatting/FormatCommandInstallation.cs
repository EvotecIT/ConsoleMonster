using System;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation;

namespace ConsoleMonster.PowerShell {
    /// <summary>Owns the three reversible, runtime-native Format-* proxies in one runspace.</summary>
    internal sealed class FormatCommandInstallation {
        internal const string Marker = "ConsoleMonster FormatCommands proxy";
        private readonly SessionState _session;
        private readonly PSModuleInfo _module;
        private readonly Dictionary<string, ScriptBlock?> _saved = new Dictionary<string, ScriptBlock?>();
        private readonly Dictionary<string, ScriptBlock> _installed = new Dictionary<string, ScriptBlock>();

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
            FormatCommandInstallation installation = new FormatCommandInstallation(cmdlet.SessionState, cmdlet.MyInvocation.MyCommand.Module);
            try {
                string body;
                using (Stream stream = typeof(FormatCommandInstallation).Assembly.GetManifestResourceStream("ConsoleMonster.PowerShell.Formatting.FormatCommandProxy.ps1")
                    ?? throw new InvalidOperationException("The FormatCommands proxy resource is missing."))
                using (StreamReader reader = new StreamReader(stream)) { body = reader.ReadToEnd(); }
                foreach (string name in names) {
                    ScriptBlock? existing = GlobalFunction(cmdlet.SessionState, name);
                    installation._saved.Add(name, existing);
                    CommandInfo native = cmdlet.InvokeCommand.GetCommand("Microsoft.PowerShell.Utility\\" + name, CommandTypes.Cmdlet);
                    CommandMetadata metadata = new CommandMetadata(native);
                    string proxy = ProxyCommand.GetCmdletBindingAttribute(metadata) + "\nparam(\n" + ProxyCommand.GetParamBlock(metadata) + "\n)\n"
                        + body.Replace("__COMMAND__", name);
                    cmdlet.SessionState.InvokeProvider.Item.Set("Function:\\global:" + name, ScriptBlock.Create(proxy));
                    installation._installed[name] = GlobalFunction(cmdlet.SessionState, name)!;
                }
                state.FormatCommands = installation;
                return true;
            } catch {
                installation.Restore();
                throw;
            }
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
