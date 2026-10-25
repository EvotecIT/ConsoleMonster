using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using SiteFrames.Capture;
using SiteFrames.Vt;
using Xunit;

namespace SiteFrames.Tests {
    /// <summary>Exercises actual native-editor bytes, rather than only saved PSReadLine preference strings.</summary>
    public sealed class NativeEditorSuppressionContracts {
        [NativeEditorTheory]
        [InlineData(false)]
        [InlineData(true)]
        public void NoColour_StaysFreeOfTrueColourSequencesWhenTyping(bool enabled) {
            string root = Repository();
            foreach (PowerShellHost host in PowerShellHost.All) {
                var environment = PowerShellRunner.Environment("Dark", color: "none", mode: "interactive");
                environment["CONSOLEMONSTER_COLOR"] = null;
                environment["NO_COLOR"] = "1";
                environment["CONSOLEMONSTER_DEVELOPMENT_CONFIGURATION"] = NativeEditorTheoryAttribute.Configuration;
                string script = Path.Combine(root, "Website", "tools", "SiteFrames", "Capture", "NativeColourSuppression.ps1");
                string module = Path.Combine(root, "Module", "ConsoleMonster.psd1");
                var chunks = PseudoConsole.Run(host.Executable + " -NoLogo -NoProfile -File \"" + script + "\" -ModulePath \"" + module + "\"" + (enabled ? " -Enabled" : ""),
                    120, 12, environment, root, TimeSpan.FromSeconds(45), inputText: "Get-Process", readyMarker: "CM_READY", completionText: "\r");
                StringBuilder output = new StringBuilder();
                foreach (OutputChunk chunk in chunks) { output.Append(Encoding.UTF8.GetString(chunk.Data)); }
                Assert.Contains("NATIVE_EDITOR_OK", output.ToString());
                Assert.DoesNotMatch(new Regex("\\x1b\\[[0-9;]*(?:38|48);2;"), output.ToString());
            }
        }

        private static string Repository([CallerFilePath] string source = "") => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(source)!, "..", "..", ".."));
    }
}
