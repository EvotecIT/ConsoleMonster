using ConsoleMonster.Live;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Widgets {
    /// <summary>Thread-safe live handle for a <see cref="StatusList"/>.</summary>
    public sealed class StatusListHandle : LiveHandle {
        /// <summary>Creates a handle.</summary>
        /// <param name="dispatcher">The dispatcher.</param>
        /// <param name="target">The list name.</param>
        public StatusListHandle(ILiveDispatcher dispatcher, string target) : base(dispatcher, target) {
        }

        /// <summary>Adds or updates a row.</summary>
        /// <param name="key">Row key.</param>
        /// <param name="status">Status.</param>
        /// <param name="label">Label (markup), or <c>null</c> to keep it.</param>
        public void Set(string key, StatusKind status, string? label = null) => Post("set", key, label, status.ToString());

        /// <summary>Sets the right-aligned detail of a row.</summary>
        /// <param name="key">Row key.</param>
        /// <param name="detail">Detail text.</param>
        public void Detail(string key, string detail) => Post("detail", key, detail);
    }
}
