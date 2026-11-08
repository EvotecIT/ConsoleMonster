using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Management.Automation;
using ConsoleMonster.Presenters;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    // The facts line under a default view: a registration's Summary script gets the objects shown together ($_ and the
    // first argument) and returns facts, each a string or a hashtable with Value, Label, Caption, Presenter, Role and NewLine.
    internal static class ViewSummary {
        /// <summary>The facts for one batch, or null when the script returns none or fails (the view is shown without them).</summary>
        internal static ViewFacts? Build(ScriptBlock summary, IReadOnlyList<PSObject> items) {
            Collection<PSObject> output;
            object[] batch = new object[items.Count];
            for (int i = 0; i < items.Count; i++) { batch[i] = items[i]; }
            try {
                output = summary.InvokeWithContext(null, new List<PSVariable> { new PSVariable("_", batch) }, new object[] { batch });
            } catch (RuntimeException) {
                return null;
            }
            List<ViewFact> facts = new List<ViewFact>();
            foreach (PSObject? result in output) {
                ViewFact? fact = Fact(result?.BaseObject);
                if (fact != null) { facts.Add(fact); }
            }
            return facts.Count == 0 ? null : new ViewFacts(facts);
        }

        private static ViewFact? Fact(object? value) {
            if (value == null) { return null; }
            if (value is string text) { return string.IsNullOrWhiteSpace(text) ? null : new ViewFact(text, presenter: CellPresenters.Text); }
            if (!(value is IDictionary map)) { return new ViewFact(value); }
            object? factValue = Get(map, "Value");
            if (factValue is PSObject wrapped) { factValue = wrapped.BaseObject; }
            string? label = Get(map, "Label") as string;
            string? caption = Get(map, "Caption") as string;
            if (factValue == null && label == null && caption == null) { return null; }
            ICellPresenter? presenter = null;
            object? configured = Get(map, "Presenter");
            if (configured is PSObject presenterObject) { configured = presenterObject.BaseObject; }
            if (configured is ICellPresenter given) { presenter = given; }
            else if (configured != null) {
                presenter = CellPresenters.FromName(Convert.ToString(configured, CultureInfo.InvariantCulture) ?? string.Empty, Get(map, "Unit") as string);
            }
            ThemeToken? role = null;
            object? roleValue = Get(map, "Role");
            if (roleValue is PSObject roleObject) { roleValue = roleObject.BaseObject; }
            if (roleValue is ThemeToken token) { role = token; }
            else if (roleValue != null && Enum.TryParse(Convert.ToString(roleValue, CultureInfo.InvariantCulture), true, out ThemeToken parsed)) { role = parsed; }
            object? newLine = Get(map, "NewLine");
            return new ViewFact(factValue, label, caption, presenter, role, newLine != null && LanguagePrimitives.IsTrue(newLine));
        }

        private static object? Get(IDictionary map, string key) {
            foreach (DictionaryEntry entry in map) {
                if (string.Equals(entry.Key as string, key, StringComparison.OrdinalIgnoreCase)) { return entry.Value; }
            }
            return null;
        }
    }
}
