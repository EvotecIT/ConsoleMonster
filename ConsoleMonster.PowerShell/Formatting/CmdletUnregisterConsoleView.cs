using System;
using System.Management.Automation;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Removes registered ConsoleMonster views and value shapers.</para>
    /// <para type="description">Names the registered types directly or reads their names from a validated data-only .consoleviews.psd1 file. Does not remove or execute the file.</para>
    /// </summary>
    /// <example><para>Remove a custom result view.</para><code>Unregister-ConsoleView -TypeName 'Contoso.Server'</code></example>
    [Cmdlet(VerbsLifecycle.Unregister, "ConsoleView", DefaultParameterSetName = "Type", SupportsShouldProcess = true)]
    public sealed class CmdletUnregisterConsoleView : NoPipelineInputCmdlet {
        /// <summary><para type="description">Type names whose registrations should be removed.</para></summary>
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "Type")]
        public string[] TypeName { get; set; } = Array.Empty<string>();
        /// <summary><para type="description">Data-only view files listing the type registrations to remove.</para></summary>
        [Parameter(Mandatory = true, ParameterSetName = "File")]
        public string[] Path { get; set; } = Array.Empty<string>();
        /// <inheritdoc />
        protected override void EndProcessing() {
            foreach (string path in Path) {
                foreach (ConsoleViewRegistration view in ConsoleHostViews.Read(this, SessionState.Path.GetUnresolvedProviderPathFromPSPath(path))) { Remove(view.TypeName); }
            }
            foreach (string type in TypeName) { Remove(type); }
        }
        private void Remove(string type) {
            if (!ShouldProcess(type, "Unregister ConsoleMonster view and shaper")) { return; }
            ConsoleHostViews.Unregister(type);
        }
    }
}
