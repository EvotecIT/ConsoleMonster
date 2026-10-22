using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json.Nodes;
using ConsoleMonster.Canvas;
using ConsoleMonster.Export;
using SiteFrames.Vt;

namespace SiteFrames.Capture {
    /// <summary>Captures the native editor and progress renderer, then exports their real cells through the site generator.</summary>
    internal static class NativeStylingReview {
        internal static int Run(Site site, string output) {
            Directory.CreateDirectory(output);
            JsonArray evidence = new JsonArray();
            foreach (PowerShellHost host in PowerShellHost.All) {
                foreach (string theme in new[] { "Dark", "Light", "HighContrast" }) {
                    foreach (int width in new[] { 120, 60 }) {
                        foreach (string phase in new[] { "syntax", "selection", "prediction", "progress" }) {
                            // The installed PSReadLine 2.0 on Desktop has no prediction feature.
                            if (host == PowerShellHost.Desktop && phase == "prediction") { continue; }
                            string id = host.Id + "-" + theme + "-" + width + "-" + phase;
                            string prefix = Path.Combine(output, id);
                            string script = Path.Combine(site.Tool, "Capture", "NativeStyling.ps1");
                            string command = host.Executable + " -NoLogo -NoProfile -File \"" + script + "\" -ModulePath \""
                                + Path.Combine(site.Repository, "Module", "ConsoleMonster.psd1") + "\" -EvidencePath \"" + prefix + "\" -Phase " + phase;
                            string? keys = phase == "progress" ? null : phase == "prediction" ? "\u001bOQGet-Pro" : "Get-Process -Name 'pwsh' | Select-Object -First 3";
                            if (phase == "selection") { keys += "\u001b[1;2H"; }
                            Dictionary<string, string?> environment = PowerShellRunner.Environment(theme.ToLowerInvariant());
                            environment["CONSOLEMONSTER_BUILD_CONFIGURATION"] = "Debug";
                            List<OutputChunk> chunks = PseudoConsole.Run(command, width, 20, environment, site.Repository,
                                TimeSpan.FromSeconds(30), inputText: keys, readyMarker: "CM_READY", capturePath: prefix + ".ansi", completionText: keys == null ? null : "\u001b[24~");
                            using MemoryStream bytes = new MemoryStream();
                            foreach (OutputChunk chunk in chunks) { bytes.Write(chunk.Data); }
                            byte[] raw = bytes.ToArray();
                            File.WriteAllBytes(prefix + ".ansi", raw);
                            string text = Encoding.UTF8.GetString(raw);
                            int marker = text.IndexOf("\u001b]0;CM_FRAME\a", StringComparison.Ordinal);
                            if (marker < 0) { throw new InvalidOperationException("Missing native frame marker: " + id); }
                            VtScreen terminal = new VtScreen(width, 20);
                            terminal.Feed(text.Substring(0, marker));
                            CellBuffer frame = terminal.Snapshot();
                            ExportOptions options = new ExportOptions {
                                Theme = theme == "Light" ? ExportTheme.Light : ExportTheme.Dark,
                                WindowChrome = true, Title = host.Label + " · " + phase
                            };
                            File.WriteAllText(prefix + ".svg", ConsoleExporter.ToSvg(frame, options));
                            File.WriteAllText(prefix + ".html", ConsoleExporter.ToHtml(frame, options));
                            File.WriteAllText(prefix + ".txt", ConsoleExporter.ToText(frame));
                            evidence.Add(new JsonObject { ["id"] = id, ["runtime"] = host.Label, ["theme"] = theme,
                                ["width"] = width, ["phase"] = phase, ["bytes"] = raw.Length });
                            Console.WriteLine("Native styling: " + id);
                        }
                    }
                }
            }
            File.WriteAllText(Path.Combine(output, "evidence.json"), evidence.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            return 0;
        }
    }
}
