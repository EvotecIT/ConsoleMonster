using System;
using System.Collections.Generic;
using System.Globalization;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Live {
    public static partial class ConsoleUpdate {
        private static IReadOnlyList<LiveEvent> Build(ILiveTarget target, WidgetUpdate u) {
            var events = new List<LiveEvent>();
            string id = target.WidgetId;
            void Add(string operation, string? key = null, string? text = null, string? status = null, double value = 0, double total = 0, object? payload = null) =>
                events.Add(new LiveEvent(id, operation, key, text, status, value, total) { Payload = payload });
            if (target is Dashboard) {
                if (u.Has(nameof(u.Label))) { Add("title", text: u.Label); }
                if (u.Has(nameof(u.Detail))) { Add("detail", text: u.Detail); }
                if (u.Has(nameof(u.Status))) { Add("state", status: u.Status.ToString()); }
            } else if (target is ActivityList) {
                if (u.Complete) { Add("complete", u.Key); }
                else {
                    if (u.Has(nameof(u.Label))) { Add("label", u.Key, u.Label); }
                    if (u.Has(nameof(u.Detail))) { Add("detail", u.Key, u.Detail); }
                    if (u.Has(nameof(u.Value))) { Add("value", u.Key, value: Numeric(u.Value)); }
                    if (u.Has(nameof(u.Remaining))) { Add("remaining", u.Key, value: u.Remaining?.TotalSeconds ?? -1); }
                    if (u.Has(nameof(u.ParentKey))) { Add("parent", u.Key, value: int.Parse(u.ParentKey!, CultureInfo.InvariantCulture)); }
                }
            } else if (target is CellMap) {
                if (u.Has(nameof(u.CellCount))) { Add("count", value: u.CellCount); }
                else if (u.AppendCells) {
                    for (int i = 0; i < u.Count; i++) { Add("add", status: u.Status.ToString()); }
                } else {
                    foreach (int index in u.Index!) { Add("set", index.ToString(CultureInfo.InvariantCulture), status: u.Status.ToString()); }
                }
            } else if (target is Lanes) {
                if (u.Has(nameof(u.Label))) { Add("label", u.Key, u.Label); }
                if (u.Has(nameof(u.Total))) { Add("total", u.Key, total: u.Total); }
                if (u.Has(nameof(u.Status)) && !u.Complete) { Add("state", u.Key, u.Detail, u.Status.ToString()); }
                else if (u.Has(nameof(u.Result))) {
                    double steps = u.Has(nameof(u.Increment)) ? u.Increment : 1;
                    if (steps != 0) { Add("advance", u.Key, u.Detail, u.Result.ToString(), steps); }
                }
                if (u.Complete) { Add("complete", u.Key, u.Detail, u.Has(nameof(u.Status)) ? u.Status.ToString() : null); }
                if (u.Has(nameof(u.Detail))) { Add("detail", u.Key, u.Detail); }
            } else if (target is LiveTable) {
                if (u.ClearRows) { Add("clear"); }
                else if (u.RemoveRow) { Add("remove", u.Key); }
                else {
                    object?[] values = (object?[])u.Row!.Values.Clone();
                    Add("update", u.Row.Key, LiveTable.TextOf(values), payload: values);
                }
            } else if (target is Phases && u.Has(nameof(u.ParentKey))) {
                string item = u.ParentKey + Phases.ItemSeparator + u.Key;
                if (u.Has(nameof(u.Status)) || u.Has(nameof(u.Label))) { Add("item", item, u.Label, u.Has(nameof(u.Status)) ? u.Status.ToString() : null); }
                if (u.Has(nameof(u.Detail))) { Add("item-detail", item, u.Detail); }
            } else if (target is Phases) {
                if (u.Has(nameof(u.Status))) { Add("state", u.Key, u.Detail, u.Status.ToString()); }
                if (u.Has(nameof(u.Detail))) { Add("detail", u.Key, u.Detail); }
            } else if (target is ProgressBar) {
                if (u.Has(nameof(u.Value))) { Add("set", value: Numeric(u.Value), total: u.Has(nameof(u.Total)) ? u.Total : 0); }
                // A distinct total operation preserves a queued value, including for unknown totals (zero).
                if (u.Has(nameof(u.Total)) && (!u.Has(nameof(u.Value)) || u.Total == 0)) { Add("total", total: u.Total); }
                if (u.Has(nameof(u.Increment)) && u.Increment != 0) { Add("advance", value: u.Increment); }
                if (u.Has(nameof(u.Label))) { Add("label", text: u.Label); }
                if (u.Has(nameof(u.Status))) { Add("state", status: u.Status.ToString()); }
            } else if (target is Spinner) {
                if (u.Restart) { Add("start"); }
                if (u.Has(nameof(u.Label))) { Add("label", text: u.Label); }
                if (u.Has(nameof(u.Status))) { Add("state", text: u.Detail, status: u.Status.ToString()); }
                if (u.Has(nameof(u.Detail))) { Add("detail", text: u.Detail); }
            } else if (target is StatusList) {
                if (u.Has(nameof(u.Status))) { Add("set", u.Key, status: u.Status.ToString()); }
                if (u.Has(nameof(u.Label))) { Add("label", u.Key, u.Label); }
                if (u.Has(nameof(u.Detail))) { Add("detail", u.Key, u.Detail); }
            } else if (target is SummaryBar) {
                if (u.Has(nameof(u.Value)) || u.Has(nameof(u.Total))) {
                    Add("progress", value: u.Has(nameof(u.Value)) ? Numeric(u.Value) : SummaryBar.Keep, total: u.Has(nameof(u.Total)) ? u.Total : SummaryBar.Keep);
                }
                if (u.Counter != null) {
                    foreach (KeyValuePair<string, int> counter in u.Counter) {
                        Add("counter", counter.Key, status: u.Has(nameof(u.Status)) ? u.Status.ToString() : StatusKinds.TryParse(counter.Key, out StatusKind status) ? status.ToString() : null, value: counter.Value);
                    }
                }
            } else if (target is KeyValueList) {
                if (u.Has(nameof(u.Value))) { Add("set", u.Key, u.Value is RichText rich ? rich.ToMarkup() : Markup.Escape(Convert.ToString(u.Value, CultureInfo.InvariantCulture)), u.Has(nameof(u.Status)) ? u.Status.ToString() : null); }
            } else if (target is BarChart || target is StackedBar) {
                if (u.Has(nameof(u.Value))) { Add("set", u.Key, value: Numeric(u.Value)); }
                else if (u.Has(nameof(u.Increment)) && u.Increment != 0) { Add("add", u.Key, value: u.Increment); }
            } else if (target is LineChart || target is Sparkline || target is Histogram) {
                Add("add", u.Key, value: Numeric(u.Value));
            } else if (target is RegionWidget) {
                Add(u.Content == null ? "clear" : "set", payload: u.Content);
            } else if (target is Flow) {
                Add(u.Has(nameof(u.NodeKey)) ? "node" : u.Has(nameof(u.Cut)) ? u.Cut ? "cut" : "connect" : "edge",
                    u.NodeKey ?? u.EdgeKey, status: u.Status.ToString());
            }
            return events;
        }
    }
}
