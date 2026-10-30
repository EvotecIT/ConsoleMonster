using System;
using System.Globalization;
using ChartForgeX.Topology;
using ConsoleMonster.Live;

namespace ConsoleMonster.Graphics {
    /// <summary>A thread-safe live handle for a <see cref="TopologyDiagram"/>: node values and statuses for dashboards, and step control.</summary>
    public sealed class TopologyDiagramHandle : LiveHandle {
        /// <summary>Creates a handle.</summary>
        /// <param name="dispatcher">The dispatcher.</param><param name="widgetId">The view identifier.</param>
        public TopologyDiagramHandle(ILiveDispatcher dispatcher, string widgetId) : base(dispatcher, widgetId) { }

        /// <summary>Shows a live value on a node, such as <c>98 %</c> or <c>12 queued</c>, with an optional status.</summary>
        /// <param name="nodeId">The node id.</param><param name="value">The value, or null to show the subtitle again.</param><param name="status">An optional live status.</param>
        public void Value(string nodeId, string? value, TopologyHealthStatus? status = null) =>
            Post("value", nodeId ?? throw new ArgumentNullException(nameof(nodeId)), value, status?.ToString().ToLowerInvariant());

        /// <summary>Changes a node's live status.</summary>
        /// <param name="nodeId">The node id.</param><param name="status">The status.</param>
        public void Status(string nodeId, TopologyHealthStatus status) =>
            Post("status", nodeId ?? throw new ArgumentNullException(nameof(nodeId)), null, status.ToString().ToLowerInvariant());

        /// <summary>Selects a use case.</summary>
        /// <param name="scenarioId">The use case id, or null for the plain diagram.</param>
        public void Scenario(string? scenarioId) => Post("scenario", null, scenarioId);

        /// <summary>Highlights a step of the selected use case.</summary>
        /// <param name="index">The zero-based step.</param>
        public void Step(int index) => Post("step", null, index.ToString(CultureInfo.InvariantCulture), null, index);

        /// <summary>Focuses a node so its details are shown.</summary>
        /// <param name="nodeId">The node id, or null to clear the focus.</param>
        public void Focus(string? nodeId) => Post("focus", nodeId);

        /// <summary>Starts playback.</summary>
        public void Play() => Post("play");

        /// <summary>Stops playback.</summary>
        public void Pause() => Post("pause");
    }
}
