using ConsoleMonster.Live;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Widgets {
    /// <summary>Thread-safe live handle for a <see cref="Phases"/> widget.</summary>
    public sealed class PhasesHandle : LiveHandle {
        /// <summary>Creates a handle.</summary>
        /// <param name="dispatcher">The dispatcher.</param>
        /// <param name="widgetId">The phases name.</param>
        public PhasesHandle(ILiveDispatcher dispatcher, string widgetId) : base(dispatcher, widgetId) {
        }

        /// <summary>Marks a phase as running (its duration is measured until the next state change).</summary>
        /// <param name="key">The phase.</param>
        public void Start(string key) => Post("state", key, state: "running");

        /// <summary>Finishes a phase.</summary>
        /// <param name="key">The phase.</param>
        /// <param name="status">The final status.</param>
        /// <param name="detail">A detail (default: the measured duration).</param>
        public void Complete(string key, StatusKind status = StatusKind.Pass, string? detail = null) => Post("state", key, detail, status.ToString());

        /// <summary>Sets a phase's detail (for example a percentage).</summary>
        /// <param name="key">The phase.</param>
        /// <param name="detail">The detail.</param>
        public void Detail(string key, string detail) => Post("detail", key, detail);
    }
}
