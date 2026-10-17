using ConsoleMonster.Live;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Widgets {
    /// <summary>Thread-safe live handle for a <see cref="Spinner"/>.</summary>
    public sealed class SpinnerHandle : LiveHandle {
        /// <summary>Creates a handle.</summary>
        /// <param name="dispatcher">The dispatcher.</param>
        /// <param name="target">The spinner name.</param>
        public SpinnerHandle(ILiveDispatcher dispatcher, string target) : base(dispatcher, target) {
        }

        /// <summary>Restarts the clock and returns to running, optionally with a new label.</summary>
        /// <param name="label">The new label (markup), or null to keep it.</param>
        public void Start(string? label = null) => Post("start", text: label);

        /// <summary>Changes the label.</summary>
        /// <param name="label">The label (markup).</param>
        public void Label(string label) => Post("label", text: label);

        /// <summary>Changes the muted detail, for example the current item.</summary>
        /// <param name="detail">The detail, or null to clear it.</param>
        public void Detail(string? detail) => Post("detail", text: detail);

        /// <summary>Sets the final status (Pass, Fail, Warn, Skip), optionally with a closing detail.</summary>
        /// <param name="status">The status.</param>
        /// <param name="detail">The detail, or null to keep the current one.</param>
        public void Complete(StatusKind status = StatusKind.Pass, string? detail = null) => Post("state", text: detail, state: status.ToString());
    }
}
