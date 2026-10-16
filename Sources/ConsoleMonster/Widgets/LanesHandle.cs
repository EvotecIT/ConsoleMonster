using ConsoleMonster.Live;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Widgets {
    /// <summary>Thread-safe live handle for a <see cref="Lanes"/> widget.</summary>
    public sealed class LanesHandle : LiveHandle {
        /// <summary>Creates a handle.</summary>
        /// <param name="dispatcher">The dispatcher.</param>
        /// <param name="target">The lanes name.</param>
        public LanesHandle(ILiveDispatcher dispatcher, string target) : base(dispatcher, target) {
        }

        /// <summary>Adds a lane.</summary>
        /// <param name="key">Unique key.</param>
        /// <param name="label">Label, or <c>null</c> for the key.</param>
        /// <param name="total">Planned steps.</param>
        public void Add(string key, string? label = null, double total = 0) => Post("add", key, label, total: total);

        /// <summary>Marks a lane as running.</summary>
        /// <param name="key">The lane.</param>
        /// <param name="detail">What it is doing.</param>
        public void Start(string key, string? detail = null) => Post("state", key, detail, "running");

        /// <summary>Records completed steps and their result.</summary>
        /// <param name="key">The lane.</param>
        /// <param name="result">The step result (counted as passed, failed, warning or skipped).</param>
        /// <param name="detail">What the lane is doing next, or <c>null</c>.</param>
        /// <param name="steps">Steps completed (default 1).</param>
        public void Advance(string key, StatusKind result = StatusKind.Pass, string? detail = null, double steps = 1) => Post("advance", key, detail, result.ToString(), steps);

        /// <summary>Sets what the lane is doing now.</summary>
        /// <param name="key">The lane.</param>
        /// <param name="detail">The detail.</param>
        public void Detail(string key, string detail) => Post("detail", key, detail);

        /// <summary>Finishes a lane; without a status it becomes Fail when any step failed, Warn when any warned, else Pass.</summary>
        /// <param name="key">The lane.</param>
        /// <param name="status">The final status, or <c>null</c> to derive it.</param>
        public void Complete(string key, StatusKind? status = null) => Post("complete", key, state: status.HasValue ? status.Value.ToString() : null);
    }
}
