using System;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;
using System.Runtime.CompilerServices;
using ConsoleMonster.Interactive;

namespace ConsoleMonster.PowerShell {
    /// <summary>Helpers for the form cmdlets: page assignment, values as hashtables and objects, and reviews from hashtables.</summary>
    internal static class PSForms {
        private static readonly ConditionalWeakTable<Field, PageName> Pages = new ConditionalWeakTable<Field, PageName>();

        private sealed class PageName {
            public PageName(string name) {
                Name = name;
            }

            public string Name { get; }
        }

        public static void SetPage(Field field, string? page) {
            Pages.Remove(field);
            if (!string.IsNullOrEmpty(page)) {
                Pages.Add(field, new PageName(page!));
            }
        }

        public static string? PageOf(Field field) => Pages.TryGetValue(field, out PageName? page) ? page.Name : null;

        /// <summary>Values as a case-insensitive hashtable (multi-selections as object arrays).</summary>
        public static Hashtable ToHashtable(FormValues values, Form? form) {
            Hashtable table = new Hashtable(StringComparer.OrdinalIgnoreCase);
            foreach (KeyValuePair<string, object?> pair in values) {
                table[pair.Key] = Normalize(pair.Value);
            }
            return table;
        }

        /// <summary>Values as an object with one property per visible field (in form order); secrets as SecureString unless asked otherwise.</summary>
        public static PSObject ToObject(FormValues values, Form form, bool plainSecrets) {
            PSObject result = new PSObject();
            foreach (Field field in form.Fields) {
                if (!values.IsVisible(field.Name)) {
                    continue;
                }
                object? value = Normalize(values[field.Name]);
                if (field.Kind == FieldKind.Secret && !plainSecrets && value is string secret) {
                    value = PSInteractive.ToSecureString(secret);
                }
                result.Properties.Add(new PSNoteProperty(field.Name, value));
            }
            return result;
        }

        private static object? Normalize(object? value) {
            if (value is IEnumerable list && !(value is string)) {
                List<object?> items = new List<object?>();
                foreach (object? item in list) {
                    items.Add(item);
                }
                return items.ToArray();
            }
            return value;
        }

        /// <summary>Builds a review from a hashtable (Rows, Command, Problems, Run, Title, Description, RunText) or passes a model through.</summary>
        public static ReviewModel ToReview(object? output) {
            object? raw = PSText.Unwrap(output);
            if (raw is ReviewModel model) {
                return model;
            }
            ReviewModel review = new ReviewModel();
            if (!(raw is IDictionary map)) {
                return review;
            }
            review.Title = Get(map, "Title") is object title ? PSInteractive.Text(title) : null;
            review.Description = Get(map, "Description") is object description ? PSInteractive.Text(description) : null;
            review.Command = Get(map, "Command") is object command ? PSInteractive.Text(command) : null;
            review.RunText = Get(map, "RunText") is object runText ? PSInteractive.Text(runText) : null;
            if (PSText.Unwrap(Get(map, "Rows")) is IDictionary rows) {
                foreach (DictionaryEntry entry in rows) {
                    review.Row(PSInteractive.Text(entry.Key), Join(entry.Value));
                }
            }
            foreach (object? problem in PSInteractive.Enumerate(Get(map, "Problems"))) {
                string text = PSInteractive.Text(problem);
                if (text.Length > 0) {
                    review.Problem(text);
                }
            }
            if (PSText.Unwrap(Get(map, "Run")) is ScriptBlock run) {
                review.Run = context => PSMenus.Invoke(run, context);
            }
            return review;
        }

        /// <summary>
        /// Field values from a hashtable, as a form, a preset or Form.Apply takes them: names (ignoring case) to plain values,
        /// collections as lists. Values of a choice field of <paramref name="form"/> that name one of its choices (the
        /// object, its title or its value's text) become that choice's value.
        /// </summary>
        public static Dictionary<string, object?> ToFieldValues(IDictionary? values, Form? form) {
            Dictionary<string, object?> result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            if (values == null) {
                return result;
            }
            foreach (DictionaryEntry entry in values) {
                string name = PSInteractive.Text(entry.Key);
                if (name.Length == 0) {
                    continue;
                }
                result[name] = FieldValue(entry.Value, form?.Find(name) as ChoiceField);
            }
            return result;
        }

        /// <summary>Maps the values of a built-in preset collected before its form existed onto the form's choices (see <see cref="ToFieldValues"/>).</summary>
        public static void MatchChoices(IDictionary<string, object?> values, Form form) {
            foreach (string name in new List<string>(values.Keys)) {
                if (form.Find(name) is ChoiceField field) {
                    values[name] = FieldValue(values[name], field);
                }
            }
        }

        private static object? FieldValue(object? value, ChoiceField? field) {
            object? raw = PSText.Unwrap(value);
            if (raw is IEnumerable list && !(raw is string) && !(raw is IDictionary)) {
                List<object?> items = new List<object?>();
                foreach (object? item in list) {
                    items.Add(ChoiceValue(item, field));
                }
                return items;
            }
            return ChoiceValue(value, field);
        }

        private static object? ChoiceValue(object? value, ChoiceField? field) {
            if (field != null) {
                Choice? match = PSInteractive.Find(field.Choices, value);
                if (match != null) {
                    return match.Value;
                }
            }
            return value == null ? null : Binding.PSObjectMemberSource.Identity(value);
        }

        /// <summary>Preview content from anything a cmdlet takes for one: a scriptblock (run now), widgets, text (markup) or objects (as a list); null for nothing.</summary>
        public static Layout.IWidget? Content(object? content, PSCmdlet cmdlet) {
            if (content == null) {
                return null;
            }
            List<Layout.IWidget> widgets = PSWidgets.CollectAny(content, cmdlet);
            if (widgets.Count == 0) {
                return null;
            }
            return widgets.Count == 1 ? widgets[0] : new Layout.Rows(widgets.ToArray()) { Gap = 1 };
        }

        private static object? Get(IDictionary map, string key) {
            foreach (DictionaryEntry entry in map) {
                if (string.Equals(PSInteractive.Text(entry.Key), key, StringComparison.OrdinalIgnoreCase)) {
                    return entry.Value;
                }
            }
            return null;
        }

        private static string Join(object? value) {
            object? raw = PSText.Unwrap(value);
            if (raw is IEnumerable list && !(raw is string)) {
                List<string> parts = new List<string>();
                foreach (object? item in list) {
                    parts.Add(PSInteractive.Text(item));
                }
                return string.Join(", ", parts.ToArray());
            }
            return PSInteractive.Text(raw);
        }
    }
}
