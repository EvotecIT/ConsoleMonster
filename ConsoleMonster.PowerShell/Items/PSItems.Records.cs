using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Management.Automation;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    internal static partial class PSItems {
        private static StatusKind? RecordStatus(object item, string name = "Status") {
            object? value = Field(item, name);
            if (value == null) { return null; }
            if (StatusKinds.TryParse(PSWidgets.ToText(value), out StatusKind status)) { return status; }
            throw new PSArgumentException("'" + PSWidgets.ToText(value) + "' is not a status for " + name + ".");
        }

        internal static void RecordFields(object item, string names) => RecordFields(item, names, null);

        internal static void RecordFields(object item, string names, string? hint) {
            var allowed = new HashSet<string>(names.Split(' '), StringComparer.OrdinalIgnoreCase);
            IEnumerable<string> fields;
            if (item is IDictionary dictionary) {
                var keys = new List<string>();
                foreach (object key in dictionary.Keys) { keys.Add(PSWidgets.ToText(key)); }
                fields = keys;
            } else {
                var keys = new List<string>();
                foreach (PSPropertyInfo property in PSObject.AsPSObject(item).Properties) { if (property.IsGettable) { keys.Add(property.Name); } }
                fields = keys;
            }
            foreach (string name in fields) {
                if (!allowed.Contains(name)) { throw new PSArgumentException("Unknown item field '" + name + "'. Use " + names + "." + (hint == null ? string.Empty : " " + hint)); }
            }
        }

        private static double RecordNumber(object item, string name, double fallback = 0) {
            object? value = Field(item, name);
            if (value == null) { return fallback; }
            if (!TryNumber(value, out double number) || double.IsNaN(number) || double.IsInfinity(number)) { throw new PSArgumentException(name + " must be a finite number."); }
            return number;
        }

        private static string RecordLabel(object? value) => value is RichText rich ? rich.ToMarkup() : Text.Markup.Escape(PSText.PlainText(value));

        private static bool TryAddNodeRecord(TreeNode parent, object item) {
            if (item is TreeNode || !IsRecord(item) || Field(item, "Label") == null) { return false; }
            RecordFields(item, "Label Children Status Detail Value Presenter Unit");
            object label = Field(item, "Label")!;
            var node = new TreeNode(RecordLabel(label), RecordStatus(item)) { Detail = Field(item, "Detail") is object detail ? PSText.PlainText(detail) : null };
            object? value = Field(item, "Value");
            if (value != null) {
                node.Value = TableCell.FromValue(value);
                string? presenter = Field(item, "Presenter") as string;
                node.Presenter = string.IsNullOrEmpty(presenter) ? Presenters.CellPresenters.ForLabel(PSText.PlainText(label), value)
                    : Presenters.CellPresenters.FromName(presenter!, Field(item, "Unit") as string);
            }
            foreach (object child in Items(Field(item, "Children"))) {
                if (!TryAddNode(node, child)) { throw new PSArgumentException("A tree child needs Label, with optional Children, Status, Detail, Value, Presenter and Unit."); }
            }
            parent.Add(node);
            return true;
        }

        // Both reserved fields identify a descriptor; an ordinary property named Key is still ordinary data.
        internal static bool IsPairRecord(object item) {
            item = Item(item);
            if (!IsRecord(item)) { return false; }
            bool Has(string name) => item is IDictionary dictionary
                ? System.Linq.Enumerable.Any(System.Linq.Enumerable.Cast<object>(dictionary.Keys), key => string.Equals(PSWidgets.ToText(key), name, StringComparison.OrdinalIgnoreCase))
                : PSObject.AsPSObject(item).Properties[name] != null;
            return Has("Key") && Has("Value");
        }

        private static bool TryAddPairRecord(KeyValueList list, object item) {
            if (item is KeyValueItem || !IsPairRecord(item)) { return false; }
            RecordFields(item, "Key Value Status Markup Presenter Unit");
            string key = PSText.PlainText(Field(item, "Key"));
            object? value = Field(item, "Value");
            string? presenter = Field(item, "Presenter") as string;
            StatusKind? status = RecordStatus(item);
            if (!string.IsNullOrEmpty(presenter)) {
                list.Add(KeyValueItem.ForValue(key, value, Presenters.CellPresenters.FromName(presenter!, Field(item, "Unit") as string), status));
            } else if (value is RichText rich) { list.Add(new KeyValueItem(key, rich.ToMarkup(), status)); }
            else if (LanguagePrimitives.IsTrue(Field(item, "Markup"))) { list.Add(new KeyValueItem(key, PSText.PlainText(value), status)); }
            else { list.Add(new KeyValueItem(key, Text.Markup.Escape(PSText.PlainText(value)), status)); }
            return true;
        }

        private static bool TryAddLaneRecord(Lanes lanes, object item) {
            if (item is Lane || !IsRecord(item) || Field(item, "Key") == null) { return false; }
            RecordFields(item, "Key Label Total Value Status Detail Passed Failed Warned Skipped StartedSeconds FinishedSeconds");
            string key = PSText.PlainText(Field(item, "Key"));
            object? label = Field(item, "Label");
            var lane = new Lane(key, label == null ? key : PSText.PlainText(label)) {
                Total = RecordNumber(item, "Total"), Value = RecordNumber(item, "Value"), Status = RecordStatus(item) ?? StatusKind.Pending,
                Passed = LanguagePrimitives.ConvertTo<int>(Field(item, "Passed") ?? 0), Failed = LanguagePrimitives.ConvertTo<int>(Field(item, "Failed") ?? 0),
                Warned = LanguagePrimitives.ConvertTo<int>(Field(item, "Warned") ?? 0), Skipped = LanguagePrimitives.ConvertTo<int>(Field(item, "Skipped") ?? 0)
            };
            if (label is RichText rich) { lane.LabelText = rich; }
            if (Field(item, "Detail") is RichText styled) { lane.DetailText = styled; }
            else { lane.Detail = Field(item, "Detail") == null ? null : PSText.PlainText(Field(item, "Detail")); }
            if (Field(item, "StartedSeconds") != null) { lane.Started = TimeSpan.FromSeconds(RecordNumber(item, "StartedSeconds")); }
            if (Field(item, "FinishedSeconds") != null) { lane.Finished = TimeSpan.FromSeconds(RecordNumber(item, "FinishedSeconds")); }
            lanes.Add(lane);
            return true;
        }

        private static bool TryAddSeriesRecord(LineChart chart, object item) {
            if (item is ChartSeries || !IsRecord(item) || Field(item, "Key") == null) { return false; }
            RecordFields(item, "Key Label Values Role");
            string key = PSText.PlainText(Field(item, "Key"));
            object? role = Field(item, "Role");
            ThemeToken? token = role == null ? (ThemeToken?)null : LanguagePrimitives.ConvertTo<ThemeToken>(role);
            ChartSeries series = chart.AddSeries(key, token);
            if (Field(item, "Label") is object label) {
                if (label is RichText rich) { series.LabelText = rich; }
                series.Label = PSText.PlainText(label);
            }
            object? values = Field(item, "Values");
            if (values != null) { foreach (double value in LanguagePrimitives.ConvertTo<double[]>(values)) { chart.Add(key, value); } }
            return true;
        }

        private static bool TryAddLogRecord(LogRegion log, object item) {
            if (!IsRecord(item) || Field(item, "Message") == null) { return false; }
            RecordFields(item, "Message Level AtSeconds Tag");
            if (!LogLevels.TryNormalize(PSText.PlainText(Field(item, "Level") ?? "info"), out string level)) { throw new PSArgumentException("Level must be a recognized log level."); }
            TimeSpan time = TimeSpan.FromSeconds(RecordNumber(item, "AtSeconds"));
            object message = Field(item, "Message")!;
            string? tag = Field(item, "Tag") is object source ? PSText.PlainText(source) : null;
            if (message is RichText rich) { log.WriteRich(rich, level, time, tag); }
            else { log.WriteLiteral(PSText.PlainText(message), level, time, null, tag); }
            return true;
        }
    }
}
