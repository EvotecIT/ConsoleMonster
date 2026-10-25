using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Styling;
using SiteFrames.Capture;

namespace SiteFrames.Pages {
    /// <summary>
    /// The home page: one table from a complete C# program (<c>Website/examples/home/controllers.cs</c>, run here) and from its
    /// PowerShell twin (captured on both hosts and compared cell by cell), at two widths, and the module's facts.
    /// </summary>
    internal static class HomePage {
        public const int Wide = 92;
        public const int Narrow = 44;

        public static string ProgramPath(Site site) => Path.Combine(site.Website, "examples", "home", "controllers.cs");

        /// <summary>What the controllers program prints at a width, in a theme.</summary>
        public static CellBuffer Run(Site site, int width, ITheme theme) => CSharpRunner.Run(ProgramPath(site), width, ColorDepth.TrueColor, GlyphTier.Full, theme);

        public static void Capture(Site site) {
            Twins.Capture(site, "home", "controllers", Wide);
        }

        public static void Render(Site site, JsonObject hosts) {
            List<string> problems = new List<string>();
            string program = ProgramPath(site);
            (string csharp, string csharpFull) = SourceSnippets.Program(Site.ReadText(program));
            CellBuffer dark = Run(site, Wide, Theme.Dark);
            JsonObject controllers = new JsonObject {
                ["width"] = Wide,
                ["csharp"] = csharp,
                ["csharpFull"] = csharpFull,
                ["csharpFile"] = "Program.cs",
                ["powershell"] = SourceSnippets.Program(Site.ReadText(Twins.Script(site, "home", "controllers"))).Short,
                ["powershellFull"] = WidgetsPage.Longer(SourceSnippets.Program(Site.ReadText(Twins.Script(site, "home", "controllers")))),
                ["powershellFile"] = "controllers.ps1",
                ["frame"] = site.Frame(Site.Crop(dark), Site.Crop(Run(site, Wide, Theme.Light))),
                ["narrow"] = site.Frame(Run(site, Narrow, Theme.Dark), Run(site, Narrow, Theme.Light)),
                ["parity"] = Twins.Check(site, "home", "controllers", Wide, dark, hosts, problems)
            };
            SourceStyles.Add(site, controllers, program);
            string manifest = File.ReadAllText(Path.Combine(site.Repository, "Module", "ConsoleMonster.psd1"));
            Match cmdlets = Regex.Match(manifest, @"CmdletsToExport\s*=\s*@\(([^)]*)\)");
            int count = cmdlets.Success ? Regex.Matches(cmdlets.Groups[1].Value, "'[^']+'").Count : 0;
            site.WriteJson(site.Data("home"), new JsonObject {
                ["controllers"] = controllers,
                ["cmdlets"] = count,
                ["widgets"] = WidgetsPage.Examples.Length
            });
            WidgetsPage.Report(problems, "home");
        }
    }
}
