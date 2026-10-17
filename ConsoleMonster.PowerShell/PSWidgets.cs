using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>Input/output mapping shared by the cmdlets: no rendering or layout logic lives here.</summary>
    internal static class PSWidgets {
        /// <summary>Runs a DSL scriptblock and collects the widgets it emits; strings become markup text blocks.</summary>
        public static List<IWidget> Collect(ScriptBlock? content) {
            List<IWidget> widgets = new List<IWidget>();
            if (content == null) {
                return widgets;
            }
            foreach (PSObject item in content.Invoke()) {
                AddItem(widgets, item);
            }
            return widgets;
        }

        /// <summary>Unwraps a pipeline value into a widget, or <c>null</c>.</summary>
        public static IWidget? AsWidget(object? value) {
            object? raw = value is PSObject ps ? ps.BaseObject : value;
            if (raw is IWidget widget) {
                return widget;
            }
            if (raw is string text) {
                return new TextBlock(text);
            }
            return null;
        }

        /// <summary>Converts any value to display text using PowerShell's conversion rules.</summary>
        public static string ToText(object? value) {
            if (value == null) {
                return string.Empty;
            }
            object raw = value is PSObject ps ? ps.BaseObject : value;
            if (raw is string s) {
                return s;
            }
            if (raw is System.Collections.IEnumerable enumerable && !(raw is System.Collections.IDictionary)) {
                List<string> parts = new List<string>();
                foreach (object? part in enumerable) {
                    parts.Add(ToText(part));
                }
                return string.Join(", ", parts.ToArray());
            }
            return LanguagePrimitives.ConvertTo<string>(raw) ?? string.Empty;
        }

        /// <summary>Wraps a widget with a layout size when the caller asked for one.</summary>
        public static IWidget WithSize(IWidget widget, double weight, int height, bool fill) {
            if (height > 0) {
                return new Sized(widget, LayoutSize.Fixed(height));
            }
            if (fill) {
                return new Sized(widget, LayoutSize.Fraction(weight > 0 ? weight : 1));
            }
            if (weight > 0) {
                return new Sized(widget, LayoutSize.Fraction(weight));
            }
            return widget;
        }

        private static void AddItem(List<IWidget> widgets, object? item) {
            object? raw = item is PSObject ps ? ps.BaseObject : item;
            if (raw is object[] array) {
                foreach (object nested in array) {
                    AddItem(widgets, nested);
                }
                return;
            }
            IWidget? widget = AsWidget(raw);
            if (widget != null) {
                widgets.Add(widget);
            }
        }
    }
}
