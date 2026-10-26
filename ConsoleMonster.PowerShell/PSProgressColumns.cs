using System;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>Adapts builtin names, column records and typed columns to the shared progress-column model.</summary>
    internal static class PSProgressColumns {
        public static IList<ProgressColumn>? Build(object[]? values) {
            if (values == null) { return null; }
            var result = new List<ProgressColumn>();
            foreach (object? value in values) {
                object? raw = PSText.Unwrap(value);
                if (raw is ProgressColumn column) { result.Add(column); continue; }
                if (raw is string name) { result.AddRange(ProgressColumns.FromNames(new[] { name })); continue; }
                if (!(raw is IDictionary) && !(value is PSObject)) { throw new PSArgumentException("A progress column needs a builtin name, a Type/Alignment/Priority/MaxWidth record or a typed ProgressColumn."); }
                object record = raw is IDictionary ? raw : value!;
                PSItems.RecordFields(record, "Type Alignment Priority MaxWidth");
                string type = PSText.PlainText(PSItems.Field(record, "Type"));
                if (string.IsNullOrWhiteSpace(type)) { throw new PSArgumentException("A progress column record requires Type."); }
                IList<ProgressColumn> columns = ProgressColumns.FromNames(new[] { type });
                object? alignment = PSItems.Field(record, "Alignment");
                object? priority = PSItems.Field(record, "Priority");
                object? maxWidth = PSItems.Field(record, "MaxWidth");
                HorizontalAlignment? aligned = alignment == null ? (HorizontalAlignment?)null : LanguagePrimitives.ConvertTo<HorizontalAlignment>(alignment);
                int? rank = priority == null ? (int?)null : LanguagePrimitives.ConvertTo<int>(priority);
                int? width = maxWidth == null ? (int?)null : LanguagePrimitives.ConvertTo<int>(maxWidth);
                if (aligned.HasValue && !Enum.IsDefined(typeof(HorizontalAlignment), aligned.Value)) { throw new PSArgumentException("Alignment must be Left, Center or Right."); }
                if (width < 0) { throw new PSArgumentException("MaxWidth must be zero or a positive cell count."); }
                foreach (ProgressColumn item in columns) {
                    if (aligned.HasValue) { item.Alignment = aligned.Value; }
                    if (rank.HasValue) { item.Priority = rank.Value; }
                    if (width.HasValue) { item.MaxWidth = width.Value; }
                    result.Add(item);
                }
            }
            return result;
        }
    }
}
