using System;
using System.Collections.Generic;

namespace ConsoleMonster.Interactive {
    /// <summary>
    /// Menu presets and remembered selections, shared by every menu layout: listing the built-in and saved presets,
    /// applying one (the selections it names, then the forms' values), and reading or restoring the selections of a menu
    /// tree by item path.
    /// </summary>
    internal static class MenuPresets {
        /// <summary>The section of the settings store that holds menu presets.</summary>
        public const string Key = "menu";

        /// <summary>The built-in presets first (in the order declared), then the saved ones whose names no built-in one takes.</summary>
        public static IReadOnlyList<PresetListing> List(Menu root, ChoiceStore? store) {
            List<PresetListing> presets = new List<PresetListing>();
            foreach (MenuPreset preset in root.Presets) {
                presets.Add(new PresetListing(preset.Name, preset.Description, true, preset.Preview));
            }
            if (store != null) {
                foreach (string name in store.PresetNames(Key)) {
                    if (root.FindPreset(name) == null) {
                        presets.Add(new PresetListing(name, null, false));
                    }
                }
            }
            return presets;
        }

        /// <summary>
        /// Applies a preset by name: a built-in one and a saved one apply the same way, the selections they name and then
        /// the forms' values (through a form's view when it is open, so the form counts as changed).
        /// </summary>
        /// <returns>Whether a preset with that name was found and applied.</returns>
        public static bool Apply(Menu root, ChoiceStore? store, string name) {
            MenuPreset? builtIn = root.FindPreset(name);
            if (builtIn != null) {
                Restore(root, string.Empty, builtIn.SelectionMap(root));
                foreach (KeyValuePair<Form, IDictionary<string, object?>> pair in builtIn.FormValues) {
                    ApplyForm(pair.Key, pair.Value, false);
                }
                return true;
            }
            if (store == null || !(store.LoadPreset(Key, name) is IDictionary<string, object?> values)) {
                return false;
            }
            if (values.TryGetValue("selections", out object? selections) && selections is IDictionary<string, object?> selectionMap) {
                Restore(root, string.Empty, selectionMap);
            }
            if (values.TryGetValue("forms", out object? forms) && forms is IDictionary<string, object?> formMap) {
                ApplyForms(root, formMap);
            }
            return true;
        }

        /// <summary>Everything a saved preset keeps: the selections of the menu tree and every form's values.</summary>
        public static Dictionary<string, object?> Capture(Menu root) {
            Dictionary<string, object?> selections = new Dictionary<string, object?>(StringComparer.Ordinal);
            Collect(root, string.Empty, selections);
            Dictionary<string, object?> forms = new Dictionary<string, object?>(StringComparer.Ordinal);
            CollectForms(root, forms);
            return new Dictionary<string, object?>(StringComparer.Ordinal) { ["selections"] = selections, ["forms"] = forms };
        }

        /// <summary>The selections of a menu tree, by item path ("Parent/Item"), in stored form.</summary>
        public static void Collect(Menu menu, string path, Dictionary<string, object?> values) {
            foreach (MenuItem item in menu.Items) {
                string key = path + item.Title;
                if (item.Kind == MenuItemKind.Select || item.Kind == MenuItemKind.MultiSelect) {
                    List<string> stored = new List<string>();
                    foreach (object? v in item.Selection) {
                        if (v != null) {
                            stored.Add(CommandLine.Format(v));
                        }
                    }
                    values[key] = stored;
                }
                if (item.SubmenuDefinition != null) {
                    Collect(item.SubmenuDefinition, key + "/", values);
                }
            }
        }

        /// <summary>Restores selections by item path; values no enabled choice matches are left out.</summary>
        public static void Restore(Menu menu, string path, IDictionary<string, object?> values) {
            foreach (MenuItem item in menu.Items) {
                string key = path + item.Title;
                if ((item.Kind == MenuItemKind.Select || item.Kind == MenuItemKind.MultiSelect) && values.TryGetValue(key, out object? stored) && stored is System.Collections.IEnumerable list) {
                    HashSet<string> wanted = new HashSet<string>(StringComparer.Ordinal);
                    foreach (object? v in list) {
                        if (v is string s) {
                            wanted.Add(s);
                        }
                    }
                    List<object?> selection = new List<object?>();
                    foreach (Choice c in item.Choices) {
                        if (c.Value != null && c.IsEnabled && wanted.Contains(CommandLine.Format(c.Value))) {
                            selection.Add(c.Value);
                        }
                    }
                    item.SetSelection(selection);
                }
                if (item.SubmenuDefinition != null) {
                    Restore(item.SubmenuDefinition, key + "/", values);
                }
            }
        }

        private static void CollectForms(Menu menu, Dictionary<string, object?> forms) {
            foreach (MenuItem item in menu.Items) {
                if (item.FormDefinition != null) {
                    forms[item.FormDefinition.Key] = item.FormDefinition.ToStored();
                }
                if (item.SubmenuDefinition != null) {
                    CollectForms(item.SubmenuDefinition, forms);
                }
            }
        }

        private static void ApplyForms(Menu menu, IDictionary<string, object?> forms) {
            foreach (MenuItem item in menu.Items) {
                if (item.FormDefinition != null && forms.TryGetValue(item.FormDefinition.Key, out object? stored) && stored is IDictionary<string, object?> map) {
                    ApplyForm(item.FormDefinition, map, true);
                }
                if (item.SubmenuDefinition != null) {
                    ApplyForms(item.SubmenuDefinition, forms);
                }
            }
        }

        // Applies values to a form: through its view when one shows it (so it counts as changed), else to the form.
        private static void ApplyForm(Form form, IDictionary<string, object?> values, bool stored) {
            FormView? view = form.View;
            if (view != null) {
                view.ApplyValues(values, stored, true);
            } else {
                form.ApplyDetached(values, stored);
            }
        }
    }
}
