using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Live {
    public sealed partial class LiveSession {
        // Validation follows accepted topology, which can be ahead of the render thread's adopted targets.
        // Topology, cell-capacity operations and high-level batches share this gate; other raw events remain lock-free.
        private readonly object _updateGate = new object();
        private Dictionary<string, ILiveTarget> _updateTargets = new Dictionary<string, ILiveTarget>(StringComparer.Ordinal);
        private readonly Dictionary<RegionWidget, IWidget?> _updateContent = new Dictionary<RegionWidget, IWidget?>();
        private readonly Dictionary<CellMap, int> _updateCellCounts = new Dictionary<CellMap, int>();
        private readonly ConditionalWeakTable<Phases, HashSet<string>> _updatePhaseKeys = new ConditionalWeakTable<Phases, HashSet<string>>();
        private readonly Dictionary<string, ILiveTarget> _additionalTargets = new Dictionary<string, ILiveTarget>(StringComparer.Ordinal);
        private bool _discovered;

        internal void ApplyUpdate(string widgetId, WidgetUpdate update) {
            lock (_updateGate) {
                if (!_updateTargets.TryGetValue(widgetId, out ILiveTarget? target)) {
                    throw new ConsoleUpdateException("ConsoleMonsterUnknownWidget", "Unknown WidgetId '" + widgetId + "'. Available targets: " + string.Join(", ", _updateTargets.Keys.OrderBy(x => x, StringComparer.Ordinal)) + ".");
                }
                int? count = target is CellMap cells ? _updateCellCounts.TryGetValue(cells, out int projected) ? projected : cells.Count : (int?)null;
                IReadOnlyList<LiveEvent> events = ConsoleUpdate.Events(target, update, count, target is Phases phases ? PhaseKeys(phases) : null);
                foreach (LiveEvent item in events) { Post(item); }
            }
        }

        private bool TryGetUpdateTarget(string widgetId, out ILiveTarget? target) {
            lock (_updateGate) { return _updateTargets.TryGetValue(widgetId, out target); }
        }

        // Phase creation, like cell capacity, follows admitted queue order rather than rendered state.
        private HashSet<string> PhaseKeys(Phases phases) {
            return _updatePhaseKeys.GetValue(phases, widget => new HashSet<string>(widget.Items.Select(phase => phase.Key), StringComparer.Ordinal));
        }

        private void ProjectPhaseKey(LiveEvent item) {
            if ((item.Operation == "add" || item.Operation == "state") && item.Key != null
                && _updateTargets.TryGetValue(item.WidgetId, out ILiveTarget? target) && target is Phases phases) {
                PhaseKeys(phases).Add(item.Key);
            }
        }

        // The accepted queue, rather than the render thread's current state, determines available cell capacity.
        private void ProjectCellCount(LiveEvent item) {
            if (!_updateTargets.TryGetValue(item.WidgetId, out ILiveTarget? target) || !(target is CellMap cells)) { return; }
            int count = _updateCellCounts.TryGetValue(cells, out int projected) ? projected : cells.Count;
            if (item.Operation == "count" && !double.IsNaN(item.Value) && item.Value > 0) { count = (int)Math.Min(CellMap.MaxItems, item.Value); }
            else if (item.Operation == "add" && count < CellMap.MaxItems) { count++; }
            else if (item.Operation == "set" && int.TryParse(item.Key, NumberStyles.Integer, CultureInfo.InvariantCulture, out int index) && index >= 0 && index < CellMap.MaxItems) { count = Math.Max(count, index + 1); }
            _updateCellCounts[cells] = count;
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
            var cells = new HashSet<CellMap>(next.Values.OfType<CellMap>());
            foreach (CellMap stale in _updateCellCounts.Keys.Where(item => !cells.Contains(item)).ToArray()) { _updateCellCounts.Remove(stale); }
            // Reattaching the same widget retains admitted keys; weak keys do not keep removed widgets alive.
            foreach (Phases current in next.Values.OfType<Phases>()) { PhaseKeys(current); }
        }
    }
}
