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
