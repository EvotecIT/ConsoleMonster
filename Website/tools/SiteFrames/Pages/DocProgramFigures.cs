using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Nodes;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Styling;
using SiteFrames.Capture;

namespace SiteFrames.Pages {
    /// <summary>
    /// Docs figures whose frame is what a program prints: the server-estate dashboard frame at a narrow width (the gallery
    /// demo shows it wide), and the headless test that drives an app and prints the screen it kept, with its PowerShell twin.
    /// </summary>
    internal static class DocProgramFigures {
        private const int HeadlessWidth = 60;

        private static string DashboardProgram(Site site) => Path.Combine(site.Website, "examples", "gallery", "server-estate", "Program.cs");

        private static string HeadlessProgram(Site site) => Path.Combine(site.Website, "examples", "docs", "headless.cs");

        private static Dictionary<string, string?> Environment() {
            Dictionary<string, string?> environment = PowerShellRunner.Environment("dark");
            environment["CONSOLEMONSTER_DEVELOPMENT_CONFIGURATION"] = "Release";
            return environment;
        }

        /// <summary>Records the headless program's PowerShell twin on every available host.</summary>
        public static void Capture(Site site) => Twins.Capture(site, "docs", "headless", HeadlessWidth, "dark-" + HeadlessWidth, Environment());

        /// <summary>Prints what each program draws, as text, for review.</summary>
        public static int Review(Site site) {
            Console.WriteLine(CSharpRunner.Run(DashboardProgram(site), 60, ColorDepth.TrueColor, GlyphTier.Full, Theme.Dark).ToPlainText());
            Console.WriteLine(CSharpRunner.Run(HeadlessProgram(site), HeadlessWidth, ColorDepth.TrueColor, GlyphTier.Full, Theme.Dark).ToPlainText());
            return 0;
        }

        // The generated fluent program must print the same cells as the initializer program.
        private static bool SameFluent(string path, string id, int width, CellBuffer expected) {
            CellBuffer fluent = CSharpRunner.Run(path, width, ColorDepth.TrueColor, GlyphTier.Full, Theme.Dark, fluent: true);
            ParityResult result = Parity.Compare(expected, fluent);
            if (!result.Identical) { WidgetsPage.Report(new List<string> { id + ": C# fluent " + result.Detail }, id + "/fluent"); }
            return result.Identical;
        }

        public static void Render(Site site) {
            CellBuffer wide = CSharpRunner.Run(DashboardProgram(site), 110, ColorDepth.TrueColor, GlyphTier.Full, Theme.Dark);
            DocCards.Offer("dashboard-frame-110", wide, Frames.Screen.Dark);
            CellBuffer dark = CSharpRunner.Run(DashboardProgram(site), 60, ColorDepth.TrueColor, GlyphTier.Full, Theme.Dark);
            CellBuffer light = CSharpRunner.Run(DashboardProgram(site), 60, ColorDepth.TrueColor, GlyphTier.Full, Theme.Light);
            JsonObject narrow = Figures.WithProgram(site, new JsonObject {
                ["title"] = "dotnet run",
                ["label"] = "The server estate frame at 60 columns: the facts wrap under the title, meters stack and sections take the full width",
                ["meta"] = new JsonArray("60×" + dark.Height, "24-bit", "Full"),
                ["frame"] = site.Frame(dark, light)
            }, DashboardProgram(site));
            narrow["parity"] = new JsonObject { ["fluent"] = SameFluent(DashboardProgram(site), "server-estate at 60", 60, dark) };
            Figures.Add("dashboard-frame-60", narrow);

            // The kept screen is text, so the PowerShell twin (Invoke-ConsoleHeadless) prints the same panel.
            CellBuffer screen = CSharpRunner.Run(HeadlessProgram(site), HeadlessWidth, ColorDepth.TrueColor, GlyphTier.Full, Theme.Dark);
            if (!screen.ToPlainText().Contains("Picked FS01", StringComparison.Ordinal)) {
                throw new InvalidOperationException("headless.cs did not print the screen after its key and click.");
            }
            JsonObject hosts = (JsonObject)JsonNode.Parse(File.ReadAllText(Path.Combine(site.Captures, "hosts.json")))!;
            List<string> problems = new List<string>();
            JsonObject twins = Twins.Check(site, "docs", "headless", HeadlessWidth, screen, hosts, problems, "dark-" + HeadlessWidth, Environment());
            WidgetsPage.Report(problems, "headless");
            JsonObject headless = Figures.WithProgram(site, new JsonObject {
                ["title"] = "Invoke-ConsoleHeadless",
                ["file"] = "headless.ps1",
                ["label"] = "The screen the headless run kept after Down and a click on FS01",
                ["meta"] = new JsonArray(HeadlessWidth + "×" + screen.Height, "24-bit", "Full"),
                ["frame"] = site.Frame(screen, null)
            }, HeadlessProgram(site));
            (string script, string scriptFull) = SourceSnippets.Program(Site.ReadText(Twins.Script(site, "docs", "headless")));
            headless["script"] = script;
            headless["scriptFull"] = scriptFull;
            headless["parity"] = new JsonObject { ["powershell"] = twins, ["fluent"] = SameFluent(HeadlessProgram(site), "headless", HeadlessWidth, screen) };
            DocCards.Offer("headless-app", screen, Frames.Screen.Dark);
            Figures.Add("headless-app", headless);
        }
    }
}
