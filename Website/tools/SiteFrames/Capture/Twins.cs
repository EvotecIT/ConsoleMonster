using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Nodes;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;

namespace SiteFrames.Capture {
    /// <summary>
    /// A C# example and its PowerShell twin (<c>Website/examples/&lt;area&gt;/&lt;id&gt;.ps1</c>). The capture runs the script on
    /// both hosts; the render compares what each host drew with the C# frame, cell by cell.
    /// </summary>
    internal static class Twins {
        public static string Script(Site site, string area, string id) => Path.Combine(site.Website, "examples", area, id + ".ps1");

        private static string CapturePath(Site site, string area, string id, PowerShellHost host) => Path.Combine(site.Captures, area, id + "." + host.Id + ".ansi");

        private static string Stamp(Site site, string area, string id, int width, PowerShellHost host) =>
            Stamps.Of(Script(site, area, id), PowerShellRunner.Environment("dark"), host.Id, width.ToString(System.Globalization.CultureInfo.InvariantCulture));

        public static void Capture(Site site, string area, string id, int width) {
            foreach (PowerShellHost host in PowerShellHost.All) {
                List<Vt.OutputChunk> chunks = PowerShellRunner.Run(site, host, Script(site, area, id), width, 80, PowerShellRunner.Environment("dark"));
                string path = CapturePath(site, area, id, host);
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllBytes(path, PowerShellRunner.Bytes(chunks));
                Stamps.Write(site, path, Stamp(site, area, id, width, host));
                Console.WriteLine("captured " + Path.GetRelativePath(site.Website, path));
            }
        }

        /// <summary>The parity record for page data, reporting any difference as a problem.</summary>
        public static JsonObject Check(Site site, string area, string id, int width, Func<IWidget> build, JsonObject hosts, List<string> problems) =>
            Check(site, area, id, width, Site.Render(build(), width, ColorDepth.TrueColor, GlyphTier.Full, Theme.Dark), hosts, problems);

        /// <summary>The parity record against a frame the C# twin printed (24-bit colour, full glyphs, dark theme).</summary>
        public static JsonObject Check(Site site, string area, string id, int width, CellBuffer expected, JsonObject hosts, List<string> problems) {
            JsonObject parity = new JsonObject();
            foreach (PowerShellHost host in PowerShellHost.All) {
                string path = CapturePath(site, area, id, host);
                if (!File.Exists(path)) {
                    problems.Add(id + " has no " + host.Label + " capture; run the capture command.");
                    continue;
                }
                string? stale = Stamps.Check(site, path, Stamp(site, area, id, width, host), area);
                if (stale != null) {
                    problems.Add(stale);
                }
                CellBuffer actual = Parity.Transcript(File.ReadAllBytes(path), width, 80);
                ParityResult result = Parity.Compare(expected, actual);
                if (!result.Identical) {
                    problems.Add(id + " on " + host.Label + ": " + result.Detail + "\n" + Parity.Describe(actual));
                }
                parity[host.Id] = new JsonObject {
                    ["host"] = (host == PowerShellHost.Core ? "PowerShell " : "Windows PowerShell ") + hosts[host.Id]?.GetValue<string>(),
                    ["version"] = hosts[host.Id]?.GetValue<string>(),
                    ["identical"] = result.Identical
                };
            }
            return parity;
        }

        /// <summary>What a host drew for a script, as a frame (for pages that show PowerShell output itself).</summary>
        public static CellBuffer? Output(Site site, string area, string id, int width, PowerShellHost host) {
            string path = CapturePath(site, area, id, host);
            return File.Exists(path) ? Parity.Transcript(File.ReadAllBytes(path), width, 80) : null;
        }
    }
}
