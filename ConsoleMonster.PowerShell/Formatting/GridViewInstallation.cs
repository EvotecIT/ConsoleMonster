using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation;
using Microsoft.Win32;

namespace ConsoleMonster.PowerShell {
    /// <summary>Installs only the unavailable Out-GridView adapter, with runspace-owned command backups.</summary>
    internal sealed class GridViewInstallation {
        private readonly PSModuleInfo _module;
        private readonly ScriptBlock _body;
        private readonly ScriptBlock _lifecycle;
        private readonly Hashtable _saved;

        private GridViewInstallation(PSModuleInfo module) {
            _module = module;
            _body = ScriptBlock.Create(Resource("GridViewFallback.ps1"));
            _lifecycle = ScriptBlock.Create(Resource("GridViewLifecycle.ps1"));
            object saved = module.Invoke(_lifecycle, "Save", null!, null!);
            _saved = (Hashtable)(saved is PSObject wrapped ? wrapped.BaseObject : saved);
        }

        internal static bool Install(PSCmdlet cmdlet, bool force) {
            FormattingSessionState state = FormattingSessionState.Current;
            if (state.GridView != null) { return true; }
            // Resolve the actual cmdlet, including module discovery. A custom function must never hide a working native
            // cmdlet from this check, and -Force is not permission to replace that native command.
            CommandInfo? native = cmdlet.InvokeCommand.GetCommand("Out-GridView", CommandTypes.Cmdlet);
            if (native != null && NativeUsable(cmdlet, native)) { return false; }
            CommandInfo? custom = cmdlet.InvokeCommand.GetCommand("Out-GridView", CommandTypes.Function | CommandTypes.Alias);
            if (custom != null && !force) {
                cmdlet.WriteWarning(Message("Conflict"));
                return false;
            }
            GridViewInstallation installation = new GridViewInstallation(cmdlet.MyInvocation.MyCommand.Module);
            Hashtable? alias = installation._saved["Alias"] as Hashtable;
            Hashtable? function = installation._saved["Function"] as Hashtable;
            // Local aliases cannot be removed through the module's global installation boundary. Constants cannot be
            // restored safely; leave both alone even with -Force.
            if ((custom is AliasInfo && alias == null) || (custom is FunctionInfo && function == null)
                || IsConstant(alias) || IsConstant(function)) {
                cmdlet.WriteWarning(Message("Immutable"));
                return false;
            }
            try {
                installation._module.Invoke(installation._lifecycle, "Install", installation._body, installation._saved);
                state.GridView = installation;
                return true;
            } catch {
                installation._module.Invoke(installation._lifecycle, "Rollback", installation._body, installation._saved);
                throw;
            }
        }

        internal static void Remove(FormattingSessionState state) {
            state.GridView?.Restore();
            state.GridView = null;
        }

        private void Restore() => _module.Invoke(_lifecycle, "Restore", _body, _saved);

        private static bool IsConstant(Hashtable? saved) => saved?["Options"] is ScopedItemOptions options && (options & ScopedItemOptions.Constant) != 0;

        private static bool NativeUsable(PSCmdlet cmdlet, CommandInfo native) {
            // Third-party implementations own their own capabilities; do not make Windows/WPF assumptions for them.
            if (!(native is CmdletInfo info) || info.ImplementingType.FullName != "Microsoft.PowerShell.Commands.OutGridViewCommand") {
                return true;
            }
            if (!Environment.UserInteractive || cmdlet.SessionState.PSVariable.GetValue("PSSenderInfo") != null
                || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SSH_CONNECTION"))
                || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SSH_CLIENT"))) {
                return false;
            }
            // Presence is the default. Only explicit evidence of a desktop-less Windows installation disables the native
            // cmdlet. Unknown registry access must not turn a working Windows 5.1 command into our fallback.
            if (Environment.OSVersion.Platform == PlatformID.Win32NT) {
                try {
                    using (RegistryKey? key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion")) {
                        string? installation = key?.GetValue("InstallationType") as string;
                        if (string.Equals(installation, "Server Core", StringComparison.OrdinalIgnoreCase)
                            || string.Equals(installation, "Nano Server", StringComparison.OrdinalIgnoreCase)) { return false; }
                    }
                } catch (System.Security.SecurityException) {
                } catch (UnauthorizedAccessException) {
                }
            }
            return true;
        }

        private static readonly Dictionary<string, string> Messages = LoadMessages();

        private static string Message(string name) => Messages[name];

        private static Dictionary<string, string> LoadMessages() {
            Dictionary<string, string> result = new Dictionary<string, string>(StringComparer.Ordinal);
            using (StringReader reader = new StringReader(Resource("GridViewMessages.txt"))) {
                string? line;
                while ((line = reader.ReadLine()) != null) {
                    int equals = line.IndexOf('=');
                    if (equals > 0) { result.Add(line.Substring(0, equals), line.Substring(equals + 1)); }
                }
            }
            return result;
        }

        private static string Resource(string name) {
            using (Stream stream = typeof(GridViewInstallation).Assembly.GetManifestResourceStream("ConsoleMonster.PowerShell.Formatting." + name)
                ?? throw new InvalidOperationException(name))
            using (StreamReader reader = new StreamReader(stream)) { return reader.ReadToEnd(); }
        }
    }
}
