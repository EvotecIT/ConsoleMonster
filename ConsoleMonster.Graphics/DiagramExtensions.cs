using System;
using ChartForgeX.Topology;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Graphics {
    /// <summary>Converts ChartForgeX topology charts to terminal diagrams.</summary>
    public static class DiagramExtensions {
        /// <summary>Imports a topology chart as a Diagram with its Graphics renderer attached.</summary>
        /// <remarks>The source chart remains the backing model, retaining its richer metadata when the returned Diagram
        /// is edited. Finish configuring the chart before importing it, then make edits through the Diagram rather than
        /// sharing the chart with another writer. Register DiagramRendererProvider once at startup
        /// to enable Graphics rendering for other diagrams and file loading on every supported target framework.</remarks>
        public static Diagram ToDiagram(this TopologyChart chart) =>
            DiagramConversion.From(chart ?? throw new ArgumentNullException(nameof(chart)));
    }
}
