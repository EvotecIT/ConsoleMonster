#if !NETFRAMEWORK
using System.Management.Automation;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// Resets the current runspace's formatting state when PowerShell 7 removes the binary module.
    /// The module's OnRemove closure restores commands from its captured backups on PowerShell 5.1 and 7.
    /// </summary>
    public sealed class ModuleCleanup : IModuleAssemblyCleanup {
        /// <inheritdoc />
        public void OnRemove(PSModuleInfo psModuleInfo) {
            FormattingSessionState.Current.Reset();
        }
    }
}
#endif
