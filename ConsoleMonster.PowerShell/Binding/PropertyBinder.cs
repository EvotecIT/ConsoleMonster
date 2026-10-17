using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Management.Automation;
using ConsoleMonster.Data;
using ConsoleMonster.Layout;
using ConsoleMonster.Presenters;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell.Binding {
    /// <summary>Resolves Format-Table style property selections and calculated columns.</summary>
    public static class PropertyBinder {
        private static readonly string[] ValidKeys = {
            "Name",
            "N",
            "Label",
            "L",
            "Expression",
            "E",
            "Overflow",
            "MaxLines",
            "Priority",
            "Key",
            "MinWidth",
            "MaxWidth",
            "Width",
            "Alignment",
            "Presenter",
            "Identifier",
            "Highlight",
            "Unit",
            "Aggregate",
            "Flexible",
            "Header",
            "Markup",
            "FailAbove",
            "WarnAbove",
            "PassAbove",
            "FailBelow",
            "WarnBelow",
            "PassBelow",
            "Tone",
        };

        /// <summary>Binds property expressions and column configuration to a dataset.</summary>
        public static IReadOnlyList<BoundColumn> Bind(
            IReadOnlyList<PSObject> items,
            object[]? property
        ) {
            if (items == null)
                throw new ArgumentNullException(nameof(items));
            List<BoundColumn> result = new List<BoundColumn>();
            if (property == null || property.Length == 0) {
                foreach (string name in DefaultNames(items))
                    result.Add(Create(name, name, null));
            }
            else {
                foreach (object spec in property) {
                    if (spec is string text) {
                        foreach (string matched in MatchNames(items, text))
                            if (
                                !result.Any(c =>
                                    string.Equals(
                                        c.Name,
                                        matched,
                                        StringComparison.OrdinalIgnoreCase
                                    )
                                )
                            )
                                result.Add(Create(matched, matched, null));
                    }
                    else if (spec is IDictionary dictionary) {
                        result.Add(CreateCalculated(dictionary));
                    }
                    else
                        throw new ArgumentException(
                            "Property entries must be strings or calculated-property hashtables.",
                            nameof(property)
                        );
                }
            }
            if (result.Count > 0 && !result.Any(c => c.Column.IsKey))
                result[0].Column.IsKey = true;
            return result.AsReadOnly();
        }

        /// <summary>Evaluates a string property name or script block against one PowerShell item.</summary>
        public static object? Evaluate(PSObject item, object expression) {
            if (item == null)
                throw new ArgumentNullException(nameof(item));
            if (expression is ScriptBlock script) {
                Collection<PSObject> output = script.InvokeWithContext(
                    null,
                    new List<PSVariable> { new PSVariable("_", item) }
                );
                return output.Count == 0 ? null : Unwrap(output[0]);
            }
            string propertyName =
                Convert.ToString(expression, CultureInfo.InvariantCulture) ?? string.Empty;
            if (
                string.Equals(propertyName, "Value", StringComparison.OrdinalIgnoreCase)
                && ColumnInference.IsScalar(Unwrap(item))
            )
                return Unwrap(item);
            PSPropertyInfo? property = item.Properties[propertyName];
            if (property == null || !property.IsGettable)
                return null;
            return Unwrap(property.Value);
        }

        internal static object? Unwrap(object? value) =>
            value is PSObject wrapped ? wrapped.BaseObject : value;

        private static BoundColumn Create(string name, object expression, IDictionary? options) {
            // Property names are data: shown literally. A Header option is written by the script author: a string is
            // markup (as before), rich text keeps its colours.
            object? configuredHeader = Unwrap(GetOption(options, "Header"));
            string header = configuredHeader is ConsoleMonster.Text.RichText richHeader
                ? richHeader.ToMarkup()
                : configuredHeader != null
                    ? Convert.ToString(configuredHeader, CultureInfo.InvariantCulture) ?? ConsoleMonster.Text.Markup.Escape(name)
                    : ConsoleMonster.Text.Markup.Escape(name);
            TableColumn column = new TableColumn(header) { Key = name };
            ApplyOptions(column, options);
            return new BoundColumn(name, column, expression);
        }

        private static BoundColumn CreateCalculated(IDictionary source) {
            Dictionary<string, object?> options = new Dictionary<string, object?>(
                StringComparer.OrdinalIgnoreCase
            );
            foreach (DictionaryEntry entry in source) {
                string key =
                    Convert.ToString(entry.Key, CultureInfo.InvariantCulture) ?? string.Empty;
                if (
                    !ValidKeys.Any(valid =>
                        string.Equals(valid, key, StringComparison.OrdinalIgnoreCase)
                    )
                )
                    throw new ArgumentException(
                        "Unknown calculated property key '"
                            + key
                            + "'. Valid keys: "
                            + string.Join(", ", ValidKeys)
                            + ".",
                        nameof(source)
                    );
                options[key] = entry.Value;
            }
            object? expression = First(options, "Expression", "E");
            string name =
                Convert.ToString(
                    First(options, "Name", "N", "Label", "L"),
                    CultureInfo.InvariantCulture
                ) ?? string.Empty;
            if (expression == null) {
                if (name.Length == 0)
                    throw new ArgumentException(
                        "A calculated property requires Name or Expression.",
                        nameof(source)
                    );
                expression = name;
            }
            if (name.Length == 0)
                name = expression is string value ? value : "Expression";
            Hashtable properties = new Hashtable(StringComparer.OrdinalIgnoreCase);
            foreach (KeyValuePair<string, object?> pair in options)
                properties[pair.Key] = pair.Value;
            return Create(name, expression, properties);
        }

        private static void ApplyOptions(TableColumn column, IDictionary? options) {
            if (options == null)
                return;
            SetEnum<ColumnOverflow>(options, "Overflow", value => column.Overflow = value);
            SetEnum<HorizontalAlignment>(options, "Alignment", value => column.Alignment = value);
            SetEnum<ColumnAggregate>(options, "Aggregate", value => column.Aggregate = value);
            SetInt(options, "MaxLines", value => column.MaxLines = value);
            SetInt(options, "Priority", value => column.Priority = value);
            SetInt(options, "MinWidth", value => column.MinWidth = value);
            SetInt(options, "MaxWidth", value => column.MaxWidth = value);
            SetInt(options, "Width", value => column.Width = value);
            SetBool(options, "Key", value => column.IsKey = value);
            SetBool(options, "Identifier", value => column.Presenter = value ? CellPresenters.Identifier : column.Presenter);
            SetBool(options, "Flexible", value => column.Flexible = value);
            if (TryOption(options, "Presenter", out object? presenter))
                column.Presenter = CellPresenters.FromName(
                    Convert.ToString(presenter, CultureInfo.InvariantCulture) ?? string.Empty,
                    OptionString(options, "Unit")
                );
            // Highlight = 'Smart' (or a language): the cell text is highlighted, and stays literal.
            if (TryOption(options, "Highlight", out object? highlight)) {
                string language = Convert.ToString(highlight, CultureInfo.InvariantCulture) ?? string.Empty;
                ConsoleMonster.Highlighting.ITextHighlighter highlighter = ConsoleMonster.Highlighting.Highlighters.Get(language) ?? throw new ArgumentException("Unknown highlight language '" + language + "'. Use Smart or one of: " + string.Join(", ", ConsoleMonster.Highlighting.Highlighters.Names) + ".");
                column.Presenter = new ConsoleMonster.Presenters.HighlightPresenter(highlighter);
            }
            // Markup = $true: string values are markup ([pass]OK[/]); data stays literal otherwise.
            SetBool(options, "Markup", value => {
                if (value && column.Presenter == null)
                    column.Presenter = CellPresenters.Markup;
            });
            AddThreshold(options, "FailAbove", true, ThemeToken.Fail, column);
            AddThreshold(options, "WarnAbove", true, ThemeToken.Warn, column);
            AddThreshold(options, "PassAbove", true, ThemeToken.Pass, column);
            AddThreshold(options, "FailBelow", false, ThemeToken.Fail, column);
            AddThreshold(options, "WarnBelow", false, ThemeToken.Warn, column);
            AddThreshold(options, "PassBelow", false, ThemeToken.Pass, column);
            if (TryOption(options, "Tone", out object? tone)) {
                if (tone is IDictionary single)
                    AddTone(column, single);
                else if (tone is IEnumerable tones && !(tone is string))
                    foreach (object? rule in tones) {
                        if (!(rule is IDictionary map))
                            throw new ArgumentException(
                                "Tone must be a hashtable or an array of hashtables."
                            );
                        AddTone(column, map);
                    }
                else
                    throw new ArgumentException(
                        "Tone must be a hashtable or an array of hashtables."
                    );
            }
        }

        private static void AddTone(TableColumn column, IDictionary rule) {
            object? toneValue = Option(rule, "Tone");
            if (toneValue == null)
                throw new ArgumentException("Each Tone rule requires a Tone key.");
            ThemeToken tone = ParseEnum<ThemeToken>(toneValue, "Tone");
            if (TryOption(rule, "Above", out object? above))
                column.ToneRules.Add(ToneRule.Above(Number(above, "Above"), tone));
            else if (TryOption(rule, "Below", out object? below))
                column.ToneRules.Add(ToneRule.Below(Number(below, "Below"), tone));
            else if (TryOption(rule, "Equals", out object? equals))
                column.ToneRules.Add(ToneRule.Equal(equals, tone));
            else
                throw new ArgumentException("Tone rule requires Above, Below or Equals.");
        }

        private static void AddThreshold(
            IDictionary options,
            string key,
            bool above,
            ThemeToken tone,
            TableColumn column
        ) {
            if (TryOption(options, key, out object? value))
                column.ToneRules.Add(
                    above
                        ? ToneRule.Above(Number(value, key), tone)
                        : ToneRule.Below(Number(value, key), tone)
                );
        }

        private static double Number(object? value, string key) {
            try {
                return Convert.ToDouble(value, CultureInfo.InvariantCulture);
            }
            catch {
                throw new ArgumentException("Column option '" + key + "' must be numeric.");
            }
        }

        private static void SetInt(IDictionary options, string key, Action<int> set) {
            if (TryOption(options, key, out object? value)) {
                try {
                    set(Convert.ToInt32(value, CultureInfo.InvariantCulture));
                }
                catch {
                    throw new ArgumentException("Column option '" + key + "' must be an integer.");
                }
            }
        }

        private static void SetBool(IDictionary options, string key, Action<bool> set) {
            if (TryOption(options, key, out object? value)) {
                try {
                    set(Convert.ToBoolean(value, CultureInfo.InvariantCulture));
                }
                catch {
                    throw new ArgumentException("Column option '" + key + "' must be Boolean.");
                }
            }
        }

        private static void SetEnum<T>(IDictionary options, string key, Action<T> set)
            where T : struct {
            if (TryOption(options, key, out object? value))
                set(ParseEnum<T>(value, key));
        }

        private static T ParseEnum<T>(object? value, string key)
            where T : struct {
            string text = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
            if (Enum.TryParse(text, true, out T parsed) && Enum.IsDefined(typeof(T), parsed))
                return parsed;
            throw new ArgumentException(
                "Invalid value '"
                    + text
                    + "' for '"
                    + key
                    + "'. Valid values: "
                    + string.Join(", ", Enum.GetNames(typeof(T)))
                    + "."
            );
        }

        private static IEnumerable<string> DefaultNames(IReadOnlyList<PSObject> items) {
            if (items.Count == 0)
                return Array.Empty<string>();
            bool hasScalar = items.Any(IsScalar);
            if (hasScalar && items.All(IsScalar))
                return new[] { "Value" };
            PSPropertySet? display = IsScalar(items[0]) ? null : GetDefaultDisplay(items[0]);
            List<string> names = new List<string>();
            if (display != null) {
                if (hasScalar)
                    names.Add("Value");
                foreach (string name in display.ReferencedPropertyNames)
                    if (!names.Contains(name, StringComparer.OrdinalIgnoreCase))
                        names.Add(name);
                return names;
            }
            foreach (PSObject item in items) {
                if (IsScalar(item))
                    continue;
                foreach (PSPropertyInfo member in item.Properties)
                    if (
                        Usable(member)
                        && !names.Contains(member.Name, StringComparer.OrdinalIgnoreCase)
                    )
                        names.Add(member.Name);
            }
            if (hasScalar && !names.Contains("Value", StringComparer.OrdinalIgnoreCase))
                names.Insert(0, "Value");
            return names;
        }

        private static IEnumerable<string> MatchNames(IReadOnlyList<PSObject> items, string pattern) {
            List<string> names = new List<string>();
            foreach (PSObject item in items)
            foreach (PSPropertyInfo member in item.Properties)
                if (
                    Usable(member)
                    && Wildcard(member.Name, pattern)
                    && !names.Contains(member.Name, StringComparer.OrdinalIgnoreCase)
                )
                    names.Add(member.Name);
            if (pattern.IndexOfAny(new[] { '*', '?' }) < 0 && names.Count == 0)
                names.Add(pattern);
            return names;
        }

        private static bool Usable(PSPropertyInfo member) =>
            member.IsGettable
            && !string.Equals(member.Name, "PSStandardMembers", StringComparison.OrdinalIgnoreCase);

        private static PSPropertySet? GetDefaultDisplay(PSObject item) {
            PSMemberSet? standard = item.Members["PSStandardMembers"] as PSMemberSet;
            return standard?.Members["DefaultDisplayPropertySet"] as PSPropertySet;
        }

        internal static PSPropertySet? GetDefaultDisplayForSource(PSObject item) =>
            GetDefaultDisplay(item);

        private static bool IsScalar(PSObject item) => ColumnInference.IsScalar(Unwrap(item));

        private static object? First(IDictionary<string, object?> map, params string[] keys) {
            foreach (string key in keys)
                if (map.TryGetValue(key, out object? value) && value != null)
                    return value;
            return null;
        }

        private static object? GetOption(IDictionary? map, string key) =>
            TryOption(map, key, out object? value) ? value : null;

        private static object? Option(IDictionary map, string key) => GetOption(map, key);

        private static string? OptionString(IDictionary map, string key) =>
            Convert.ToString(GetOption(map, key), CultureInfo.InvariantCulture);

        private static bool TryOption(IDictionary? map, string key, out object? value) {
            if (map != null)
                foreach (DictionaryEntry entry in map)
                    if (
                        string.Equals(
                            Convert.ToString(entry.Key, CultureInfo.InvariantCulture),
                            key,
                            StringComparison.OrdinalIgnoreCase
                        )
                    ) {
                        value = entry.Value;
                        return true;
                    }
            value = null;
            return false;
        }

        private static bool Wildcard(string text, string pattern) {
            int t = 0,
                p = 0,
                star = -1,
                mark = -1;
            while (t < text.Length) {
                if (
                    p < pattern.Length
                    && (
                        pattern[p] == '?'
                        || char.ToUpperInvariant(pattern[p]) == char.ToUpperInvariant(text[t])
                    )
                ) {
                    p++;
                    t++;
                }
                else if (p < pattern.Length && pattern[p] == '*') {
                    star = p++;
                    mark = t;
                }
                else if (star >= 0) {
                    p = star + 1;
                    t = ++mark;
                }
                else
                    return false;
            }
            while (p < pattern.Length && pattern[p] == '*')
                p++;
            return p == pattern.Length;
        }
    }
}
