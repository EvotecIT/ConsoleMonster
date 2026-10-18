using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using System.Xml;
using ConsoleMonster.Canvas;
using SiteFrames.Capture;
using SiteFrames.Vt;

namespace SiteFrames.Pages {
    /// <summary>
    /// The PowerShell page and the PowerShell figures in the guides: scripts under <c>Website/examples/powershell/</c> run on
    /// Windows PowerShell 5.1 and PowerShell 7, including the built-in Format cmdlets they replace, and the cmdlet list
    /// from the module's help.
    /// </summary>
    internal static class PowerShellPage {
        public const int Width = 100;

        /// <summary>A capture: the script, a variant name, the environment and the hosts it runs on.</summary>
        private sealed class Run {
            public Run(string id, string variant, Dictionary<string, string?> environment, PowerShellHost[] hosts, int width = Width) {
                Id = id;
                Variant = variant;
                Environment = environment;
                Hosts = hosts;
                RunWidth = width;
            }

            public string Id { get; }
            public string Variant { get; }
            public Dictionary<string, string?> Environment { get; }
            public PowerShellHost[] Hosts { get; }
            public int RunWidth { get; }
        }

        private static IEnumerable<Run> Runs() {
            foreach (string id in new[] { "format-table", "format-consoletable", "format-list", "format-consolelist", "format-consoletree", "markup", "write-consoletext", "job" }) {
                yield return new Run(id, "rich", PowerShellRunner.Environment("dark"), PowerShellHost.All);
            }
            Dictionary<string, string?> plain = PowerShellRunner.Environment("dark", mode: "plain");
            yield return new Run("format-consoletable", "plain", plain, new[] { PowerShellHost.Core });
            Dictionary<string, string?> noColor = PowerShellRunner.Environment("dark");
            noColor["CONSOLEMONSTER_COLOR"] = null;
            noColor["NO_COLOR"] = "1";
            yield return new Run("format-consoletable", "nocolor", noColor, new[] { PowerShellHost.Core });
            Dictionary<string, string?> ascii = PowerShellRunner.Environment("dark", "16", "ascii");
            yield return new Run("format-consoletable", "16-ascii", ascii, new[] { PowerShellHost.Desktop });
            yield return new Run("format-consoletable", "narrow", PowerShellRunner.Environment("dark"), new[] { PowerShellHost.Core }, 56);
        }

        private static string Path_(Site site, Run run, PowerShellHost host) => Path.Combine(site.Captures, "powershell", run.Id + "." + run.Variant + "." + host.Id + ".ansi");

        public static void Capture(Site site) {
            foreach (Run run in Runs()) {
                foreach (PowerShellHost host in run.Hosts) {
                    List<OutputChunk> chunks = PowerShellRunner.Run(site, host, Twins.Script(site, "powershell", run.Id), run.RunWidth, 120, run.Environment);
                    string path = Path_(site, run, host);
                    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                    File.WriteAllBytes(path, PowerShellRunner.Bytes(chunks));
                    Console.WriteLine("captured " + Path.GetRelativePath(site.Website, path));
                }
            }
        }

        public static void Render(Site site, JsonObject hosts) {
            JsonObject examples = new JsonObject();
            List<string> problems = new List<string>();
            foreach (Run run in Runs()) {
                JsonObject example = examples[run.Id] as JsonObject ?? new JsonObject { ["script"] = Site.ReadText(Twins.Script(site, "powershell", run.Id)) };
                examples[run.Id] = example;
                JsonObject variant = new JsonObject();
                foreach (PowerShellHost host in run.Hosts) {
                    string path = Path_(site, run, host);
                    if (!File.Exists(path)) {
                        problems.Add("missing capture " + Path.GetFileName(path) + "; run the capture command.");
                        continue;
                    }
                    CellBuffer frame = Site.Crop(TrimTop(Parity.Transcript(File.ReadAllBytes(path), run.RunWidth, 120)));
                    variant[host.Id] = new JsonObject {
                        ["host"] = (host == PowerShellHost.Core ? "PowerShell " : "Windows PowerShell ") + hosts[host.Id]?.GetValue<string>(),
                        ["frame"] = site.Frame(frame, null)
                    };
                }
                example[run.Variant] = variant;
            }
            examples["artifacts"] = new JsonObject { ["script"] = Site.ReadText(Twins.Script(site, "powershell", "artifacts")) };
            site.WriteJson(site.Data("powershell"), new JsonObject {
                ["examples"] = examples,
                ["families"] = Families(site)
            });
            WidgetsPage.Report(problems, "powershell");
        }

        // Built-in formatting starts with an empty line; the page frames start at the first line with text.
        private static CellBuffer TrimTop(CellBuffer frame) {
            int first = 0;
            while (first < frame.Height - 1 && frame.GetRowText(first).Trim().Length == 0) {
                first++;
            }
            if (first == 0) {
                return frame;
            }
            CellBuffer trimmed = new CellBuffer(frame.Width, frame.Height - first);
            trimmed.Blit(frame, 0, first, frame.Width, frame.Height - first, 0, 0);
            return trimmed;
        }

        // The cmdlets grouped by verb, with the synopsis from the module's MAML help.
        private static JsonArray Families(Site site) {
            XmlDocument help = new XmlDocument();
            help.Load(Path.Combine(site.Repository, "Module", "en-US", "ConsoleMonster-help.xml"));
            XmlNamespaceManager ns = new XmlNamespaceManager(help.NameTable);
            ns.AddNamespace("command", "http://schemas.microsoft.com/maml/dev/command/2004/10");
            ns.AddNamespace("maml", "http://schemas.microsoft.com/maml/2004/10");
            SortedDictionary<string, List<(string Name, string Synopsis)>> verbs = new SortedDictionary<string, List<(string, string)>>(StringComparer.Ordinal);
            foreach (XmlNode command in help.SelectNodes("//command:command", ns)!) {
                string name = command.SelectSingleNode("command:details/command:name", ns)?.InnerText.Trim() ?? string.Empty;
                string synopsis = command.SelectSingleNode("command:details/maml:description/maml:para", ns)?.InnerText.Trim() ?? string.Empty;
                string verb = name.Split('-')[0];
                if (!verbs.TryGetValue(verb, out List<(string, string)>? list)) {
                    list = new List<(string, string)>();
                    verbs[verb] = list;
                }
                list.Add((name, synopsis));
            }
            string[] order = { "Write", "Format", "New", "Start", "Update", "Read", "Show", "Export", "Invoke" };
            JsonArray families = new JsonArray();
            foreach (string verb in order.Concat(verbs.Keys.Where(v => !order.Contains(v)))) {
                if (!verbs.TryGetValue(verb, out List<(string Name, string Synopsis)>? list)) {
                    continue;
                }
                JsonArray items = new JsonArray();
                foreach ((string name, string synopsis) in list.OrderBy(c => c.Name, StringComparer.Ordinal)) {
                    items.Add(new JsonObject { ["name"] = name, ["synopsis"] = synopsis, ["url"] = "/powershell/commands/" + name.ToLowerInvariant() + "/" });
                }
                families.Add(new JsonObject { ["verb"] = verb, ["count"] = list.Count, ["commands"] = items });
            }
            return families;
        }
    }
}
