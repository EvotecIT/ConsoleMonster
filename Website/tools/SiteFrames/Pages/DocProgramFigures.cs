using System;
using System.IO;
using System.Text.Json.Nodes;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Styling;
using SiteFrames.Capture;

namespace SiteFrames.Pages {
    /// <summary>
    /// C# programs on the docs pages whose frame is what they print: the dashboard frame (C# only, as the parity catalogue
    /// declares it) at a wide and a narrow width, and the headless test that drives an app and prints the screen it kept.
    /// </summary>
    internal static class DocProgramFigures {
        private static string ProgramPath(Site site, string id) => Path.Combine(site.Website, "examples", "docs", id + ".cs");

        /// <summary>Prints what each program draws, as text, for review.</summary>
        public static int Review(Site site) {
            foreach (int width in new[] { 110, 60 }) { Console.WriteLine(CSharpRunner.Run(ProgramPath(site, "dashboard-frame"), width, ColorDepth.TrueColor, GlyphTier.Full, Theme.Dark).ToPlainText()); }
            Console.WriteLine(CSharpRunner.Run(ProgramPath(site, "headless"), 60, ColorDepth.TrueColor, GlyphTier.Full, Theme.Dark).ToPlainText());
            return 0;
        }

        public static void Render(Site site) {
            foreach (int width in new[] { 110, 60 }) {
                CellBuffer dark = CSharpRunner.Run(ProgramPath(site, "dashboard-frame"), width, ColorDepth.TrueColor, GlyphTier.Full, Theme.Dark);
                CellBuffer light = CSharpRunner.Run(ProgramPath(site, "dashboard-frame"), width, ColorDepth.TrueColor, GlyphTier.Full, Theme.Light);
                JsonObject figure = Figures.WithProgram(site, new JsonObject {
                    ["title"] = "dotnet run",
                    ["label"] = width >= 80 ? "The estate frame at " + width + " columns: meters, the attention list beside the summary sections, details, legend and footer"
                        : "The same frame at " + width + " columns: the title facts wrap, meters stack and sections take the full width",
                    ["meta"] = new JsonArray(width + "×" + dark.Height, "24-bit", "Full"),
                    ["frame"] = site.Frame(dark, light)
                }, ProgramPath(site, "dashboard-frame"));
                DocCards.Offer("dashboard-frame-" + width, dark, Frames.Screen.Dark);
                Figures.Add("dashboard-frame-" + width, figure);
            }

            // The headless snapshot keeps the cells the app drew in the headless terminal's own (dark) theme.
            CellBuffer screen = CSharpRunner.Run(ProgramPath(site, "headless"), 60, ColorDepth.TrueColor, GlyphTier.Full, Theme.Dark);
            if (!screen.ToPlainText().Contains("Picked FS01", StringComparison.Ordinal)) {
                throw new InvalidOperationException("headless.cs did not print the screen after its key and click.");
            }
            JsonObject headless = Figures.WithProgram(site, new JsonObject {
                ["title"] = "dotnet run",
                ["label"] = "The screen HeadlessTerminal kept after Down and a click on FS01",
                ["meta"] = new JsonArray("60×" + screen.Height, "24-bit", "Full"),
                ["frame"] = site.Frame(screen, null)
            }, ProgramPath(site, "headless"));
            DocCards.Offer("headless-app", screen, Frames.Screen.Dark);
            Figures.Add("headless-app", headless);
        }
    }
}
