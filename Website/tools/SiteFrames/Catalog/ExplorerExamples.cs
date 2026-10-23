using System;
using System.IO;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;
using ConsoleMonster.Styling;
using SiteFrames.Capture;

namespace SiteFrames.Catalog {
    /// <summary>Runs the published explorer program; only scripted input and capture geometry live in the tool.</summary>
    internal static class ExplorerExamples {
        public static string ProgramPath(Site site) => Path.Combine(site.Website, "examples", "docs", "object-explorer.cs");

        public static CellBuffer Frame(Site site, int width, ColorDepth depth, GlyphTier glyphs, ITheme theme, string keys = "F10") {
            var capture = new ScreenCapture(width, 20);
            TerminalProfile profile = CSharpRunner.Profile(width, 20, depth, glyphs);
            CSharpRunner.Execute(ProgramPath(site), profile, theme, new InteractiveOptions {
                Mode = InteractionMode.Rich,
                Input = new ScriptedInput(keys) { Paced = true },
                Capture = capture,
                Profile = profile,
                Theme = theme
            });
            return capture.LastFrame ?? throw new InvalidOperationException("Explorer program produced no frame.");
        }
    }
}
