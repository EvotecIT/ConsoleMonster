using System;
using System.Globalization;
using System.IO;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Examples.Diagrams;
using ConsoleMonster.Export;
using ConsoleMonster.Graphics;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Examples.Extensions {
    /// <summary>Runs the initializer/fluent diagram ladder with the optional Graphics renderer.</summary>
    internal static class DiagramLadderExample {
        internal static int Run(string[] args) {
            DiagramRendererProvider.Register();
            if (args.Length > 2 && args[1] == "--capture") {
                string directory = Path.GetFullPath(args[2]);
                Directory.CreateDirectory(directory);
                for (int level = 1; level <= 8; level++) {
                    int width = level == 1 ? 80 : level == 5 ? 140 : level == 8 ? 100 : 120;
                    foreach (bool fluent in new[] { false, true }) {
                        Diagram diagram = DiagramLadder.Create(level, fluent);
                        if (level == 7) diagram.Step(2);
                        string name = "level-" + level.ToString(CultureInfo.InvariantCulture) + (fluent ? "-fluent" : "-initializer");
                        Save(diagram, directory, name, width);
                    }
                }
                Save(DiagramLadder.LivePingInitializer(), directory, "live-narrow-60", 60);
                Save(DiagramLadder.StatusInitializer(), directory, "status-narrow-34", 34);
                return 0;
            }
            int selected = args.Length > 1 ? int.Parse(args[1], CultureInfo.InvariantCulture) : 1;
            DiagramLadder.Create(selected, args.Length > 2 && args[2] == "fluent").Run();
            return 0;
        }

        private static void Save(Diagram diagram, string directory, string name, int width) {
            var frame = new ExportOptions { Width = width, Glyphs = GlyphTier.Full, Theme = ExportTheme.Dark };
            File.WriteAllText(Path.Combine(directory, name + ".txt"), ConsoleExporter.ToText(diagram, frame));
            File.WriteAllText(Path.Combine(directory, name + ".svg"), ConsoleExporter.ToSvg(diagram, frame));
            File.WriteAllBytes(Path.Combine(directory, name + ".png"), ConsoleGraphicsExtensions.ToPng(diagram, new GraphicsExportOptions { Frame = frame }));
        }
    }
}
