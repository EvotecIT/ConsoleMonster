using System;
using System.IO;
using System.Runtime.CompilerServices;
using Xunit;

namespace SiteFrames.Tests {
    /// <summary>Runs native-editor integration cases only with Windows and the module's built dual-runtime outputs.</summary>
    public sealed class NativeEditorTheoryAttribute : TheoryAttribute {
        /// <summary>Records the missing platform or build prerequisite as an explicit skipped integration case.</summary>
        public NativeEditorTheoryAttribute() {
            if (!OperatingSystem.IsWindows()) { Skip = "The native editor regression requires Windows ConPTY."; return; }
            string root = Repository();
            foreach (string target in new[] { "net472", "net8.0", "net10.0" }) {
                if (!File.Exists(Path.Combine(root, "ConsoleMonster.PowerShell", "bin", "Debug", target, "ConsoleMonster.PowerShell.dll"))) {
                    Skip = "Build ConsoleMonster.PowerShell in Debug for both PowerShell hosts before native-editor integration tests.";
                    return;
                }
            }
        }

        private static string Repository([CallerFilePath] string source = "") => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(source)!, "..", "..", ".."));
    }
}
