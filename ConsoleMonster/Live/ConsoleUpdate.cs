using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Live {
    /// <summary>Validates a complete high-level update before publishing any of its existing protocol events.</summary>
    public static partial class ConsoleUpdate {
        /// <summary>Applies a validated update to an addressed session target.</summary>
        /// <param name="session">The session which owns the widget.</param>
        /// <param name="widgetId">The registered widget identifier.</param>
        /// <param name="update">Only explicitly set fields participate in the update.</param>
        public static void Apply(LiveSession session, string widgetId, WidgetUpdate update) {
            if (session == null) { throw new ArgumentNullException(nameof(session)); }
            if (update == null) { throw new ArgumentNullException(nameof(update)); }
            session.ApplyUpdate(widgetId, update);
        }

        /// <summary>Updates one caller-managed run item through the same operation used by PowerShell.</summary>
        /// <param name="run">The caller-managed run.</param>
        /// <param name="update">The item position, state, label and detail.</param>
        public static void Apply(LiveRun run, RunUpdate update) {
            if (run == null) { throw new ArgumentNullException(nameof(run)); }
            if (update == null) { throw new ArgumentNullException(nameof(update)); }
            if (update.Index < 0 || update.Index >= run.Snapshot().Total) { throw new ArgumentOutOfRangeException(nameof(update.Index)); }
            if (update.Duration < TimeSpan.Zero) { throw new ArgumentOutOfRangeException(nameof(update.Duration)); }
            if (update.Status != StatusKind.Running && update.Status != StatusKind.Pass && update.Status != StatusKind.Fail && update.Status != StatusKind.Warn && update.Status != StatusKind.Skip) {
                throw new ArgumentException("A run state must be Running, Pass, Fail, Warn or Skip.", nameof(update.Status));
            }
            if (update.Status == StatusKind.Running) { run.Started(update.Index, update.Label); }
            else { run.Finished(update.Index, update.Status, update.Detail, update.Duration); }
        }

        // Event creation is separate from publication so every field and payload is checked before the first enqueue.
        internal static IReadOnlyList<LiveEvent> Events(ILiveTarget target, WidgetUpdate update, int? cellCount = null) {
            string allowed = Allowed(target);
            var fields = new HashSet<string>(allowed.Split(' '), StringComparer.Ordinal);
            foreach (string field in update.Bound) {
                if (!fields.Contains(field)) {
                    throw new ConsoleUpdateException("ConsoleMonsterUpdateParameterNotSupported", target.GetType().Name + " does not support " + field + ". Supported fields: " + allowed + ".");
                }
            }
            if (!update.Bound.Any()) { throw new ArgumentException("Specify at least one update field.", nameof(update)); }
            Validate(target, update, cellCount);
            return Build(target, update);
        }

        private static string Allowed(ILiveTarget target) {
            if (target is ActivityList) { return "Key ParentKey Label Detail Value Total Remaining Complete"; }
            if (target is CellMap) { return "Index Status AppendCells Count CellCount"; }
            if (target is Lanes) { return "Key Label Detail Total Status Result Increment Complete"; }
            if (target is LiveTable) { return "Row Key RemoveRow ClearRows"; }
            if (target is Phases) { return "Key Status Detail"; }
            if (target is ProgressBar) { return "Value Total Increment Label Status"; }
            if (target is Spinner) { return "Label Detail Restart Status"; }
            if (target is StatusList) { return "Key Label Detail Status"; }
            if (target is SummaryBar) { return "Value Total Counter Status"; }
            if (target is KeyValueList) { return "Key Value Status"; }
            if (target is BarChart || target is StackedBar) { return "Key Value Increment"; }
            if (target is LineChart || target is Sparkline || target is Histogram) { return "Append Key Value"; }
            if (target is RegionWidget) { return "Content"; }
            if (target is Flow) { return "NodeKey EdgeKey Status Cut"; }
            throw new ConsoleUpdateException("ConsoleMonsterUnsupportedUpdateTarget", "Use the raw event API for custom target " + target.GetType().FullName + ".");
        }

        private static void Validate(ILiveTarget target, WidgetUpdate u, int? cellCount) {
            if (u.Has(nameof(u.Value)) && u.Has(nameof(u.Increment))) { throw new ArgumentException("Value and Increment cannot be combined."); }
            if (u.Has(nameof(u.Status)) && !Enum.IsDefined(typeof(StatusKind), u.Status)) { throw new ArgumentOutOfRangeException(nameof(u.Status)); }
            if (u.Has(nameof(u.Result)) && !Enum.IsDefined(typeof(StatusKind), u.Result)) { throw new ArgumentOutOfRangeException(nameof(u.Result)); }
            if (u.Has(nameof(u.Status)) && u.Has(nameof(u.Result))) { throw new ArgumentException("Status and Result cannot be combined."); }
            if (target is ActivityList || target is Lanes || target is Phases || target is StatusList || target is KeyValueList || target is BarChart || target is StackedBar) { RequireKey(u.Key); }
            if (u.Has(nameof(u.Total))) { Nonnegative(u.Total, nameof(u.Total)); }
            if (u.Has(nameof(u.Increment))) { Number(u.Increment, nameof(u.Increment)); }
            if (u.Has(nameof(u.Value)) && !(target is KeyValueList)) { Number(Numeric(u.Value), nameof(u.Value)); }
            if (u.Remaining < TimeSpan.Zero) { throw new ArgumentOutOfRangeException(nameof(u.Remaining)); }
            if (target is Flow flow) {
                bool node = u.Has(nameof(u.NodeKey)), edge = u.Has(nameof(u.EdgeKey));
                if (node == edge || !u.Has(nameof(u.Status))) { throw new ArgumentException("A flow update requires exactly one NodeKey or EdgeKey, and Status."); }
                if (node && u.Has(nameof(u.Cut))) { throw new ArgumentException("Cut requires EdgeKey."); }
                string? key = node ? u.NodeKey : u.EdgeKey;
                RequireKey(key);
                if (node ? !flow.Nodes.Any(item => item.Key == key) : !flow.Edges.Any(item => item.Key == key)) {
                    throw new ConsoleUpdateException("ConsoleMonsterUnknownItem", "Unknown flow " + (node ? "NodeKey" : "EdgeKey") + " '" + key + "'.");
                }
            }
            if (target is ActivityList) {
                if (!int.TryParse(u.Key, NumberStyles.None, CultureInfo.InvariantCulture, out _)) { throw new ArgumentException("An activity Key must be a nonnegative integer."); }
                if (u.Has(nameof(u.ParentKey)) && !int.TryParse(u.ParentKey, NumberStyles.None, CultureInfo.InvariantCulture, out _)) { throw new ArgumentException("ParentKey must be a nonnegative integer."); }
                if (u.Has(nameof(u.Total)) && u.Total != 100) { throw new ArgumentException("Activity percentages use Total 100."); }
                if (u.Has(nameof(u.Value)) && (Numeric(u.Value) < 0 || Numeric(u.Value) > 100)) { throw new ArgumentOutOfRangeException(nameof(u.Value)); }
            }
            if (target is Lanes && u.Has(nameof(u.Increment)) && !u.Has(nameof(u.Result))) { throw new ArgumentException("A lane Increment requires Result."); }
            if (target is Lanes && u.Has(nameof(u.Increment))) { Nonnegative(u.Increment, nameof(u.Increment)); }
            if (target is CellMap) {
                int modes = (u.Has(nameof(u.Index)) ? 1 : 0) + (u.AppendCells ? 1 : 0) + (u.Has(nameof(u.CellCount)) ? 1 : 0);
                if (modes != 1) { throw new ArgumentException("Choose Index, AppendCells or CellCount."); }
                if (u.Has(nameof(u.CellCount)) && u.Has(nameof(u.Status))) { throw new ArgumentException("Status applies to indexed or appended cells, not CellCount."); }
                if (u.Has(nameof(u.Index)) && (u.Index == null || u.Index.Length == 0 || u.Index.Any(x => x < 0 || x >= CellMap.MaxItems) || !u.Has(nameof(u.Status)))) { throw new ArgumentException("Index requires positions below CellMap.MaxItems and Status."); }
                if (u.Has(nameof(u.Count)) && !u.AppendCells) { throw new ArgumentException("Count requires AppendCells."); }
                if (u.AppendCells && (u.Count < 1 || u.Count > CellMap.MaxItems)) { throw new ArgumentOutOfRangeException(nameof(u.Count)); }
                if (u.AppendCells && u.Count > CellMap.MaxItems - (cellCount ?? ((CellMap)target).Count)) { throw new ArgumentOutOfRangeException(nameof(u.Count), "The appended cells exceed CellMap.MaxItems."); }
                if (u.Has(nameof(u.CellCount)) && (u.CellCount < 1 || u.CellCount > CellMap.MaxItems)) { throw new ArgumentOutOfRangeException(nameof(u.CellCount)); }
            }
            if (target is LiveTable) {
                if ((u.Has(nameof(u.Row)) ? 1 : 0) + (u.RemoveRow ? 1 : 0) + (u.ClearRows ? 1 : 0) != 1) { throw new ArgumentException("Choose Row, RemoveRow or ClearRows."); }
                if (u.RemoveRow) { RequireKey(u.Key); }
                if (u.Has(nameof(u.Row))) {
                    if (u.Row == null || u.Row.Values == null) { throw new ArgumentException("Row requires Key and Values."); }
                    RequireKey(u.Row.Key);
                }
                if (!u.RemoveRow && u.Has(nameof(u.Key))) { throw new ArgumentException("Put the upsert key in Row.Key; Key is for RemoveRow."); }
            }
            if (target is LineChart || target is Sparkline || target is Histogram) {
                if (!u.Append || !u.Has(nameof(u.Value))) { throw new ArgumentException("A sample requires Append and Value."); }
            }
            if (target is SummaryBar && u.Has(nameof(u.Status)) && (u.Counter == null || u.Counter.Count == 0)) { throw new ArgumentException("Status requires at least one Counter."); }
            if (target is KeyValueList && !u.Has(nameof(u.Value))) { throw new ArgumentException("A key/value update requires Value."); }
            if (u.Counter != null) {
                foreach (KeyValuePair<string, int> counter in u.Counter) {
                    RequireKey(counter.Key);
                    if (counter.Value < 0) { throw new ArgumentOutOfRangeException(nameof(u.Counter)); }
                }
            }
        }

        private static void RequireKey(string? key) { if (string.IsNullOrWhiteSpace(key)) { throw new ArgumentException("A nonempty Key is required."); } }
        private static void Number(double value, string name) { if (double.IsNaN(value) || double.IsInfinity(value)) { throw new ArgumentOutOfRangeException(name, "A finite number is required."); } }
        private static void Nonnegative(double value, string name) { Number(value, name); if (value < 0) { throw new ArgumentOutOfRangeException(name); } }
        private static double Numeric(object? value) {
            if (value == null || value is bool || value is DateTime) { throw new ArgumentException("A numeric Value is required."); }
            try { return Convert.ToDouble(value, CultureInfo.InvariantCulture); }
            catch (Exception error) when (error is FormatException || error is InvalidCastException || error is OverflowException) {
                throw new ArgumentException("A finite numeric Value is required.", nameof(value), error);
            }
        }
    }

    /// <summary>An actionable high-level update error, shared with the PowerShell error identifier.</summary>
    public sealed class ConsoleUpdateException : ArgumentException {
        /// <summary>Creates an update error with a stable error identifier.</summary>
        public ConsoleUpdateException(string errorId, string message) : base(message) { ErrorId = errorId; }
        /// <summary>The error identifier used by hosts.</summary>
        public string ErrorId { get; }
    }
}
