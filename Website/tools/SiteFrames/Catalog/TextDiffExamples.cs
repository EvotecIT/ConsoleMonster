using System.IO;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Styling;
using SiteFrames.Capture;

namespace SiteFrames.Catalog {
    /// <summary>The complete configuration comparison program shown with its rendered output.</summary>
    internal static class TextDiffExamples {
        /// <summary>Locates the standalone program a reader can run with the ConsoleMonster package.</summary>
        /// <param name="site">The website containing the published examples.</param>
        /// <returns>The C# program's absolute path.</returns>
        public static string ProgramPath(Site site) => Path.Combine(site.Website, "examples", "docs", "text-diff.cs");

        /// <summary>Compiles and executes the exact shown program against the requested terminal profile.</summary>
        /// <param name="site">The website containing the published program.</param>
        /// <param name="width">The terminal width in cells.</param>
        /// <param name="depth">The terminal's supported colour depth.</param>
        /// <param name="glyphs">The terminal's supported glyph tier.</param>
        /// <param name="theme">The theme used by the core renderer.</param>
        /// <returns>The actual cells printed by the program.</returns>
        public static CellBuffer Run(Site site, int width, ColorDepth depth, GlyphTier glyphs, ITheme theme) =>
            CSharpRunner.Run(ProgramPath(site), width, depth, glyphs, theme);
    }
}
