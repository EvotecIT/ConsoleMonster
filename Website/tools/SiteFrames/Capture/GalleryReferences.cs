using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Graphics;
using ConsoleMonster.Export;
using ConsoleMonster.Output;
using ConsoleMonster.Styling;
using SiteFrames.Pages;

namespace SiteFrames.Capture {
    /// <summary>Static gallery references retain cells and styles; the contact sheet uses the same frames.</summary>
    internal static class GalleryReferences {
        internal static readonly string[] Themes = { "Dark", "Light", "ColorBlindSafe" };

        internal static void Verify(Site site, string themeName, bool update = false) {
            ITheme theme = themeName switch { "Dark" => Theme.Dark, "Light" => Theme.Light, "ColorBlindSafe" => Theme.ColorBlindSafe, _ => throw new ArgumentException("Unknown reference theme.", nameof(themeName)) };
            string directory = Path.Combine(site.Website, "captures", "gallery", "references", themeName);
            if (update) Directory.CreateDirectory(directory);
            foreach (GalleryPage.Demo demo in GalleryPage.Read(site).Where(d => d.Metadata["mode"]!.GetValue<string>() == "static")) {
                CellBuffer frame = GalleryFrames.Run(demo, 0, theme: theme, width: 120, height: 42);
                CellBuffer fluent = GalleryFrames.Run(demo, 0, true, theme, 120, 42);
                Same(frame, fluent, demo.Slug + " " + themeName + " fluent");
                string path = Path.Combine(directory, demo.Slug + ".json");
                if (update) {
                    var target = new RecordingTarget(CSharpRunner.Profile(frame.Width, frame.Height, ColorDepth.TrueColor, GlyphTier.Full));
                    using (var writer = ConsoleMonster.Terminal.CreateOutput(target.Profile, target.Writer)) writer.WriteBlock(frame);
                    File.WriteAllText(path, JsonSerializer.Serialize(new Reference(frame.Width, frame.Height, target.Output)) + "\n", new UTF8Encoding(false));
                } else {
                    Reference reference = JsonSerializer.Deserialize<Reference>(File.ReadAllText(path))!;
                    Same(Parity.Transcript(Encoding.UTF8.GetBytes(reference.Vt), reference.Width, reference.Height), frame, demo.Slug + " " + themeName);
                }
            }
        }

        internal static string ContactSheet(Site site, string directory) {
            Directory.CreateDirectory(directory);
            var frames = new List<(string Name, CellBuffer Frame)>();
            foreach (GalleryPage.Demo demo in GalleryPage.Read(site).Where(d => d.Metadata["mode"]!.GetValue<string>() == "static")) {
                CellBuffer frame = GalleryFrames.Run(demo, 0, theme: Theme.Dark, width: 120, height: 42);
                frame.SavePng(Path.Combine(directory, demo.Slug + ".png"), new GraphicsExportOptions { Frame = new ExportOptions { Theme = ExportTheme.Dark } });
                frames.Add((demo.Slug, frame));
            }
            int rows = 0;
            for (int i = 0; i < frames.Count; i += 2) rows += Math.Max(frames[i].Frame.Height, i + 1 < frames.Count ? frames[i + 1].Frame.Height : 0) + 3;
            CellBuffer sheet = new CellBuffer(244, rows);
            RenderContext context = ConsoleMonster.Terminal.CreateContext(CSharpRunner.Profile(244, rows, ColorDepth.TrueColor, GlyphTier.Full), Theme.Dark);
            int y = 0;
            for (int i = 0; i < frames.Count; i += 2) {
                int height = 0;
                for (int c = 0; c < 2 && i + c < frames.Count; c++) {
                    var entry = frames[i + c];
                    new Region(sheet).Write(c * 124, y, entry.Name, context.Style(ThemeToken.Accent).With(TextDecoration.Bold));
                    sheet.Blit(entry.Frame, 0, 0, entry.Frame.Width, entry.Frame.Height, c * 124, y + 1);
                    height = Math.Max(height, entry.Frame.Height);
                }
                y += height + 3;
            }
            string path = Path.Combine(directory, "gallery-dark-120-contact-sheet.png");
            sheet.SavePng(path, new GraphicsExportOptions { Frame = new ExportOptions { Theme = ExportTheme.Dark } });
            return path;
        }

        private static void Same(CellBuffer expected, CellBuffer actual, string name) {
            ParityResult result = Parity.Compare(expected, actual);
            if (!result.Identical) throw new InvalidDataException(name + ": " + result.Detail);
        }

        internal sealed record Reference(int Width, int Height, string Vt);
    }
}
