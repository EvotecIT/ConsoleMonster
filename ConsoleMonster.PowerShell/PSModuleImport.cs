using System.IO;
using System.Management.Automation;

namespace ConsoleMonster.PowerShell {
    /// <summary>The importable entrypoint for a module loaded from either its manifest or its binary.</summary>
    internal static class PSModuleImport {
        internal static string PathFor(PSCmdlet cmdlet) {
            string? moduleBase = cmdlet.MyInvocation.MyCommand.Module?.ModuleBase;
            string? manifest = moduleBase == null ? null : Path.Combine(moduleBase, "ConsoleMonster.psd1");
            return manifest != null && File.Exists(manifest) ? manifest : typeof(PSModuleImport).Assembly.Location;
        }
    }
}
