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

        private static string CapturePath(Site site, string area, string id, PowerShellHost host, string? variant = null) =>
            Path.Combine(site.Captures, area, id + (variant == null ? string.Empty : "-" + variant) + "." + host.Id + ".ansi");

        private static string Stamp(Site site, string area, string id, int width, PowerShellHost host, Dictionary<string, string?>? environment = null) =>
            Stamps.Of(Script(site, area, id), environment ?? PowerShellRunner.Environment("dark"), host.Id, width.ToString(System.Globalization.CultureInfo.InvariantCulture));

        /// <summary>Captures both hosts, optionally naming a profile variant and pinning its environment.</summary>
        public static void Capture(Site site, string area, string id, int width, string? variant = null, Dictionary<string, string?>? environment = null) {
            foreach (PowerShellHost host in PowerShellHost.Available) {
                List<Vt.OutputChunk> chunks = PowerShellRunner.Run(site, host, Script(site, area, id), width, 80, environment ?? PowerShellRunner.Environment("dark"));
                string path = CapturePath(site, area, id, host, variant);
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllBytes(path, PowerShellRunner.Bytes(chunks));
                Stamps.Write(site, path, Stamp(site, area, id, width, host, environment));
                CaptureRun.Record(path, width, 80);
                Console.WriteLine("captured " + Path.GetRelativePath(site.Website, path));
            }
        }

        /// <summary>The parity record for page data, reporting any difference as a problem.</summary>
        public static JsonObject Check(Site site, string area, string id, int width, Func<IWidget> build, JsonObject hosts, List<string> problems) =>
            Check(site, area, id, width, Site.Render(build(), width, ColorDepth.TrueColor, GlyphTier.Full, Theme.Dark), hosts, problems);

        /// <summary>The parity record against the C# frame, using the default dark capture or a pinned profile variant.</summary>
        public static JsonObject Check(Site site, string area, string id, int width, CellBuffer expected, JsonObject hosts, List<string> problems, string? variant = null, Dictionary<string, string?>? environment = null) {
            JsonObject parity = new JsonObject();
            foreach (PowerShellHost host in PowerShellHost.All) {
                string path = CapturePath(site, area, id, host, variant);
                if (!File.Exists(path)) {
                    problems.Add(id + " has no " + host.Label + " capture; run the capture command.");
                    continue;
                }
                string? stale = Stamps.Check(site, path, Stamp(site, area, id, width, host, environment), area);
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
