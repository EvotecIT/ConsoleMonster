using System;
using ChartForgeX.Topology;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Graphics {
    /// <summary>Converts ChartForgeX topology charts to terminal diagrams.</summary>
    public static class DiagramExtensions {
        /// <summary>Imports a topology chart as a Diagram with its Graphics renderer attached.</summary>
        /// <remarks>Finish configuring the source chart before importing it and do not mutate that chart afterwards.
        /// Configure the returned Diagram through its own model. Register DiagramRendererProvider once at startup
        /// to enable Graphics rendering for other diagrams and file loading on every supported target framework.</remarks>
        public static Diagram ToDiagram(this TopologyChart chart) =>
            DiagramConversion.From(chart ?? throw new ArgumentNullException(nameof(chart)));
    }
}
