using VtScreen = ConsoleMonster.Sessions.VtScreen;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using ConsoleMonster.Canvas;
using ConsoleMonster.Export;
using SiteFrames.Vt;

namespace SiteFrames.Capture {
    /// <summary>Captures the native editor and progress renderer, then exports their real cells through the site generator.</summary>
    internal static class NativeStylingReview {
        internal static int Run(Site site, string output, bool useAll = false) {
            Directory.CreateDirectory(output);
            JsonArray evidence = new JsonArray();
            foreach (PowerShellHost host in PowerShellHost.Available) {
                string script = Path.Combine(site.Tool, "Capture", "NativeStyling.ps1");
                string[] commandPrefix = { "-NoLogo", "-NoProfile", "-File", script, "-ModulePath", Path.Combine(site.Repository, "Module", "ConsoleMonster.psd1") };
                string capabilitiesPath = Path.Combine(output, host.Id + "-capabilities");
                TerminalCapture.Run(host.Executable, commandPrefix.Concat(new[] { "-Theme", "Dark", "-CapabilitiesOnly", "-EvidencePath", capabilitiesPath }), 120, 20,
                    CaptureEnvironment("Dark"), site.Repository, TimeSpan.FromSeconds(30), capturePath: capabilitiesPath + ".ansi");
                JsonNode capabilities = JsonNode.Parse(File.ReadAllText(capabilitiesPath + ".json"))!;
                foreach (string theme in new[] { "Dark", "Light", "HighContrast" }) {
                    foreach (int width in new[] { 120, 60 }) {
                        foreach (string phase in new[] { "syntax", "selection", "prediction", "progress" }) {
                            string id = host.Id + "-" + theme + "-" + width + "-" + phase;
                            if (phase == "prediction" && !capabilities["PredictionAvailable"]!.GetValue<bool>()) {
                                evidence.Add(new JsonObject { ["id"] = id, ["skipped"] = true,
                                    ["reason"] = "Missing native properties: " + capabilities["MissingPredictionProperties"]!.ToJsonString(),
                                    ["readLine"] = capabilities["ReadLine"]!.GetValue<string>() });
                                continue;
                            }
                            string prefix = Path.Combine(output, id);
                            var command = commandPrefix.Concat(new[] { "-Theme", theme, "-EvidencePath", prefix, "-Phase", phase }).ToList();
                            if (useAll) { command.Add("-UseAll"); }
                            string? keys = phase == "progress" ? null : phase == "prediction" ? "\u001bOQGet-Pro" : "Get-Process -Name 'pwsh' | Select-Object -First 3";
                            if (phase == "selection") { keys += "\u001b[1;2H"; }
                            Dictionary<string, string?> environment = CaptureEnvironment(theme);
                            List<OutputChunk> chunks = TerminalCapture.Run(host.Executable, command, width, 20, environment, site.Repository,
                                TimeSpan.FromSeconds(30), inputText: keys, readyMarker: "CM_READY", capturePath: prefix + ".ansi", completionText: keys == null ? null : "\u001b[24~");
                            using MemoryStream bytes = new MemoryStream();
                            foreach (OutputChunk chunk in chunks) { bytes.Write(chunk.Data); }
                            byte[] raw = bytes.ToArray();
                            File.WriteAllBytes(prefix + ".ansi", raw);
                            JsonNode receipt = JsonNode.Parse(File.ReadAllText(prefix + ".json"))!;
                            if (receipt["ResolvedTheme"]!.GetValue<string>() != theme) { throw new InvalidOperationException("Wrong resolved native theme: " + id); }
                            string text = Encoding.UTF8.GetString(raw);
                            int marker = text.IndexOf("\u001b]0;CM_FRAME\a", StringComparison.Ordinal);
                            if (marker < 0) { throw new InvalidOperationException("Missing native frame marker: " + id); }
                            CellBuffer frame = FrameFromVt(text.Substring(0, marker), width, 20);
                            ExportOptions options = new ExportOptions {
                                Theme = theme == "Light" ? ExportTheme.Light : ExportTheme.Dark,
                                WindowChrome = true, Title = host.Label + " · " + phase
                            };
                            File.WriteAllText(prefix + ".svg", ConsoleExporter.ToSvg(frame, options));
                            File.WriteAllText(prefix + ".html", ConsoleExporter.ToHtml(frame, options));
                            File.WriteAllText(prefix + ".txt", ConsoleExporter.ToText(frame));
                            evidence.Add(new JsonObject { ["id"] = id, ["runtime"] = host.Label, ["theme"] = theme,
                                ["width"] = width, ["phase"] = phase, ["bytes"] = raw.Length, ["all"] = useAll,
                                ["capturedRows"] = 20, ["frameRows"] = frame.Height,
                                ["resolvedTheme"] = receipt["ResolvedTheme"]!.GetValue<string>(),
                                ["readLine"] = receipt["ReadLine"]!.GetValue<string>(), ["readLinePath"] = receipt["ReadLinePath"]!.GetValue<string>() });
                            Console.WriteLine("Native styling: " + id);
                        }
                    }
                }
            }
            File.WriteAllText(Path.Combine(output, "evidence.json"), evidence.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            return 0;
        }

        private static Dictionary<string, string?> CaptureEnvironment(string theme) {
            Dictionary<string, string?> environment = PowerShellRunner.Environment(theme == "Light" ? "light" : "dark");
            environment["CONSOLEMONSTER_DEVELOPMENT_CONFIGURATION"] = "Debug";
            return environment;
        }

        /// <summary>Replays native cells, retaining their content and exactly one trailing blank row.</summary>
        internal static CellBuffer FrameFromVt(string text, int width, int height) {
            VtScreen terminal = new VtScreen(width, height);
            terminal.Feed(text);
            CellBuffer captured = terminal.Snapshot();
            int last = -1;
            for (int y = 0; y < captured.Height; y++) {
                if (!string.IsNullOrWhiteSpace(captured.GetRowText(y))) { last = y; }
            }
            CellBuffer frame = new CellBuffer(width, last + 2);
            frame.Blit(captured, 0, 0, width, Math.Min(captured.Height, frame.Height), 0, 0);
            if (frame.Height > captured.Height) {
                ConsoleMonster.Styling.Style blank = new ConsoleMonster.Styling.Style(default, captured[0, 0].Style.BackgroundColor);
                for (int x = 0; x < width; x++) { frame.Set(x, frame.Height - 1, " ", blank); }
            }
            return frame;
        }
    }
}
