using System;
using ChartForgeX.Topology;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Graphics {
    /// <summary>Connects the optional ChartForgeX renderer and file importer to the core Diagram widget.</summary>
    /// <remarks>Call Register once at application startup, before creating diagrams. This explicit path works on
    /// net472 and netstandard2.0 as well as modern .NET; merely mentioning a package does not load its assembly.</remarks>
    public static class DiagramRendererProvider {
        /// <summary>Registers topology rendering, diagram-file import and PNG frame export for this process.</summary>
        public static void Register() {
            Diagram.RendererFactory = diagram => new DiagramGraphicsRenderer(diagram);
            Diagram.ChartLoader = source => source is TopologyChart chart ? DiagramConversion.From(chart) : throw new ArgumentException("Diagram.From expects a ChartForgeX TopologyChart.", nameof(source));
            Diagram.FileLoader = (path, id) => DiagramFileReader.Load(path, id);
            PngFrameExportProvider.Register();
        }
    }
}
