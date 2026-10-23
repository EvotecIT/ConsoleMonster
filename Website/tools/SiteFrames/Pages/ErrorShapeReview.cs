using System;
using System.IO;
using System.Text;
using ConsoleMonster.Export;
using SiteFrames.Capture;

namespace SiteFrames.Pages {
    /// <summary>Captures synthetic compiled PowerShell errors through the site's native host runner for review.</summary>
    internal static class ErrorShapeReview {
        internal static void Run(Site site, string directory) {
            string output = Path.GetFullPath(directory);
            Directory.CreateDirectory(output);
            string fixtures = Path.Combine(output, "fixtures");
            Directory.CreateDirectory(fixtures);
            string script = Path.Combine(site.Repository, "Module", "Tests", "Support", "Capture-ErrorShape.ps1");
            StringBuilder index = new StringBuilder("# Synthetic PowerShell error shapes\n\n");
            index.Append("Native captures use ConciseView on PowerShell 7 and NormalView on Windows PowerShell 5.1, which has no ConciseView. Both columns are real console output, including styles and ordering. No external incident data is used.\n\n");
            index.Append("| Shape | Host | Width | Theme | Native | Compact |\n| --- | --- | --- | --- | --- | --- |\n");
            foreach (string shape in new[] { "splat-command", "duplicate-inner", "distinct-cause", "embedded-stack", "detached-record", "mixed-output" }) {
                foreach (PowerShellHost host in PowerShellHost.All) {
                    foreach (int width in new[] { 120, 60 }) {
                        foreach (string theme in new[] { "light", "dark" }) {
                            string stem = shape + "-" + host.Id + "-" + width + "-" + theme;
                            foreach (string style in new[] { "native", "compact" }) {
                                string name = stem + "-" + style;
                                var environment = PowerShellRunner.Environment(theme);
                                environment["CONSOLEMONSTER_DEVELOPMENT_CONFIGURATION"] = "Debug";
                                string arguments = "-Shape '" + shape + "' -Style '" + style + "' -Directory '" + fixtures.Replace("'", "''") + "'";
                                var chunks = PowerShellRunner.Run(site, host, script, width, 160, environment, arguments: arguments);
                                byte[] bytes = PowerShellRunner.Bytes(chunks);
                                File.WriteAllBytes(Path.Combine(output, name + ".ansi"), bytes);
                                var frame = Parity.Transcript(bytes, width, 160);
                                string text = frame.ToPlainText();
                                if (!text.Contains("synthetic", StringComparison.OrdinalIgnoreCase)
                                    && !text.Contains("Object reference", StringComparison.Ordinal)
                                    && !text.Contains("Runspace execution failed.", StringComparison.Ordinal)
                                    && !text.Contains("Get-Command", StringComparison.Ordinal)) {
                                    throw new InvalidOperationException("The native capture did not contain its synthetic error: " + name + "\n" + text);
                                }
                                ExportOptions options = new ExportOptions {
                                    Width = width, Theme = theme == "light" ? ExportTheme.Light : ExportTheme.Dark,
                                    Title = host.Label + " · " + shape + " · " + style, WindowChrome = true
                                };
                                File.WriteAllText(Path.Combine(output, name + ".txt"), text);
                                File.WriteAllText(Path.Combine(output, name + ".html"), ConsoleExporter.ToHtml(frame, options));
                                File.WriteAllText(Path.Combine(output, name + ".svg"), ConsoleExporter.ToSvg(frame, options));
                                Console.WriteLine("captured " + name);
                            }
                            index.Append("| ").Append(shape).Append(" | ").Append(host.Id).Append(" | ").Append(width).Append(" | ").Append(theme)
                                .Append(" | [native](").Append(stem).Append("-native.html) | [compact](").Append(stem).Append("-compact.html) |\n");
                        }
                    }
                }
            }
            File.WriteAllText(Path.Combine(output, "review-index.md"), index.ToString());
        }
    }
}
