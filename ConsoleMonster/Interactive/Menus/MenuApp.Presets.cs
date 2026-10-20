using System;
using System.Collections.Generic;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Interactive {
    // Menu presets: every selection and form value under one name, saved by people or built into the app.
    internal sealed partial class MenuApp {
        private const string PresetKey = "menu";

        // The built-in presets first (in the order declared), then the saved ones whose names no built-in one takes.
        private IReadOnlyList<PresetListing> MenuPresets() {
            List<PresetListing> presets = new List<PresetListing>();
            foreach (MenuPreset preset in _root.Presets) {
                presets.Add(new PresetListing(preset.Name, preset.Description, true, preset.Preview));
            }
            if (_store != null) {
                foreach (string name in _store.PresetNames(PresetKey)) {
                    if (_root.FindPreset(name) == null) {
                        presets.Add(new PresetListing(name, null, false));
                    }
                }
            }
            return presets;
        }

        private void PromptPresetName() {
            ShowModal(new TextDialog(Labels.SavePreset, null, null, null, SaveMenuPreset, Labels));
        }

        private void SaveMenuPreset(string name) {
            if (_store == null) {
                return;
            }
            if (_root.FindPreset(name) != null) {
                ShowToast(new Toast(PromptLabels.Format(Labels.PresetReadOnly, name), StatusKind.Warn));
                return;
            }
            Dictionary<string, object?> selections = new Dictionary<string, object?>(StringComparer.Ordinal);
            Collect(_root, string.Empty, selections);
            Dictionary<string, object?> forms = new Dictionary<string, object?>(StringComparer.Ordinal);
            CollectForms(_root, forms);
            Dictionary<string, object?> values = new Dictionary<string, object?>(StringComparer.Ordinal) { ["selections"] = selections, ["forms"] = forms };
            try {
                _store.SavePreset(PresetKey, name, values);
                ShowToast(new Toast(PromptLabels.Format(Labels.PresetSaved, name)));
            } catch (Exception ex) when (ex is System.IO.IOException || ex is UnauthorizedAccessException) {
                ShowToast(new Toast(ex.Message, StatusKind.Fail));
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

        // A built-in preset and a saved one apply the same way: the selections they name, then the forms' values.
        private void ApplyMenuPreset(string name) {
            MenuPreset? builtIn = _root.FindPreset(name);
            if (builtIn != null) {
                ApplySelections(builtIn.SelectionMap(_root));
                foreach (KeyValuePair<Form, IDictionary<string, object?>> pair in builtIn.FormValues) {
                    ApplyForm(pair.Key, pair.Value, false);
                }
                ShowToast(new Toast(PromptLabels.Format(Labels.PresetApplied, builtIn.Name), StatusKind.Info));
                return;
            }
            if (_store == null || !(_store.LoadPreset(PresetKey, name) is IDictionary<string, object?> values)) {
                return;
            }
            if (values.TryGetValue("selections", out object? selections) && selections is IDictionary<string, object?> selectionMap) {
                ApplySelections(selectionMap);
            }
            if (values.TryGetValue("forms", out object? forms) && forms is IDictionary<string, object?> formMap) {
                ApplyForms(_root, formMap);
            }
            ShowToast(new Toast(PromptLabels.Format(Labels.PresetApplied, name), StatusKind.Info));
        }

        private void ApplySelections(IDictionary<string, object?> selectionMap) {
            Restore(_root, string.Empty, selectionMap);
            // Selection panes are rebuilt from the items, so they show the new selections.
            List<MenuItem> stale = new List<MenuItem>();
            foreach (KeyValuePair<MenuItem, IPane> pair in _panes) {
                if (pair.Value is ChoiceItemPane) {
                    stale.Add(pair.Key);
                }
            }
            foreach (MenuItem item in stale) {
                _panes.Remove(item);
            }
        }

        private void ApplyForms(Menu menu, IDictionary<string, object?> forms) {
            foreach (MenuItem item in menu.Items) {
                if (item.FormDefinition != null && forms.TryGetValue(item.FormDefinition.Key, out object? stored) && stored is IDictionary<string, object?> map) {
                    ApplyForm(item.FormDefinition, map, true);
                }
                if (item.SubmenuDefinition != null) {
                    ApplyForms(item.SubmenuDefinition, forms);
                }
            }
        }

        // Applies values to a form: through its pane when it has been opened (so it counts as changed), else to the form.
        private static void ApplyForm(Form form, IDictionary<string, object?> values, bool stored) {
            FormView? view = form.View;
            if (view != null) {
                view.ApplyValues(values, stored, true);
            } else {
                form.ApplyDetached(values, stored);
            }
        }

        private void DeleteMenuPreset(string name) {
            if (_root.FindPreset(name) != null) {
                ShowToast(new Toast(PromptLabels.Format(Labels.PresetReadOnly, name), StatusKind.Warn));
                return;
            }
            ShowModal(new ConfirmDialog(Labels.Presets, name, () => {
                if (_store != null && _store.DeletePreset(PresetKey, name)) {
                    ShowToast(new Toast(name, StatusKind.Info));
                }
            }, Labels, true));
        }

        // The built-in presets of forms not opened yet (opened forms list theirs above, saved ones included).
        private void AddUnopenedFormPresets(Menu menu, List<PaletteCommand> commands) {
            foreach (MenuItem item in menu.Items) {
                if (item.FormDefinition != null && !_panes.ContainsKey(item)) {
                    foreach (FormPreset preset in item.FormDefinition.Presets) {
                        MenuItem target = item;
                        string name = preset.Name;
                        commands.Add(new PaletteCommand(name, _ => {
                            Activate(target);
                            (PaneFor(target) as FormView)?.ApplyPreset(name);
                        }) {
                            Category = Labels.Presets + " " + InteractiveGlyphs.For(_context).Breadcrumb + " " + item.Title,
                            Description = Labels.BuiltIn
                        });
                    }
                }
                if (item.SubmenuDefinition != null) {
                    AddUnopenedFormPresets(item.SubmenuDefinition, commands);
                }
            }
        }
    }
}
