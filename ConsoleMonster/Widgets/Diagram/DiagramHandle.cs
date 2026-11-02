using ConsoleMonster.Live;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Widgets {
    /// <summary>Posts diagram changes through the same live-update contract as PowerShell.</summary>
    public sealed class DiagramHandle : LiveHandle {
        /// <summary>Creates a handle for a live diagram.</summary>
        public DiagramHandle(ILiveDispatcher dispatcher, string widgetId) : base(dispatcher, widgetId) { }
        /// <summary>Updates a component's third line and optionally its status.</summary>
        /// <exception cref="System.ArgumentException">The node key is null, empty or whitespace. Validation runs before the update is queued.</exception>
        public void Value(string node, string? text, StatusKind? status = null) {
            var update = new WidgetUpdate { Key = node, Value = text };
            if (status.HasValue) update.Status = status.Value;
            Update(update);
        }
        /// <summary>Selects a use case.</summary>
        public void Scenario(string? id) => Post("scenario", text: id);
        /// <summary>Selects a zero-based step.</summary>
        public void Step(int index) => Post("step", value: index);
        /// <summary>Starts playback.</summary>
        public void Play() => Post("play");
        /// <summary>Pauses playback.</summary>
        public void Pause() => Post("pause");
        /// <summary>Focuses a component.</summary>
        public void Focus(string? node) => Post("focus", key: node);
    }
}
