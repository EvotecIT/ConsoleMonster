using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Management.Automation;
using ConsoleMonster.Text;
using ConsoleMonster.Values;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    internal static partial class EverydayDataView {
        private const int ExpansionDepth = 2;
        private const int InlineItemLimit = 6;
        private const int NestedRowLimit = 5;

        // These are display budgets, never mutations or enumeration limits on the pipeline objects.
        private sealed class ExpansionBudget {
            internal int Containers = 128;
            internal int Rows = 24;
        }

        private sealed class DataRecord {
            internal readonly List<KeyValuePair<string, object?>> Fields = new List<KeyValuePair<string, object?>>();
            internal int Count;
            internal object Source = null!;
            internal bool StoredOnly;

            // Column selection belongs to the whole table, not each row's property order. Keep all names available
            // without reading discarded values, and resolve a selected field even when it occurs later in this row.
            internal IEnumerable<string> Names() {
                if (Unwrap(Source) is IDictionary dictionary) {
                    foreach (object key in dictionary.Keys) { yield return Convert.ToString(key, CultureInfo.CurrentCulture) ?? string.Empty; }
                } else {
                    foreach (PSPropertyInfo property in ((PSObject)Source).Properties) {
                        if (Readable(property, StoredOnly)) { yield return property.Name; }
                    }
                }
            }

            internal object? GetValue(string name) {
                foreach (KeyValuePair<string, object?> field in Fields) {
                    if (string.Equals(field.Key, name, StringComparison.OrdinalIgnoreCase)) { return field.Value; }
                }
                if (Unwrap(Source) is IDictionary dictionary) {
                    foreach (DictionaryEntry entry in dictionary) {
                        if (string.Equals(Convert.ToString(entry.Key, CultureInfo.CurrentCulture), name, StringComparison.OrdinalIgnoreCase)) { return entry.Value; }
                    }
                    return null;
                }
                PSPropertyInfo? property = ((PSObject)Source).Properties[name];
                return property != null && Readable(property, StoredOnly) ? ReadValue(property) : null;
            }
        }

        private static object? Unwrap(object? value) => value is PSObject wrapped ? wrapped.BaseObject : value;
        private static bool IsRecord(object? value) => Unwrap(value) is PSCustomObject || Unwrap(value) is IDictionary;
        private static bool IsNested(object? value) => Unwrap(value) is object raw && !ValueShaper.IsScalar(raw);

        // Only stored JSON/data fields are visited below the root. Script properties and arbitrary .NET getters stay out.
        private static DataRecord ReadRecord(object value, bool nested = true) {
            DataRecord record = new DataRecord { Source = value as PSObject ?? PSObject.AsPSObject(value), StoredOnly = nested };
            if (Unwrap(value) is IDictionary dictionary) {
                record.Count = dictionary.Count;
                foreach (DictionaryEntry entry in dictionary) {
                    if (record.Fields.Count == ColumnLimit) { break; }
                    record.Fields.Add(new KeyValuePair<string, object?>(Convert.ToString(entry.Key, CultureInfo.CurrentCulture) ?? string.Empty, entry.Value));
                }
            } else {
                PSObject wrapper = value as PSObject ?? PSObject.AsPSObject(value);
                foreach (PSPropertyInfo property in wrapper.Properties) {
                    if (!Readable(property, nested)) { continue; }
                    record.Count++;
                    if (record.Fields.Count >= ColumnLimit) { continue; }
                    record.Fields.Add(new KeyValuePair<string, object?>(property.Name, ReadValue(property)));
                }
            }
            return record;
        }

        private static bool Readable(PSPropertyInfo property, bool storedOnly) => property.IsGettable && (!storedOnly || property.MemberType == PSMemberTypes.NoteProperty);

        private static object? ReadValue(PSPropertyInfo property) {
            try { return property.Value; }
            catch (Exception ex) when (!(ex is PipelineStoppedException) && !(ex is OperationCanceledException)) { return "<unavailable>"; }
        }

        private static bool IsRecordArray(object? value) {
            if (!(Unwrap(value) is IList list) || list.Count == 0) { return false; }
            for (int i = 0; i < Math.Min(list.Count, NestedRowLimit); i++) {
                if (!IsRecord(list[i])) { return false; }
            }
            return true;
        }

        private static RecordView BuildRecord(DataRecord record, int depth, ExpansionBudget budget, bool title) {
            int key = title ? ValueShaper.TitleIndex(record.Fields.Select(field => field.Key).ToList()) : -1;
            string? heading = null;
            if (key >= 0 && !IsNested(record.Fields[key].Value)) { heading = Placeholders.Resolve(InlineText(record.Fields[key].Value), Capabilities.GlyphTier.Full); }
            else { key = -1; }
            RecordView view = new RecordView(heading == null ? null : Markup.Escape(heading)) { HideEmpty = false };
            List<KeyValuePair<string, object?>> branches = new List<KeyValuePair<string, object?>>();
            for (int i = 0; i < record.Fields.Count; i++) {
                if (i == key) { continue; }
                KeyValuePair<string, object?> field = record.Fields[i];
                bool array = IsRecordArray(field.Value);
                bool nestedBranch = IsRecord(field.Value) && depth < ExpansionDepth
                    && ReadRecord(field.Value!).Fields.Any(child => IsRecordArray(child.Value));
                if (depth < ExpansionDepth && budget.Rows > 0 && (array || nestedBranch)) {
                    branches.Add(field);
                } else {
                    view.AddFieldAt(0, BoundedText(field.Key, 48), ToCell(FormatValue(field.Value, depth + 1, budget)));
                }
            }
            foreach (KeyValuePair<string, object?> branch in branches) {
                view.AddHeading(Markup.Escape(BoundedText(branch.Key, 48)), 0);
                if (Unwrap(branch.Value) is IList list) {
                    int count = Math.Min(Math.Min(list.Count, NestedRowLimit), budget.Rows);
                    budget.Rows -= count;
                    List<DataRecord> rows = new List<DataRecord>();
                    for (int i = 0; i < count; i++) { rows.Add(ReadRecord(list[i]!)); }
                    if (count > 0) { view.AddWidgetAt(1, BuildTable(rows, list.Count - count, depth + 1, budget, true)); }
                    else { view.AddFieldAt(1, "items", TableCell.FromValue(ItemCount(list.Count))); }
                } else {
                    view.AddWidgetAt(1, BuildRecord(ReadRecord(branch.Value!), depth + 1, budget, false));
                }
            }
            if (record.Count > ColumnLimit) { view.Summary = More(record.Count - ColumnLimit, "fields"); }
            if (record.Count == 0) { view.AddFieldAt(0, "fields", TableCell.FromValue("(none)")); }
            return view;
        }

        // A cell value: ConsoleMonster's placeholders (missing, cut, nested) are drawn for the glyph tier of the output.
        private static object? FormatValue(object? value, int depth, ExpansionBudget budget) {
            object? shaped = ShapeValue(value, depth, budget);
            return shaped is string text ? Placeholders.Cell(text) : shaped;
        }

        private static TableCell ToCell(object? value) => value as TableCell ?? TableCell.FromValue(value);

        private static object? ShapeValue(object? value, int depth, ExpansionBudget budget) {
            object? raw = Unwrap(value);
            if (raw == null) { return Placeholders.Missing; }
            if (raw is string scalar) { return BoundedText(scalar, 160, Placeholders.Ellipsis); }
            if (ValueShaper.IsScalar(raw)) { return raw; }
            if (IsRecord(value)) {
                DataRecord record = ReadRecord(value!);
                if (record.Count == 0) { return "(none)"; }
                if (depth > ExpansionDepth || record.Count > InlineItemLimit || budget.Containers-- <= 0) {
                    return "{" + Placeholders.Ellipsis + "} " + record.Count.ToString("N0", CultureInfo.CurrentCulture) + (record.Count == 1 ? " key" : " keys");
                }
                string text = string.Join(", ", record.Fields.Select(field => BoundedText(field.Key, 48, Placeholders.Ellipsis) + "=" + InlineText(ShapeValue(field.Value, depth + 1, budget))));
                return BoundedText(depth > 1 ? "{" + text + "}" : text, 160, Placeholders.Ellipsis);
            }
            if (raw is IList list) {
                if (list.Count == 0) { return "(none)"; }
                if (depth > ExpansionDepth || budget.Containers-- <= 0) { return ItemCount(list.Count); }
                int count = Math.Min(list.Count, InlineItemLimit);
                List<string> values = new List<string>();
                for (int i = 0; i < count; i++) {
                    values.Add(TextWrapper.Truncate(InlineText(ShapeValue(list[i], depth + 1, budget)), 48, Placeholders.Ellipsis));
                }
                if (list.Count > count) { values.Add(More(list.Count - count, null)); }
                return string.Join(", ", values);
            }
            return raw is ICollection collection ? ItemCount(collection.Count) : "{" + Placeholders.Ellipsis + "}";
        }

        // A display limit applies to text as well as containers. Bound raw input before normalizing line breaks so a
        // very large response body cannot create an unbounded record/frame. Shared TextWrapper handles display cells.
        private static string BoundedText(string text, int cells = 160, string ellipsis = TextWrapper.Ellipsis) {
            const int characterLimit = 512;
            bool shortened = text.Length > characterLimit;
            int length = Math.Min(text.Length, characterLimit);
            if (length > 0 && char.IsHighSurrogate(text[length - 1])) { length--; }
            string line = text.Substring(0, length).Replace('\r', ' ').Replace('\n', ' ');
            return TextWrapper.Truncate(line + (shortened ? ellipsis : string.Empty), cells, ellipsis);
        }

        private static string InlineText(object? value) => value == null ? Placeholders.Missing : BoundedText(ValueShaper.ScalarText(value), 160, Placeholders.Ellipsis);
        private static string ItemCount(int count) => count.ToString("N0", CultureInfo.CurrentCulture) + (count == 1 ? " item" : " items");
        private static string More(int count, string? kind) {
            if (count == 1 && (kind == "rows" || kind == "fields")) { kind = kind.Substring(0, kind.Length - 1); }
            return "+" + count.ToString("N0", CultureInfo.CurrentCulture) + " more" + (kind == null ? string.Empty : " " + kind);
        }
    }
}
