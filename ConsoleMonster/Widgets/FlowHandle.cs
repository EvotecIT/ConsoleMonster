using ConsoleMonster.Live;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Widgets {
    /// <summary>A thread-safe live handle for a flow strip.</summary>
    public sealed class FlowHandle : LiveHandle {
        /// <summary>Creates a flow handle.</summary>
        /// <param name="dispatcher">The dispatcher.</param><param name="widgetId">The flow identifier.</param>
        public FlowHandle(ILiveDispatcher dispatcher, string widgetId) : base(dispatcher, widgetId) { }
        /// <summary>Updates a node's status.</summary>
        /// <param name="key">The node key.</param><param name="status">The new status.</param>
        public void Node(string key, StatusKind status) => Post("node", key, state: status.ToString());
        /// <summary>Updates an edge's status and optionally cuts or reconnects it.</summary>
        /// <param name="key">The edge key.</param><param name="status">The new status.</param><param name="cut">Null preserves connectivity; true cuts; false reconnects.</param>
        public void Edge(string key, StatusKind status, bool? cut = null) => Post(cut.HasValue ? (cut.Value ? "cut" : "connect") : "edge", key, state: status.ToString());
    }
}
