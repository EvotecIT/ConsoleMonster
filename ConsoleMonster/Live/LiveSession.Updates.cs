using System;
using System.Collections.Generic;
using System.Linq;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Live {
    public sealed partial class LiveSession {
        // Validation follows accepted topology, which can be ahead of the render thread's adopted targets.
        // Ordinary raw events keep their lock-free queue; topology changes and high-level update batches share this gate.
        private readonly object _updateGate = new object();
        private Dictionary<string, ILiveTarget> _updateTargets = new Dictionary<string, ILiveTarget>(StringComparer.Ordinal);
        private readonly Dictionary<RegionWidget, IWidget?> _updateContent = new Dictionary<RegionWidget, IWidget?>();
        private readonly Dictionary<string, ILiveTarget> _additionalTargets = new Dictionary<string, ILiveTarget>(StringComparer.Ordinal);
        private bool _discovered;

        internal void ApplyUpdate(string widgetId, WidgetUpdate update) {
            lock (_updateGate) {
                if (!_updateTargets.TryGetValue(widgetId, out ILiveTarget? target)) {
                    throw new ConsoleUpdateException("ConsoleMonsterUnknownWidget", "Unknown WidgetId '" + widgetId + "'. Available targets: " + string.Join(", ", _updateTargets.Keys.OrderBy(x => x, StringComparer.Ordinal)) + ".");
                }
                IReadOnlyList<LiveEvent> events = ConsoleUpdate.Events(target, update);
                foreach (LiveEvent item in events) { Post(item); }
            }
        }

        private bool TryGetUpdateTarget(string widgetId, out ILiveTarget? target) {
            lock (_updateGate) { return _updateTargets.TryGetValue(widgetId, out target); }
        }

        // Called only after admission succeeds, under the same gate as enqueue, so a rejected publication cannot alter validation.
        private void ProjectContent(LiveEvent item) {
            if (!_updateTargets.TryGetValue(item.WidgetId, out ILiveTarget? target) || !(target is RegionWidget region)) { return; }
            _updateContent[region] = item.Operation == "clear" ? null : item.Payload as IWidget;
            var next = new Dictionary<string, ILiveTarget>(StringComparer.Ordinal);
            var regions = new HashSet<RegionWidget>();
            var visited = new HashSet<IWidget>();
            void Visit(IWidget widget) {
                if (!visited.Add(widget)) { return; }
                if (widget is ILiveTarget live && live.WidgetId != "$log") { next[live.WidgetId] = live; }
                if (widget is RegionWidget slot) {
                    regions.Add(slot);
                    IWidget? content = _updateContent.TryGetValue(slot, out IWidget? replacement) ? replacement : slot.Content;
                    if (content != null) { Visit(content); }
                } else if (widget is IContainer container) {
                    foreach (IWidget child in container.Children) { Visit(child); }
                }
            }
            Visit(_root);
            foreach (KeyValuePair<string, ILiveTarget> additional in _additionalTargets) {
                if (additional.Value is IWidget widget) { Visit(widget); }
                else { next[additional.Key] = additional.Value; }
            }
            foreach (RegionWidget stale in _updateContent.Keys.Where(slot => !regions.Contains(slot)).ToArray()) { _updateContent.Remove(stale); }
            _updateTargets = next;
        }
    }
}
