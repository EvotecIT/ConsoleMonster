using System;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Interactive;

namespace ConsoleMonster.PowerShell {
    /// <summary>Maps parent preset records onto the existing typed form/menu preset models.</summary>
    internal static class PSPresets {
        internal static bool IsRecord(object? value) => Read(value, "Name") != null;

        internal static FormPreset Form(object source, PSCmdlet cmdlet) {
            if (PSText.Unwrap(source) is FormPreset typed) { return typed; }
            string name = Name(source);
            if (!(Read(source, "Values") is IDictionary values)) { throw new PSArgumentException("Form preset '" + name + "' requires Values mapped by field key."); }
            return new FormPreset(name, PSForms.ToFieldValues(values, null)) {
                Description = Read(source, "Description") as string, Preview = PSForms.Content(Read(source, "Preview"), cmdlet)
            };
        }

        internal static MenuPreset Menu(object source, PSCmdlet cmdlet) {
            if (PSText.Unwrap(source) is MenuPreset typed) { return typed; }
            string name = Name(source);
            var preset = new MenuPreset(name) { Description = Read(source, "Description") as string, Preview = PSForms.Content(Read(source, "Preview"), cmdlet) };
            object? formValue = Read(source, "Form");
            object? values = Read(source, "Values");
            if (formValue != null || values != null) {
                if (!(formValue is Form form)) { throw new PSArgumentException("Menu preset Values requires its Form."); }
                if (values != null && !(values is IDictionary)) { throw new PSArgumentException("Menu preset Values must be a dictionary."); }
                preset.Form(form, PSForms.ToFieldValues(values as IDictionary, form));
            }
            object? selection = Read(source, "Selection");
            object? selectedItem = Read(source, "Item");
            if (selection != null || selectedItem != null) {
                if (!(selectedItem is MenuItem item)) { throw new PSArgumentException("Menu preset Selection requires its Item."); }
                Select(preset, item, selection);
            }
            object? selections = Read(source, "Selections");
            if (selections != null) {
                if (!(selections is IDictionary bindings)) { throw new PSArgumentException("Selections must map MenuItem objects to selected values."); }
                foreach (DictionaryEntry binding in bindings) {
                    if (!(PSText.Unwrap(binding.Key) is MenuItem item)) { throw new PSArgumentException("Selections keys must be MenuItem objects."); }
                    Select(preset, item, binding.Value);
                }
            }
            return preset;
        }

        private static void Select(MenuPreset preset, MenuItem item, object? values) {
            var selected = new List<object?>();
            foreach (object? value in PSInteractive.Enumerate(values)) {
                Choice? match = PSInteractive.Find(item.Choices, value);
                selected.Add(match != null ? match.Value : value == null ? null : Binding.PSObjectMemberSource.Identity(value));
            }
            preset.Selection(item, selected);
        }

        private static string Name(object source) {
            string name = PSText.PlainText(Read(source, "Name"));
            if (string.IsNullOrWhiteSpace(name)) { throw new PSArgumentException("A preset record needs Name."); }
            return name;
        }

        private static object? Read(object? source, string name) {
            object? value = PSText.Unwrap(source);
            if (value is IDictionary dictionary) {
                foreach (DictionaryEntry entry in dictionary) {
                    if (string.Equals(PSText.PlainText(entry.Key), name, StringComparison.OrdinalIgnoreCase)) { return PSText.Unwrap(entry.Value); }
                }
                return null;
            }
            return source == null ? null : PSText.Unwrap(PSObject.AsPSObject(source).Properties[name]?.Value);
        }
    }
}
