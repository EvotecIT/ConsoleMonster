using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using ConsoleMonster.Data;
using ConsoleMonster.Values;

namespace ConsoleMonster.PowerShell.Binding {
    /// <summary>
    /// Picks the columns for objects when none are given, the same way for every command:
    /// <list type="number">
    /// <item>a view registered for the type (Register-ConsoleView, or ConsoleMonster's own views for files and folders,
    /// processes and services);</item>
    /// <item>for data values (dictionaries, data rows, XML elements, JSON objects), their keys or columns;</item>
    /// <item>PowerShell's table view for the type (format.ps1xml, as Format-Table uses);</item>
    /// <item>the default display property set;</item>
    /// <item>the object's fields without plumbing (provider paths, CIM metadata, collection synchronisation), at most
    /// <see cref="ShapeOptions.MaxFields"/>.</item>
    /// </list>
    /// Values (scalars and collections) get one Value column.
    /// </summary>
    internal static class ColumnResolver {
        private const int SampleSize = 50;

        /// <summary>The columns for a run of objects of one kind.</summary>
        /// <param name="items">The objects.</param>
        /// <param name="cmdlet">The calling cmdlet (for PowerShell's table views), or null.</param>
        /// <param name="options">Shape options.</param>
        /// <param name="view">The registered view that supplied the columns, or null.</param>
        /// <returns>Property names and calculated-property hashtables.</returns>
        internal static object[] Resolve(IReadOnlyList<PSObject> items, PSCmdlet? cmdlet, ShapeOptions options, out ConsoleViewRegistration? view) {
            view = null;
            List<PSObject> present = items.Where(item => item != null).ToList();
            if (present.Count == 0) {
                return Array.Empty<object>();
            }
            bool anyValue = present.Any(item => PSShaping.IsValueLike(item, options));
            if (anyValue && present.All(item => PSShaping.IsValueLike(item, options))) {
                return new object[] { "Value" };
            }
            PSObject first = present.First(item => !PSShaping.IsValueLike(item, options));
            List<object> columns = new List<object>();
            if (anyValue) {
                columns.Add("Value");
            }
            view = ConsoleFormatting.Find(first);
            if (view?.Property != null && view.Property.Length > 0) {
                columns.AddRange(view.Property);
                return columns.ToArray();
            }
            object? prepared = ValueShaper.Prepare(first, options);
            if (prepared != null && !(prepared is PSObject) && ValueShaper.IsStructural(prepared)) {
                columns.AddRange(FieldNames(present, options, Math.Max(options.MaxFields, 30)));
                return columns.ToArray();
            }
            object[]? format = FormatViews.TableColumns(first, cmdlet);
            if (format != null && format.Length > 0) {
                columns.AddRange(format);
                return columns.ToArray();
            }
            PSPropertySet? display = PropertyBinder.GetDefaultDisplayForSource(first);
            if (display != null && display.ReferencedPropertyNames.Count > 0) {
                foreach (string name in display.ReferencedPropertyNames) {
                    if (!columns.OfType<string>().Contains(name, StringComparer.OrdinalIgnoreCase)) {
                        columns.Add(name);
                    }
                }
                return columns.ToArray();
            }
            columns.AddRange(FieldNames(present, options, options.MaxFields));
            return columns.ToArray();
        }

        // The union of the fields of up to SampleSize objects, in order of first appearance, plumbing left out.
        private static IEnumerable<string> FieldNames(IReadOnlyList<PSObject> items, ShapeOptions options, int max) {
            List<string> names = new List<string>();
            int sampled = 0;
            foreach (PSObject item in items) {
                if (sampled++ >= SampleSize) {
                    break;
                }
                object? prepared = ValueShaper.Prepare(item, options);
                if (prepared == null || ValueShaper.IsScalar(prepared)) {
                    continue;
                }
                foreach (InspectMember field in ValueShaper.Fields(prepared, options)) {
                    if (!names.Contains(field.Name, StringComparer.OrdinalIgnoreCase)) {
                        names.Add(field.Name);
                    }
                }
            }
            return names.Take(Math.Max(1, max));
        }

        /// <summary>A key that tells runs apart: consecutive objects with the same key share one table (Format-Table splits on type changes too).</summary>
        /// <param name="item">The object.</param>
        /// <param name="options">Shape options.</param>
        /// <returns>The key.</returns>
        internal static string RunKey(PSObject? item, ShapeOptions options) {
            if (item == null || PSShaping.IsValueLike(item, options)) {
                return "value";
            }
            ConsoleViewRegistration? view = ConsoleFormatting.Find(item);
            if (view != null) {
                return "view:" + view.Group;
            }
            object? prepared = ValueShaper.Prepare(item, options);
            if (prepared is System.Collections.IDictionary) {
                return "dictionary";
            }
            if (prepared != null && !(prepared is PSObject) && ValueShaper.IsStructural(prepared)) {
                return "data:" + ValueShaper.TypeName(prepared, options);
            }
            return item.TypeNames.Count > 0 ? item.TypeNames[0] : item.BaseObject?.GetType().FullName ?? "object";
        }
    }
}
