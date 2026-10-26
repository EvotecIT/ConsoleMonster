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
            "Role",
            "FormatString",
        };

        /// <summary>Binds property expressions and column configuration to a dataset.</summary>
        public static IReadOnlyList<BoundColumn> Bind(
            IReadOnlyList<PSObject> items,
            object[]? property
        ) => Bind(items, property, null, null);

        /// <summary>
        /// Binds property expressions and column configuration to a dataset. Without properties, the columns come from
        /// <see cref="ColumnResolver"/> (registered view, data keys, PowerShell's table view, default display set, fields).
        /// The key column is the one the shared title rule picks (Name, DisplayName, …Name, Id), else the first.
        /// </summary>
        /// <param name="items">The objects.</param>
        /// <param name="property">Property names and calculated properties, or null for the default columns.</param>
        /// <param name="options">Shape options, or null for PowerShell defaults.</param>
        /// <param name="cmdlet">The calling cmdlet (for PowerShell's table views), or null.</param>
        /// <returns>The bound columns.</returns>
        internal static IReadOnlyList<BoundColumn> Bind(
            IReadOnlyList<PSObject> items,
            object[]? property,
            Values.ShapeOptions? options,
            PSCmdlet? cmdlet
        ) {
            if (items == null)
                throw new ArgumentNullException(nameof(items));
            Values.ShapeOptions shape = options ?? PSShaping.Options();
            StringComparison comparison = items.Any(item => Values.ValueShaper.Prepare(item, shape) is object prepared && Values.ValueShaper.IsStructural(prepared)) ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            List<BoundColumn> result = new List<BoundColumn>();
            bool literal = property == null || property.Length == 0;
            object[] specs = literal ? ColumnResolver.Resolve(items, cmdlet, shape, out _) : property!;
            {
                foreach (object spec in specs) {
                    if (spec is string literalName && literal) {
                        if (!result.Any(c => string.Equals(c.Name, literalName, comparison)))
                            result.Add(Create(literalName, literalName, null));
                    }
                    else if (spec is string text) {
                        foreach (string matched in MatchNames(items, text, shape))
                            if (
                                !result.Any(c =>
                                    string.Equals(
                                        c.Name,
                                        matched,
                                        comparison
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
            if (result.Count > 0 && !result.Any(c => c.Column.IsKey)) {
                int key = Values.ValueShaper.TitleIndex(result.Select(c => c.Name).ToList());
                result[key >= 0 ? key : 0].Column.IsKey = true;
            }
            foreach (BoundColumn column in result)
                column.Options = shape;
            return result.AsReadOnly();
        }

        /// <summary>Evaluates a string property name or script block against one PowerShell item.</summary>
        public static object? Evaluate(PSObject item, object expression) => Evaluate(item, expression, null);

        /// <summary>
        /// Evaluates a property name or script block against one item. Names read dictionary keys, data-row columns, XML
        /// attributes and JSON fields through the shared value rules, PowerShell properties otherwise; <c>Value</c> is the
        /// item itself when it is a scalar or a collection. Getter exceptions propagate (the caller marks the cell).
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="expression">A property name or a script block.</param>
        /// <param name="options">Shape options, or null for PowerShell defaults.</param>
        /// <returns>The value.</returns>
        internal static object? Evaluate(PSObject item, object expression, Values.ShapeOptions? options) {
            if (item == null)
                throw new ArgumentNullException(nameof(item));
            if (expression is ScriptBlock script) {
                Collection<PSObject> output = script.InvokeWithContext(
                    null,
                    new List<PSVariable> { new PSVariable("_", item) }
                );
                if (output.Count > 1) {
                    // Several outputs (format views such as $_.ExportedCommands.Keys) are one collection value, joined in the cell.
                    List<object?> values = new List<object?>(output.Count);
                    foreach (PSObject piece in output)
                        values.Add(FormatViews.StripEscapes(Unwrap(piece)));
                    return values;
                }
                return output.Count == 0 ? null : FormatViews.StripEscapes(Unwrap(output[0]));
            }
            Values.ShapeOptions shape = options ?? PSShaping.Options();
            string propertyName =
                Convert.ToString(expression, CultureInfo.InvariantCulture) ?? string.Empty;
            object? prepared = Values.ValueShaper.Prepare(item, shape);
            if (string.Equals(propertyName, "Value", StringComparison.OrdinalIgnoreCase) && PSShaping.IsValueLike(item, shape))
                return prepared;
            // Dictionary keys are the data (a key named Count beats the Count property); for other data values (files, data
            // rows, XML) PowerShell's own properties come first (Mode, PSParentPath), then the shared fields.
            if (prepared is IDictionary)
                return Values.ValueShaper.TryGetField(prepared, propertyName, out object? key, shape) ? Unwrap(key) : null;
            // A dedicated source (directory entries) reads its own fields; PowerShell's adapter would bind to the directory.
            // Other names (an LDAP attribute asked for with -Property) still go to PowerShell's adapter below.
            if (prepared != null && shape.Sources.Any(s => !(s is PSObjectMemberSource) && s.CanRead(prepared)) && Values.ValueShaper.TryGetField(prepared, propertyName, out object? own, shape))
                return Unwrap(own);
            PSObject source = prepared as PSObject ?? (ReferenceEquals(prepared, item.BaseObject) ? item : PSObject.AsPSObject(prepared));
            PSPropertyInfo? property = source.Properties[propertyName];
            if (property == null || !property.IsGettable) {
                if (prepared != null && !(prepared is PSObject) && Values.ValueShaper.TryGetField(prepared, propertyName, out object? other, shape))
                    return Unwrap(other);
                return null;
            }
            return Unwrap(property.Value);
        }

        private static readonly System.Text.RegularExpressions.Regex SinglePropertyScript =
            new System.Text.RegularExpressions.Regex(@"^\s*\$(?:_|PSItem)\.(?<name>[\p{L}\p{N}_]+)\s*$", System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.CultureInvariant);

        /// <summary>
        /// The property a column expression reads, past alias properties of <paramref name="sample"/> (ProcessName on a
        /// process is an alias of Name): a property name, or a script block that only reads one property. Null for other
        /// expressions. Lets a group key and a column be compared by where their values come from, not by their names.
        /// </summary>
        /// <param name="expression">A property name, a script block, or another expression.</param>
        /// <param name="sample">An item to resolve aliases on, or null.</param>
        /// <returns>The source property name, or null.</returns>
        internal static string? SourceProperty(object? expression, PSObject? sample) {
            expression = expression is PSObject wrapped ? wrapped.BaseObject : expression;
            string? name = expression switch {
                string text => text,
                ScriptBlock script => SinglePropertyScript.Match(script.ToString()) is System.Text.RegularExpressions.Match match && match.Success ? match.Groups["name"].Value : null,
                _ => null
            };
            if (string.IsNullOrEmpty(name) || sample == null) {
                return name;
            }
            for (int hop = 0; hop < 8; hop++) {
                PSAliasProperty? alias;
                try {
                    alias = sample.Properties[name!] as PSAliasProperty;
                } catch (Exception ex) when (!(ex is PipelineStoppedException)) {
                    break;
                }
                if (alias == null || string.IsNullOrEmpty(alias.ReferencedMemberName)) {
                    break;
                }
                name = alias.ReferencedMemberName;
            }
            return name;
        }

        // A PowerShell object around a scalar, collection or data value reads as that value; PSCustomObjects and other
        // objects stay PowerShell objects so their PowerShell properties keep working.
        internal static object? Unwrap(object? value) =>
            value is PSObject wrapped ? PSObjectMemberSource.UnwrapPSObject(wrapped) : value;

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
            BoundColumn bound = new BoundColumn(name, column, expression);
            if (TryOption(options, "FormatString", out object? format) && format != null) {
                bound.FormatString = Convert.ToString(format, CultureInfo.InvariantCulture);
            }
            return bound;
        }

        /// <summary>
        /// A table column from a column hashtable (Name or Label for the header, and the column options of Show-ConsoleTable:
        /// Alignment, Width, MinWidth, MaxWidth, Flexible, Overflow, Presenter, Markup, role rules...), for tables whose rows
        /// come later, such as live tables.
        /// </summary>
        /// <param name="spec">The hashtable.</param>
        /// <returns>The column.</returns>
        internal static TableColumn ColumnFrom(IDictionary spec) => CreateCalculated(spec).Column;

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
            // MaxLines = 0 in a column hashtable keeps its meaning of "no limit"; leaving it out follows the table (3 lines, or every line with -Wrap).
            SetInt(options, "MaxLines", value => column.MaxLines = value <= 0 ? TableColumn.Unlimited : value);
            SetInt(options, "Priority", value => column.Priority = value);
            SetInt(options, "MinWidth", value => column.MinWidth = value);
            SetInt(options, "MaxWidth", value => column.MaxWidth = value);
            SetInt(options, "Width", value => column.Width = value);
            SetBool(options, "Key", value => column.IsKey = value);
            SetBool(options, "Identifier", value => column.Presenter = value ? CellPresenters.Identifier : column.Presenter);
            SetBool(options, "Flexible", value => column.Flexible = value);

            if (TryOption(options, "Presenter", out object? presenter)) {
                object? value = PSText.Unwrap(presenter);
                column.Presenter = value is ICellPresenter configured ? configured : CellPresenters.FromName(
                    Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty,
                    OptionString(options, "Unit")
                );
            }
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
            if (TryOption(options, "Role", out object? role)) {
                if (role is IDictionary single)
                    AddRole(column, single);
                else if (role is IEnumerable roles && !(role is string))
                    foreach (object? rule in roles) {
                        if (!(rule is IDictionary map))
                            throw new ArgumentException(
                                "Role must be a hashtable or an array of hashtables."
                            );
                        AddRole(column, map);
                    }
                else
                    throw new ArgumentException(
                        "Role must be a hashtable or an array of hashtables."
                    );
            }
        }

        private static void AddRole(TableColumn column, IDictionary rule) {
            object? roleValue = Option(rule, "Role");
            if (roleValue == null)
                throw new ArgumentException("Each Role rule requires a Role key.");
            ThemeToken role = ParseEnum<ThemeToken>(roleValue, "Role");
            if (TryOption(rule, "Above", out object? above))
                column.RoleRules.Add(RoleRule.Above(Number(above, "Above"), role));
            else if (TryOption(rule, "Below", out object? below))
                column.RoleRules.Add(RoleRule.Below(Number(below, "Below"), role));
            else if (TryOption(rule, "Equals", out object? equals))
                column.RoleRules.Add(RoleRule.Equal(equals, role));
            else
                throw new ArgumentException("Role rule requires Above, Below or Equals.");
        }

        private static void AddThreshold(
            IDictionary options,
            string key,
            bool above,
            ThemeToken role,
            TableColumn column
        ) {
            if (TryOption(options, key, out object? value))
                column.RoleRules.Add(
                    above
                        ? RoleRule.Above(Number(value, key), role)
                        : RoleRule.Below(Number(value, key), role)
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

        private static IEnumerable<string> MatchNames(IReadOnlyList<PSObject> items, string pattern, Values.ShapeOptions shape) {
            List<string> names = new List<string>();
            foreach (PSObject item in items) {
                object? prepared = Values.ValueShaper.Prepare(item, shape);
                bool structural = prepared != null && !(prepared is PSObject) && Values.ValueShaper.IsStructural(prepared);
                List<string> candidates = (structural
                    ? Values.ValueShaper.Fields(prepared!, shape).Select(field => field.Name)
                    : item.Properties.Where(Usable).Select(member => member.Name)).ToList();
                if (structural && pattern.IndexOfAny(new[] { '*', '?' }) < 0 && candidates.Contains(pattern, StringComparer.Ordinal)) {
                    if (!names.Contains(pattern, StringComparer.Ordinal)) {
                        names.Add(pattern);
                    }
                    continue;
                }
                foreach (string name in candidates)
                    if (Wildcard(name, pattern) && !names.Contains(name, structural ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase))
                        names.Add(name);
            }
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
