using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using ConsoleMonster.Layout;
using SiteFrames.Capture;
using SiteFrames.Catalog;

namespace SiteFrames.Pages {
    /// <summary>The home page: one table written from C# and from PowerShell, at two widths, and the module's facts.</summary>
    internal static class HomePage {
        public const int Wide = 92;
        public const int Narrow = 44;

        public static void Capture(Site site) {
            Twins.Capture(site, "home", "controllers", Wide);
        }

        public static void Render(Site site, JsonObject hosts) {
            List<string> problems = new List<string>();
            Func<IWidget> build = () => Catalog.Terminal.Capture(PageExamples.Controllers);
            string source = Path.Combine(site.Tool, "Catalog", "PageExamples.cs");
            JsonObject controllers = new JsonObject {
                ["width"] = Wide,
                ["csharp"] = SourceSnippets.Split(SourceSnippets.Get(source, "Controllers")).Short,
                ["csharpFull"] = WidgetsPage.Longer(SourceSnippets.Split(SourceSnippets.Get(source, "Controllers"))),
                ["powershell"] = SourceSnippets.Split(Site.ReadText(Twins.Script(site, "home", "controllers"))).Short,
                ["powershellFull"] = WidgetsPage.Longer(SourceSnippets.Split(Site.ReadText(Twins.Script(site, "home", "controllers")))),
                ["frame"] = site.Frame(build, Wide, crop: true),
                ["narrow"] = site.Frame(build, Narrow),
                ["parity"] = Twins.Check(site, "home", "controllers", Wide, build, hosts, problems)
            };
            string manifest = File.ReadAllText(Path.Combine(site.Repository, "Module", "ConsoleMonster.psd1"));
            Match cmdlets = Regex.Match(manifest, @"CmdletsToExport\s*=\s*@\(([^)]*)\)");
            int count = cmdlets.Success ? Regex.Matches(cmdlets.Groups[1].Value, "'[^']+'").Count : 0;
            site.WriteJson(site.Data("home"), new JsonObject {
                ["controllers"] = controllers,
                ["cmdlets"] = count,
                ["widgets"] = WidgetExamples.All().Count
            });
            WidgetsPage.Report(problems, "home");
        }
    }
}
