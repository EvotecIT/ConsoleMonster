using System;
using System.IO;
using System.Runtime.CompilerServices;
using Xunit;

namespace SiteFrames.Tests {
    /// <summary>Runs native-editor integration cases only with Windows and the module's built dual-runtime outputs.</summary>
    public sealed class NativeEditorTheoryAttribute : TheoryAttribute {
        /// <summary>Uses the same development build as the caller and the module loader.</summary>
        public static string Configuration => Environment.GetEnvironmentVariable("CONSOLEMONSTER_DEVELOPMENT_CONFIGURATION") ?? "Debug";

        /// <summary>Records the missing platform or build prerequisite as an explicit skipped integration case.</summary>
        public NativeEditorTheoryAttribute() {
            if (!OperatingSystem.IsWindows()) { Skip = "The native editor regression requires Windows ConPTY."; return; }
            string root = Repository();
            foreach (string target in new[] { "net472", "net8.0" }) {
                if (!File.Exists(Path.Combine(root, "ConsoleMonster.PowerShell", "bin", Configuration, target, "ConsoleMonster.PowerShell.dll"))) {
                    Skip = "Build ConsoleMonster.PowerShell in " + Configuration + " for net472 and net8.0 before native-editor integration tests.";
                    return;
                }
            }
        }

        private static string Repository([CallerFilePath] string source = "") => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(source)!, "..", "..", ".."));
    }
}
