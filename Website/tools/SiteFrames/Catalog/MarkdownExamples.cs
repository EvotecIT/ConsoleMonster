using System.IO;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;
using ConsoleMonster.Styling;
using SiteFrames.Capture;

namespace SiteFrames.Catalog {
    /// <summary>Runs the complete, downloadable program used by the Markdown guide.</summary>
    internal static class MarkdownExamples {
        internal static string ProgramPath(Site site) => Path.Combine(site.Website, "examples", "docs", "markdown.cs");

        internal static CellBuffer Run(Site site, int width, ColorDepth depth, GlyphTier glyphs, ITheme theme) =>
            CSharpRunner.Run(ProgramPath(site), width, depth, glyphs, theme);

        /// <summary>Runs the same compiled program with scripted pager navigation and retains its actual first screen.</summary>
        internal static CellBuffer Pager(Site site, int width, ITheme theme) {
            ScreenCapture capture = new ScreenCapture(width, 18);
            var profile = CSharpRunner.Profile(width, 18, ColorDepth.TrueColor, GlyphTier.Full);
            using ScriptedInput input = new ScriptedInput("PageDown PageUp End Home Escape") { Paced = true };
            CSharpRunner.Execute(ProgramPath(site), profile, theme, new InteractiveOptions {
                Capture = capture, Input = input, Profile = profile, Theme = theme
            });
            return capture.Frames[0];
        }
    }
}
