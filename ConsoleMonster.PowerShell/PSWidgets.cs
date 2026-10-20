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

        /// <summary>
        /// Panel content from anything: a scriptblock (its output), a widget, a string, or other objects, which go through
        /// the shared value rules (one object as a record, several as a table, as Format-ConsoleList and Format-ConsoleTable
        /// show them). Widgets and text keep their place among the objects.
        /// </summary>
        public static List<IWidget> CollectAny(object? content, PSCmdlet cmdlet) {
            List<IWidget> widgets = new List<IWidget>();
            object? raw = content is PSObject wrapped ? wrapped.BaseObject : content;
            if (raw == null) {
                return widgets;
            }
            List<PSObject> items = new List<PSObject>();
            if (raw is ScriptBlock script) {
                foreach (PSObject emitted in script.Invoke()) {
                    // An array emitted as one object is its items, as the other containers read it.
                    if (emitted?.BaseObject is object[] array) {
                        foreach (object element in array) {
                            if (element != null) {
                                items.Add(PSObject.AsPSObject(element));
                            }
                        }
                    } else if (emitted != null) {
                        items.Add(emitted);
                    }
                }
            } else if (raw is IWidget || raw is string) {
                items.Add(PSObject.AsPSObject(raw));
            } else if (LanguagePrimitives.GetEnumerable(raw) is System.Collections.IEnumerable list) {
                foreach (object? element in list) {
                    if (element != null) {
                        items.Add(PSObject.AsPSObject(element));
                    }
                }
            } else {
                items.Add(content is PSObject ps ? ps : PSObject.AsPSObject(raw));
            }
            List<PSObject> objects = new List<PSObject>();
            foreach (PSObject item in items) {
                if (item == null) {
                    continue;
                }
                IWidget? widget = AsWidget(item);
                if (widget != null) {
                    FlushObjects(widgets, objects, cmdlet);
                    widgets.Add(widget);
                } else {
                    objects.Add(item);
                }
            }
            FlushObjects(widgets, objects, cmdlet);
            return widgets;
        }

        private static void FlushObjects(List<IWidget> widgets, List<PSObject> objects, PSCmdlet cmdlet) {
            if (objects.Count == 0) {
                return;
            }
            widgets.Add(objects.Count == 1
                ? PSFormatting.BuildList(objects.ToArray(), null, null, false, 1, null, null, cmdlet)
                : PSFormatting.Combine(PSFormatting.BuildTables(objects.ToArray(), null, null, new TableBuildOptions { Cmdlet = cmdlet }, table => table.RecordViewRows = 0)));
            objects.Clear();
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
