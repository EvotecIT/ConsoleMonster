using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using ConsoleMonster.Sessions;
using SiteFrames.Capture;
using SiteFrames.Vt;
using Xunit;

namespace SiteFrames.Tests {
    /// <summary>Exercises explicit document width through the actual PowerShell host and ConPTY input loop.</summary>
    public sealed class MarkdownPagerContracts {
        /// <summary>A requested document width survives a wider physical terminal on both supported PowerShell hosts.</summary>
        [NativeEditorTheory]
        [InlineData(40)]
        public void ExplicitWidth_RemainsNarrowOnARealTerminal(int width) {
            string root = Repository();
            foreach (PowerShellHost host in PowerShellHost.Available) {
                var environment = PowerShellRunner.Environment("dark", mode: "interactive");
                environment["CONSOLEMONSTER_DEVELOPMENT_CONFIGURATION"] = "Release";
                string script = Path.Combine(root, "Website", "tools", "SiteFrames", "Capture", "MarkdownWidth.ps1");
                string module = Path.Combine(root, "Module", "ConsoleMonster.psd1");
                var chunks = TerminalCapture.Run(host.Executable, new[] { "-NoLogo", "-NoProfile", "-File", script, "-ModulePath", module, "-Width", width.ToString(System.Globalization.CultureInfo.InvariantCulture) },
                    120, 12, environment, root, TimeSpan.FromSeconds(45), inputText: "q", readyMarker: "alpha");
                using MemoryStream bytes = new MemoryStream();
                foreach (OutputChunk chunk in chunks) { bytes.Write(chunk.Data, 0, chunk.Data.Length); }
                string vt = Encoding.UTF8.GetString(bytes.ToArray());
                Assert.Contains("MARKDOWN_PAGER_DONE", vt);
                VtScreen screen = new VtScreen(120, 12);
                bool first = true;
                bool sawDocument = false;
                foreach (string part in vt.Split('\u001b')) {
                    screen.Feed((first ? string.Empty : "\u001b") + part);
                    first = false;
                    string line = screen.Snapshot().GetRowText(1).TrimEnd();
                    if (!line.StartsWith("alpha", StringComparison.Ordinal)) { continue; }
                    sawDocument = true;
                    Assert.True(line.Length <= width, host.Label + " used the physical width for document content: " + line);
                }
                Assert.True(sawDocument, host.Label + " did not render the interactive document.");
            }
        }

        private static string Repository([CallerFilePath] string source = "") =>
            Path.GetFullPath(Path.Combine(Path.GetDirectoryName(source)!, "..", "..", ".."));
    }
}
