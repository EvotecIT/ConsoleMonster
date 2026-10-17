using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using ConsoleMonster.Canvas;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;
using SiteFrames.Capture;
using SiteFrames.Vt;

namespace SiteFrames.Pages {
    /// <summary>
    /// Benchmarks: the repository's ConsoleMonster.Benchmarks program, run in a pseudo console by the capture command, with
    /// the machine it ran on recorded beside its output. Nothing here is estimated; every number is parsed from that output.
    /// </summary>
    internal static class BenchmarksPage {
        private static string CastPath(Site site) => Path.Combine(site.Captures, "benchmarks", "benchmarks.cast");

        private static string MachinePath(Site site) => Path.Combine(site.Captures, "benchmarks", "machine.json");

        public static void Capture(Site site) {
            string project = Path.Combine(site.Repository, "ConsoleMonster.Benchmarks", "ConsoleMonster.Benchmarks.csproj");
            using (Process build = Process.Start(new ProcessStartInfo("dotnet", "build \"" + project + "\" -c Release -nologo -v q") { UseShellExecute = false })!) {
                build.WaitForExit();
                if (build.ExitCode != 0) {
                    throw new InvalidOperationException("Building ConsoleMonster.Benchmarks failed.");
                }
            }
            string exe = Path.Combine(site.Repository, "ConsoleMonster.Benchmarks", "bin", "Release", "net8.0", "ConsoleMonster.Benchmarks.exe");
            List<OutputChunk> chunks = PseudoConsole.Run("\"" + exe + "\" 600", 110, 20, PowerShellRunner.Environment("dark"), site.Repository, TimeSpan.FromMinutes(5));
            Directory.CreateDirectory(Path.GetDirectoryName(CastPath(site))!);
            File.WriteAllText(CastPath(site), Replay.Replays.ToCast(chunks, 110, 20, "ConsoleMonster.Benchmarks 600"), new UTF8Encoding(false));
            string cpu = string.Empty;
            try {
                using (Process wmic = Process.Start(new ProcessStartInfo("powershell.exe", "-NoProfile -Command \"(Get-CimInstance Win32_Processor | Select-Object -First 1).Name\"") { RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true })!) {
                    cpu = wmic.StandardOutput.ReadToEnd().Trim();
                    wmic.WaitForExit();
                }
            } catch (Exception) {
                cpu = "unknown processor";
            }
            JsonObject machine = new JsonObject {
                ["processor"] = cpu,
                ["logicalProcessors"] = Environment.ProcessorCount,
                ["os"] = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
                ["recorded"] = DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            };
            site.WriteJson(MachinePath(site), machine);
            Console.WriteLine("captured benchmarks");
        }

        public static void Render(Site site, JsonObject live) {
            if (!File.Exists(CastPath(site))) {
                WidgetsPage.Report(new List<string> { "missing benchmark capture; run the capture command." }, "benchmarks");
                return;
            }
            Replay.Replays.FromCast(File.ReadAllText(CastPath(site)), 4, false, out CellBuffer output, out int width, out _);
            string text = Parity.Describe(output);
            double Number(string pattern) {
                Match m = Regex.Match(text, pattern);
                return m.Success ? double.Parse(m.Groups[1].Value.Replace(",", string.Empty), CultureInfo.InvariantCulture) : double.NaN;
            }
            double apply = Number(@"event apply:\s+([\d,]+) events/s");
            double compose = Number(@"compose per frame:\s+([\d.]+) ms");
            double encode = Number(@"diff\+VT per frame:\s+([\d.]+) ms");
            double chars = Number(@"chars per frame:\s+([\d,]+)");
            double alloc = Number(@"alloc per frame:\s+([\d,]+) bytes");
            double cpu = Number(@"render CPU at 15 fps:\s+([\d.]+)%");
            string runtime = Regex.Match(text, @"ConsoleMonster benchmark \(([^,]+),").Groups[1].Value;

            // The numbers as a ConsoleMonster table, so the page shows them the way a console would.
            Table table = new Table { Title = "[bold]ConsoleMonster.Benchmarks[/] [muted]140×45, 32 lanes, 5,000 updates between frames[/]" };
            table.AddColumn(new TableColumn("Measure") { IsKey = true });
            table.AddColumn(new TableColumn("Result") { Alignment = ConsoleMonster.Layout.HorizontalAlignment.Right });
            table.AddColumn(new TableColumn("Share of a 66.7 ms frame") { Presenter = new ConsoleMonster.Presenters.DataBarPresenter(66.7, new ConsoleMonster.Presenters.NumberPresenter("ms")), Priority = 5 });
            table.AddValues("Compose a frame", compose.ToString("0.000", Site.Culture) + " ms", compose);
            table.AddValues("Diff and encode VT", encode.ToString("0.000", Site.Culture) + " ms", encode);
            table.AddValues("Characters written per frame", chars.ToString("N0", Site.Culture), null);
            table.AddValues("Allocated per frame", alloc.ToString("N0", Site.Culture) + " B", null);
            table.AddValues("Events applied per second", apply.ToString("N0", Site.Culture), null);

            JsonObject machine = (JsonObject)JsonNode.Parse(File.ReadAllText(MachinePath(site)))!;
            site.WriteJson(site.Data("benchmarks"), new JsonObject {
                ["runtime"] = runtime,
                ["machine"] = machine.DeepClone(),
                ["output"] = new JsonObject { ["title"] = "ConsoleMonster.Benchmarks 600", ["frame"] = site.Frame(Site.Crop(Parity.Trim(output)), null), ["w"] = width },
                ["table"] = site.Frame(() => table, 96),
                ["facts"] = new JsonArray(
                    new JsonObject { ["label"] = "Compose plus diff and VT, per frame", ["value"] = (compose + encode).ToString("0.000", Site.Culture) + " ms" },
                    new JsonObject { ["label"] = "Render CPU at 15 fps", ["value"] = cpu.ToString("0.0", Site.Culture) + "% of a core" },
                    new JsonObject { ["label"] = "Events applied per second", ["value"] = (apply / 1_000_000).ToString("0.0", Site.Culture) + " million" },
                    new JsonObject { ["label"] = "Characters per diffed frame", ["value"] = chars.ToString("N0", Site.Culture) }),
                ["stress"] = live["stress"]?["result"]?.DeepClone()
            });
        }
    }
}
