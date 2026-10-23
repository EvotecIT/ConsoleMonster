using System;
using System.IO;
using System.Linq;
using System.Text;
using SiteFrames.Vt;

namespace SiteFrames.Capture {
    /// <summary>Producer-acknowledged real terminal input for both supported Windows PowerShell hosts.</summary>
    internal static class GridViewHostProof {
        internal static int Run(Site site, string output) {
            Directory.CreateDirectory(output);
            foreach (PowerShellHost host in PowerShellHost.All) {
                foreach ((string name, string keys) in new[] {
                    ("Single", "\u001b[B\u001b[21~"),
                    ("Multiple", " \u001b[B \u001b[21~"),
                    ("Wait", "\u001b"), ("Browse", "\u001b"), ("Captures", "")
                }) {
                    string script = Path.Combine(site.Repository, "Module", "Tests", "Support", "Assert-GridViewHost.ps1");
                    string command = host.Executable + " -NoLogo -NoProfile -File \"" + script + "\" -Case " + name;
                    var environment = PowerShellRunner.Environment("dark");
                    environment["CONSOLEMONSTER_DEVELOPMENT_CONFIGURATION"] = "Debug";
                    var chunks = PseudoConsole.Run(command, 80, 25, environment, site.Repository, TimeSpan.FromSeconds(45),
                        readyText: name == "Captures" ? null : "GRIDVIEW_READY", keys: name == "Captures" ? null : keys);
                    string text = Encoding.UTF8.GetString(chunks.SelectMany(chunk => chunk.Data).ToArray());
                    File.WriteAllText(Path.Combine(output, host.Id + "-" + name + ".ansi"), text);
                    if (!text.Contains("HOST_OK", StringComparison.Ordinal)) { throw new InvalidOperationException(host.Id + " " + name + ": receipt missing."); }
                    Console.WriteLine(host.Id + " " + name + " HOST_OK");
                }
            }
            return 0;
        }
    }
}
