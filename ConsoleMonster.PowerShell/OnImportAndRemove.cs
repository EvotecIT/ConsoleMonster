using System;
using System.IO;
using System.Management.Automation;
using System.Reflection;
using ConsoleMonster.PowerShell;

/// <summary>
/// Resolves ConsoleMonster.dll from the module's own library folder on Windows PowerShell 5.1 (.NET Framework), where
/// binary module dependencies are not probed next to the module assembly, and points calls to retired commands at their
/// replacements in the importing runspace.
/// </summary>
public class OnModuleImportAndRemove : IModuleAssemblyInitializer, IModuleAssemblyCleanup {
    /// <inheritdoc />
    public void OnImport() {
        if (IsNetFramework()) {
            AppDomain.CurrentDomain.AssemblyResolve += Resolve;
        }
        RetiredCommands.Register();
    }

    /// <inheritdoc />
    public void OnRemove(PSModuleInfo module) {
        RetiredCommands.Unregister();
        if (IsNetFramework()) {
            AppDomain.CurrentDomain.AssemblyResolve -= Resolve;
        }
    }

    private static Assembly? Resolve(object? sender, ResolveEventArgs args) {
        string? directory = Path.GetDirectoryName(typeof(OnModuleImportAndRemove).Assembly.Location);
        if (string.IsNullOrEmpty(directory)) {
            return null;
        }
        string name = new AssemblyName(args.Name).Name + ".dll";
        string candidate = Path.Combine(directory, name);
        return File.Exists(candidate) ? Assembly.LoadFrom(candidate) : null;
    }

    private static bool IsNetFramework() {
        return System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription.StartsWith(".NET Framework", StringComparison.OrdinalIgnoreCase);
    }
}
