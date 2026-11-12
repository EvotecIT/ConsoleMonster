using System;
using System.IO;

namespace ConsoleMonster.PowerShell {
    /// <summary>Loads native scripts embedded in the PowerShell assembly.</summary>
    internal static class PSScriptResources {
        internal static string Read(string resource) {
            using (Stream stream = typeof(PSScriptResources).Assembly.GetManifestResourceStream("ConsoleMonster.PowerShell." + resource)
                ?? throw new InvalidOperationException("The PowerShell script resource is missing: " + resource))
            using (StreamReader reader = new StreamReader(stream)) { return reader.ReadToEnd(); }
        }
    }
}
