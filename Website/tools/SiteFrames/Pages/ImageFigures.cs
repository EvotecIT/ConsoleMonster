using System.IO;
using System.Text.Json.Nodes;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Graphics;
using ConsoleMonster.Styling;

namespace SiteFrames.Pages {
    /// <summary>
    /// Inline image figures from the downloadable sample chart: the half-block tier in 24-bit and 256 colours, and the
    /// placeholder that redirected and no-colour output get. Kitty, iTerm2 and Sixel pixels are not terminal text, so the
    /// page shows the source image for them instead of a frame.
    /// </summary>
    internal static class ImageFigures {
        private const string Name = "requests-chart.png";

        public static void Render(Site site) {
            byte[] png = File.ReadAllBytes(site.Static("examples/images/" + Name));
            ConsoleImage Image(int width) => new ConsoleImage(png, Name) { Width = width };
            Add(site, "inline-images-60", "Half blocks: the upper pixel in the foreground colour, the lower one in the background", Image(56), 60, ColorDepth.TrueColor, GlyphTier.Full, "HalfBlock");
            Add(site, "inline-images-256", "A 256-colour terminal", Image(56), 60, ColorDepth.Ansi256, GlyphTier.Full, "HalfBlock");
            Add(site, "inline-images-placeholder", "Redirected, CI and no-colour output", Image(56), 60, ColorDepth.None, GlyphTier.Ascii, "None");
            DocCards.Offer("inline-images-60", Site.Render(Image(56), 60, ColorDepth.TrueColor, GlyphTier.Full, Theme.Dark), Frames.Screen.Dark);
        }

        // -Protocol pins the tier the frame shows, so the command draws the same cells in any terminal.
        private static void Add(Site site, string id, string label, ConsoleImage image, int width, ColorDepth depth, GlyphTier glyphs, string protocol) {
            string script = "Show-ConsoleImage -Path ./" + Name + " -Width " + image.Width + " -Protocol " + protocol;
            JsonObject figure = Figures.Figure(site, "Show-ConsoleImage", label, image, width, depth: depth, glyphs: glyphs);
            figure["file"] = "inline-images.ps1";
            figure["script"] = script;
            figure["scriptFull"] = script;
            Figures.Add(id, figure);
        }
    }
}
