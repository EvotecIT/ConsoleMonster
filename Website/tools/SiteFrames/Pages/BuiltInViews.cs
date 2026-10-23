using System;
using System.IO;
using System.Text;
using System.Text.Json.Nodes;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Export;
using SiteFrames.Capture;

namespace SiteFrames.Pages {
    /// <summary>Real host captures and review exports for the everyday built-in PowerShell views.</summary>
    internal static class BuiltInViews {
        internal static int Verify(Site site, string output) {
            Directory.CreateDirectory(output);
            foreach (PowerShellHost host in PowerShellHost.All) {
                string result = Path.Combine(output, host.Id + ".json");
                string script = Path.Combine(site.Repository, "Module", "Tests", "Support", "Assert-BuiltInViewsHost.ps1");
                byte[] bytes = PowerShellRunner.Bytes(PowerShellRunner.Run(site, host, script, 120, 80,
                    PowerShellRunner.Environment("dark"), arguments: "-ResultPath '" + result.Replace("'", "''") + "'",
                    setup: "trap { $_ | Out-String | Set-Content -LiteralPath '" + (result + ".failure.txt").Replace("'", "''") + "'; exit 1 }"));
                File.WriteAllBytes(Path.Combine(output, host.Id + ".ansi"), bytes);
                File.WriteAllText(Path.Combine(output, host.Id + ".txt"), ConsoleExporter.ToText(Parity.Transcript(bytes, 120, 80)));
                if (!File.Exists(result)) { throw new InvalidOperationException("Host contracts failed on " + host.Id + "; inspect " + output); }
                Console.WriteLine("host contracts passed " + host.Id);
            }
            return 0;
        }

        internal static int Review(Site site, string output) {
            Directory.CreateDirectory(output);
            JsonArray manifest = new JsonArray();
            foreach (PowerShellHost host in PowerShellHost.All) {
                foreach (int width in new[] { 120, 60 }) {
                    foreach (string theme in new[] { "dark", "light" }) {
                        foreach (string surface in new[] { "Member", "Command", "Module", "History" }) {
                          foreach (bool native in new[] { true, false }) {
                            string id = surface.ToLowerInvariant() + "-" + host.Id + "-" + width + "-" + theme + (native ? "-before" : "-after");
                            string script = Path.Combine(site.Website, "examples", "docs", "built-in-views.ps1");
                            var environment = PowerShellRunner.Environment(theme);
                            environment["CONSOLEMONSTER_HYPERLINKS"] = "1";
                            byte[] bytes = PowerShellRunner.Bytes(PowerShellRunner.Run(site, host, script, width, 60,
                                environment, arguments: "-Surface " + surface + (native ? " -Native" : string.Empty)));
                            File.WriteAllBytes(Path.Combine(output, id + ".ansi"), bytes);
                            var frame = Site.Crop(Parity.Transcript(bytes, width, 60));
                            var options = new ExportOptions { Width = width, Theme = theme == "light" ? ExportTheme.Light : ExportTheme.Dark,
                                ColorDepth = ColorDepth.TrueColor, Title = "Get-" + surface + " — " + host.Label, WindowChrome = true };
                            File.WriteAllText(Path.Combine(output, id + ".html"), ConsoleExporter.ToHtml(frame, options), new UTF8Encoding(false));
                            File.WriteAllText(Path.Combine(output, id + ".svg"), ConsoleExporter.ToSvg(frame, options), new UTF8Encoding(false));
                            File.WriteAllText(Path.Combine(output, id + ".txt"), ConsoleExporter.ToText(frame), new UTF8Encoding(false));
                            manifest.Add(new JsonObject { ["id"] = id, ["width"] = width, ["height"] = frame.Height,
                                ["source"] = "examples/docs/built-in-views.ps1", ["host"] = PowerShellRunner.Version(host) });
                            Console.WriteLine("captured " + id);
                          }
                        }
                    }
                }
            }
            File.WriteAllText(Path.Combine(output, "manifest.json"), manifest.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            return 0;
        }
    }
}
