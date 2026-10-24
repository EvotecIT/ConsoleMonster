using System;
using System.Collections.Generic;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Interactive {
    // Menu presets: every selection and form value under one name, saved by people or built into the app. The listing and
    // applying are shared with the inline menu (MenuPresets); this part adds the panes, dialogs and toasts.
    internal sealed partial class MenuApp {
        private IReadOnlyList<PresetListing> ListMenuPresets() => MenuPresets.List(_root, _store);

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
            try {
                _store.SavePreset(MenuPresets.Key, name, MenuPresets.Capture(_root));
                ShowToast(new Toast(PromptLabels.Format(Labels.PresetSaved, name)));
            } catch (Exception ex) when (ex is System.IO.IOException || ex is UnauthorizedAccessException) {
                ShowToast(new Toast(ex.Message, StatusKind.Fail));
            }
        }

        private void ApplyMenuPreset(string name) {
            if (!MenuPresets.Apply(_root, _store, name)) {
                return;
            }
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
            ShowToast(new Toast(PromptLabels.Format(Labels.PresetApplied, name), StatusKind.Info));
        }

        private void DeleteMenuPreset(string name) {
            if (_root.FindPreset(name) != null) {
                ShowToast(new Toast(PromptLabels.Format(Labels.PresetReadOnly, name), StatusKind.Warn));
                return;
            }
            ShowModal(new ConfirmDialog(Labels.Presets, name, () => {
                if (_store != null && _store.DeletePreset(MenuPresets.Key, name)) {
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
                            Category = Labels.Presets + " " + InteractiveGlyphs.For(_context).Breadcrumb + " " + item.Label,
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
