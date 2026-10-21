using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using ConsoleMonster.Views;

namespace ConsoleMonster.PowerShell {
    /// <summary>Discovers conventional module view data without importing modules, changing objects, or executing view scripts.</summary>
    internal static class ConsoleModuleViews {
        internal static void Discover(PSCmdlet cmdlet) {
            FormattingSessionState state = FormattingSessionState.Current;
            foreach (PSObject item in cmdlet.InvokeCommand.InvokeScript("Microsoft.PowerShell.Core\\Get-Module")) {
                if (!(item.BaseObject is PSModuleInfo module) || string.IsNullOrEmpty(module.ModuleBase)) { continue; }
                string path = System.IO.Path.Combine(module.ModuleBase, module.Name + ".consoleviews.psd1");
                string key = module.Path + "|" + module.Guid + "|" + module.Version;
                if (!state.ViewModules.Add(key) || !File.Exists(path)) { continue; }
                try { Import(cmdlet, path, true); }
                catch (Exception ex) when (!(ex is PipelineStoppedException)) {
                    cmdlet.WriteWarning("ConsoleMonster could not load views from '" + path + "': " + ex.Message);
                }
            }
        }

        internal static void Import(PSCmdlet cmdlet, string path, bool onlyIfMissing) {
            if (!path.EndsWith(".consoleviews.psd1", StringComparison.OrdinalIgnoreCase)) {
                throw new PSArgumentException("View files must use the .consoleviews.psd1 extension.");
            }
            var output = cmdlet.InvokeCommand.InvokeScript("param($path) Microsoft.PowerShell.Utility\\Import-PowerShellDataFile -LiteralPath $path -ErrorAction Stop", true, PipelineResultTypes.None, null, path);
            if (output.Count != 1 || !(output[0].BaseObject is IDictionary document)) { throw new PSArgumentException("A view file must contain one data hashtable."); }
            Keys(document, "SchemaVersion", "Views");
            if (Convert.ToInt32(document["SchemaVersion"], System.Globalization.CultureInfo.InvariantCulture) != 1) { throw new PSArgumentException("SchemaVersion must be 1."); }
            List<ConsoleViewRegistration> registrations = new List<ConsoleViewRegistration>();
            foreach (object value in Sequence(document["Views"])) {
                if (!(value is IDictionary view)) { throw new PSArgumentException("Each view must be a data hashtable."); }
                Keys(view, "TypeName", "View", "Property", "GroupBy", "Group");
                string type = Convert.ToString(view["TypeName"], System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;
                if (string.IsNullOrWhiteSpace(type)) { throw new PSArgumentException("Each view needs a nonempty TypeName."); }
                ConsoleViewKind kind = ConsoleViewKind.Table;
                if (view.Contains("View") && (!Enum.TryParse(Convert.ToString(view["View"], System.Globalization.CultureInfo.InvariantCulture), true, out kind) || !Enum.IsDefined(typeof(ConsoleViewKind), kind))) {
                    throw new PSArgumentException("Unknown console view kind.");
                }
                List<object> properties = new List<object>();
                foreach (object property in Sequence(view["Property"])) {
                    if (!(property is string) && !(property is IDictionary)) { throw new PSArgumentException("Properties must be names or data-only column hashtables."); }
                    if (property is IDictionary column) {
                        foreach (object entry in column.Values) {
                            if (entry is ScriptBlock) { throw new PSArgumentException("Declarative views cannot contain script blocks."); }
                        }
                    }
                    properties.Add(property);
                }
                if (properties.Count > 0) { Binding.PropertyBinder.Bind(Array.Empty<PSObject>(), properties.ToArray()); }
                object? groupBy = view["GroupBy"];
                if (groupBy != null && !(groupBy is string)) { throw new PSArgumentException("GroupBy must be a property name."); }
                string group = Convert.ToString(view["Group"], System.Globalization.CultureInfo.InvariantCulture) ?? type;
                registrations.Add(new ConsoleViewRegistration(type, kind, properties.Count > 0 ? properties.ToArray() : null, groupBy, group));
            }
            // Validate the complete file before installing any entry.
            foreach (ConsoleViewRegistration registration in registrations) { ConsoleFormatting.Register(registration, onlyIfMissing); }
        }

        private static IEnumerable Sequence(object? value) => value == null ? Array.Empty<object>() : value is string || value is IDictionary ? new[] { value } : value as IEnumerable ?? new[] { value };

        private static void Keys(IDictionary data, params string[] allowed) {
            foreach (object key in data.Keys) {
                if (!(key is string name) || !Array.Exists(allowed, item => string.Equals(item, name, StringComparison.OrdinalIgnoreCase))) {
                    throw new PSArgumentException("Unknown view-file key: " + key);
                }
            }
        }
    }
}
