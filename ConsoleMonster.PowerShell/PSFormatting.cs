using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Management.Automation;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Data;
using ConsoleMonster.Layout;
using ConsoleMonster.PowerShell.Binding;
using ConsoleMonster.Presenters;
using ConsoleMonster.Values;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>Options for building tables (PSFormatting.BuildTable and BuildTables) beyond the columns.</summary>
    internal sealed class TableBuildOptions {
        public bool Total { get; set; }

        public bool AutoPresenters { get; set; }

        public PSCmdlet? Cmdlet { get; set; }

        public CultureInfo? Culture { get; set; }

        public bool ShowGroupColumn { get; set; }

        public bool NoSort { get; set; }
    }

    internal static class PSFormatting {
        // Record titles (with an Id after a name) are computed for record and card views up to this many rows.
        private const int TitledRows = 200;

        /// <summary>
        /// The items to show: a single collection passed as one object (<c>,$list</c>, a function that returns a list, a data
        /// table) is shown as its items; anything else as it is.
        /// </summary>
        internal static IReadOnlyList<PSObject> Expand(IReadOnlyList<PSObject> items, ShapeOptions options) {
            bool data = items.Count > 0 && items.All(item => item != null && (item.BaseObject is System.Data.DataTable || item.BaseObject is System.Data.DataSet));
            if (!data && (items.Count != 1 || items[0] == null)) {
                return items;
            }
            List<PSObject> expanded = new List<PSObject>();
            foreach (PSObject item in items) {
                object? prepared = ValueShaper.Prepare(item, options);
                if (prepared == null || ValueShaper.IsScalar(prepared) || ValueShaper.IsStructural(prepared) || !ValueShaper.TryGetItems(prepared, out System.Collections.IEnumerable? sequence)) {
                    expanded.Add(item);
                    continue;
                }
                foreach (object? child in sequence!) {
                    if (child == null) {
                        continue;
                    }
                    // A data set is its tables' rows (one table per run, since each table has its own columns).
                    if (child is System.Data.DataTable table) {
                        foreach (System.Data.DataRow row in table.Rows) {
                            expanded.Add(PSObject.AsPSObject(row));
                        }
                    } else {
                        expanded.Add(PSObject.AsPSObject(child));
                    }
                }
            }
            return expanded;
        }

        /// <summary>Consecutive objects of one kind (see <see cref="ColumnResolver.RunKey"/>): each run is its own table, as Format-Table switches format when the type changes.</summary>
        internal static List<IReadOnlyList<PSObject>> Runs(IReadOnlyList<PSObject> items, ShapeOptions options) {
            List<IReadOnlyList<PSObject>> runs = new List<IReadOnlyList<PSObject>>();
            List<PSObject>? current = null;
            string? key = null;
            foreach (PSObject item in items) {
                string itemKey = ColumnResolver.RunKey(item, options);
                if (current == null || !string.Equals(itemKey, key, StringComparison.Ordinal)) {
                    current = new List<PSObject>();
                    runs.Add(current);
                    key = itemKey;
                }
                current.Add(item);
            }
            return runs;
        }

        /// <summary>
        /// The widgets for objects: one table per run of objects of one kind (explicit properties keep one table), a single
        /// dictionary as a key/value record. Every widget gets the same configuration through <paramref name="configure"/>.
        /// </summary>
        internal static List<IWidget> BuildTables(IReadOnlyList<PSObject> items, object[]? property, object? groupBy, TableBuildOptions options, Action<Table>? configure = null) {
            ShapeOptions shape = PSShaping.Options(options.Culture);
            IReadOnlyList<PSObject> expanded = Expand(items, shape);
            List<IReadOnlyList<PSObject>> runs = property != null && property.Length > 0 ? new List<IReadOnlyList<PSObject>> { expanded } : Runs(expanded, shape);
            List<IWidget> widgets = new List<IWidget>();
            foreach (IReadOnlyList<PSObject> run in runs) {
                if (run.Count == 1 && groupBy == null && (property == null || property.Length == 0) && ValueShaper.Prepare(run[0], shape) is System.Collections.IDictionary) {
                    // The caller's table settings still apply: the configuration runs on a stand-in table (so a title meant
                    // for the first widget stays first), then its title heads the record and its caption becomes the
                    // record's summary line, as in card view. Borders, widths and row limits have no record equivalent.
                    Table settings = new Table();
                    configure?.Invoke(settings);
                    IWidget record = BuildList(run, null, null, false, 1, settings.Title, options.Culture, options.Cmdlet);
                    if (record is RecordView view && !string.IsNullOrEmpty(settings.Caption)) {
                        view.Summary = Text.Markup.Strip(settings.Caption!);
                    }
                    widgets.Add(record);
                    continue;
                }
                Table table = BuildTable(run, property, groupBy, options, shape);
                configure?.Invoke(table);
                widgets.Add(table);
            }
            return widgets;
        }

        internal static Table BuildTable(IReadOnlyList<PSObject> items, object[]? property, object? groupBy, bool total, bool autoPresenters = false, PSCmdlet? cmdlet = null, CultureInfo? culture = null) =>
            BuildTable(items, property, groupBy, new TableBuildOptions { Total = total, AutoPresenters = autoPresenters, Cmdlet = cmdlet, Culture = culture }, null);

        internal static Table BuildTable(IReadOnlyList<PSObject> items, object[]? property, object? groupBy, TableBuildOptions options, ShapeOptions? shapeOptions) {
            ShapeOptions shape = shapeOptions ?? PSShaping.Options(options.Culture);
            ShapeBudget budget = ShapeBudget.For(shape);
            ConsoleViewRegistration? view = null;
            object[]? resolved = property;
            if (resolved == null || resolved.Length == 0) {
                resolved = ColumnResolver.Resolve(items, options.Cmdlet, shape, out view);
            }
            List<BoundColumn> columns = PropertyBinder.Bind(items, resolved, shape, options.Cmdlet).ToList();
            string? groupSource = options.ShowGroupColumn || columns.Count <= 1 ? null : PropertyBinder.SourceProperty(groupBy, items.Count > 0 ? items[0] : null);
            if (groupSource != null) {
                // The group heading names the key; a column repeating it on every row adds nothing. Columns are compared by
                // where their values come from, so the process view's ProcessName (an alias of Name) goes with -GroupBy Name.
                // Aliases are resolved on the first item: a grouped table normally holds one type.
                PSObject? sample = items.Count > 0 ? items[0] : null;
                string? typed = groupBy as string;
                StringComparison comparison = sample != null && ValueShaper.Prepare(sample, shape) is object prepared && ValueShaper.IsStructural(prepared) ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
                Predicate<BoundColumn> repeatsKey = column => (typed != null && string.Equals(column.Name, typed, comparison))
                    || string.Equals(column.SourceProperty(sample), groupSource, comparison);
                if (columns.Exists(column => !repeatsKey(column))) {
                    columns.RemoveAll(repeatsKey);
                }
                if (!columns.Any(column => column.Column.IsKey) && columns.Count > 0) {
                    int key = ValueShaper.TitleIndex(columns.Select(column => column.Name).ToList());
                    columns[key >= 0 ? key : 0].Column.IsKey = true;
                }
            }
            List<object?[]> rows = new List<object?[]>(items.Count);
            foreach (PSObject item in items) {
                rows.Add(Values(columns, item, shape, budget));
            }
            ChoosePresenters(columns, rows, options.AutoPresenters);
            Table table = new Table();
            foreach (BoundColumn column in columns) {
                table.AddColumn(column.Column);
            }
            if (options.Total) {
                MarkNumericTotals(columns, rows);
                table.ShowTotals = true;
            }
            object? grouping = groupBy;
            if (view != null && property == null && grouping == null && view.Title != null) {
                // A registered view's title (files: "Directory: ..."): a shared title heads the table, different ones group it.
                string? title = SharedTitle(items, view.Title);
                if (title != null) {
                    table.Title = Text.Markup.Escape(title);
                } else {
                    grouping = view.Title;
                }
            } else if (view?.GroupBy != null && property == null && grouping == null) {
                grouping = view.GroupBy;
            }
            // Record and card headings: the key column's value (a name-like column picked by the title rule) with an Id
            // column's value after it, "pwsh (90836)". Without a name-like key column, the key cell itself is the heading.
            int keyColumn = columns.FindIndex(column => column.Column.IsKey);
            int idColumn = IdColumn(columns, keyColumn);
            bool titled = items.Count <= TitledRows && keyColumn >= 0 && idColumn >= 0 && ValueShaper.TitleIndex(columns.Select(column => column.Name).ToList()) == keyColumn;
            List<string?> titles = new List<string?>();
            if (grouping != null) {
                table.ShowSubtotals = options.Total;
                Dictionary<PSObject, int> index = new Dictionary<PSObject, int>(ReferenceComparer.Instance);
                for (int i = 0; i < items.Count; i++) {
                    index[items[i]] = i;
                }
                foreach (KeyValuePair<string, IReadOnlyList<PSObject>> group in GroupBinder.Group(items, grouping, !options.NoSort, shape)) {
                    table.AddGroup(Text.Markup.Escape(group.Key));
                    foreach (PSObject item in group.Value) {
                        table.AddValues(rows[index[item]]);
                        if (titled) {
                            titles.Add(KeyTitle(rows[index[item]][keyColumn], rows[index[item]][idColumn]));
                        }
                    }
                }
            } else {
                for (int i = 0; i < items.Count; i++) {
                    table.AddValues(rows[i]);
                    if (titled) {
                        titles.Add(KeyTitle(rows[i][keyColumn], rows[i][idColumn]));
                    }
                }
            }
            if (titled) {
                table.RowTitles = titles;
            }
            return table;
        }

        // A record or card title: the shared title rule (pwsh (90836)); values have none.
        private static readonly string[] IdNames = { "Id", "ProcessId", "PID" };

        // The first Id-like column other than the key, or -1.
        private static int IdColumn(IReadOnlyList<BoundColumn> columns, int keyColumn) {
            foreach (string name in IdNames) {
                for (int c = 0; c < columns.Count; c++) {
                    if (c != keyColumn && string.Equals(columns[c].Name, name, StringComparison.OrdinalIgnoreCase)) {
                        return c;
                    }
                }
            }
            return -1;
        }

        // "pwsh (90836)" from the key cell and the Id cell (already read, so no getter runs again); null when the key is empty.
        private static string? KeyTitle(object? key, object? id, ShapeOptions? shape = null, ShapeBudget? budget = null) {
            string keyText = CellText(key, shape, budget);
            if (keyText.Length == 0) {
                return null;
            }
            string idText = id is TableCell || id is FieldError || id is BudgetExceeded ? string.Empty : Convert.ToString(id is PSObject wrapped ? wrapped.BaseObject : id, CultureInfo.InvariantCulture) ?? string.Empty;
            return idText.Length == 0 || string.Equals(idText, keyText, StringComparison.OrdinalIgnoreCase) ? keyText : keyText + " (" + idText + ")";
        }

        private static string CellText(object? value, ShapeOptions? shape, ShapeBudget? budget) {
            switch (value) {
                case null:
                    return string.Empty;
                case TableCell cell:
                    return Text.Markup.Strip(cell.Markup);
                case Text.RichText rich:
                    return Text.Markup.Strip(rich.ToMarkup());
                default:
                    // A name that is a list or a record (JSON, nested objects) reads as its joined items or its own title.
                    return ValueShaper.IsScalar(value) ? ValueShaper.ScalarText(value) : ValueShaper.Text(value, shape ?? PSShaping.Options(), budget);
            }
        }

        // Identifier numbers (Id, PID, Port, Index, Year...) never get thousands separators; with autoPresenters, presenters
        // by member name and type too (bytes for Length, dates, durations, status-like enums). Columns with a presenter only
        // receive values the presenter can format.
        private static void ChoosePresenters(List<BoundColumn> columns, List<object?[]> rows, bool autoPresenters) {
            for (int c = 0; c < columns.Count; c++) {
                TableColumn column = columns[c].Column;
                object? sample = null;
                foreach (object?[] row in rows) {
                    if (row[c] != null && !(row[c] is TableCell)) {
                        sample = row[c];
                        break;
                    }
                }
                if (sample == null || column.Presenter != null) {
                    continue;
                }
                if (IdentifierPresenter.LooksLikeIdentifier(columns[c].Name) && CellPresenters.ForMember(columns[c].Name, sample.GetType()) == CellPresenters.Identifier) {
                    column.Presenter = CellPresenters.Identifier;
                } else if (CellPresenters.ForStatus(columns[c].Name, sample) is ICellPresenter status) {
                    // Status columns get markers in every command, not only in Out-ConsoleDefault's views.
                    column.Presenter = status;
                } else if (autoPresenters) {
                    column.Presenter = CellPresenters.ForMember(columns[c].Name, sample.GetType());
                }
            }
        }

        internal static IWidget BuildList(IReadOnlyList<PSObject> items, object[]? property, object? groupBy, bool showEmpty, int depth, string? title, CultureInfo? culture = null, PSCmdlet? cmdlet = null) {
            CultureInfo resolved = ResolveCulture(culture);
            ShapeOptions shape = PSShaping.Options(culture);
            ShapeBudget budget = ShapeBudget.For(shape);
            IReadOnlyList<PSObject> expanded = Expand(items, shape);
            if (expanded.Count > 0 && expanded.All(item => PSShaping.IsValueLike(item, shape))) {
                // Values have no fields: one line each, as text.
                string lines = string.Join("\n", expanded.Select(item => Text.Markup.Escape(ValueShaper.Text(item, shape, ShapeBudget.For(shape)))));
                return title == null ? new TextBlock(lines) : (IWidget)new Rows(new TextBlock(title), new TextBlock(lines));
            }
            RecordView view = new RecordView(title) { HideEmpty = !showEmpty };
            if (expanded.Count > 1 || groupBy != null) {
                view.Summary = expanded.Count.ToString("N0", resolved) + (expanded.Count == 1 ? " record" : " records");
            }
            List<IReadOnlyList<PSObject>> runs = property != null && property.Length > 0 ? new List<IReadOnlyList<PSObject>> { expanded } : Runs(expanded, shape);
            foreach (IReadOnlyList<PSObject> run in runs) {
                object[]? specs = property;
                if (specs == null || specs.Length == 0) {
                    specs = ColumnResolver.Resolve(run, cmdlet, shape, out _);
                }
                IReadOnlyList<BoundColumn> columns = PropertyBinder.Bind(run, specs, shape, cmdlet);
                // The heading comes from a name-like column (Name, DisplayName, ...) with an Id column after it; that column is
                // not repeated below the heading. Without a name-like column the heading is the record's title (or type) and
                // every column is listed.
                int key = ValueShaper.TitleIndex(columns.Select(column => column.Name).ToList());
                if (groupBy != null) {
                    foreach (KeyValuePair<string, IReadOnlyList<PSObject>> group in GroupBinder.Group(run, groupBy, true, shape)) {
                        view.AddSection(Text.Markup.Escape(group.Key));
                        foreach (PSObject item in group.Value) {
                            AddRecord(view, columns, key, item, 1, depth, true, shape, budget);
                        }
                    }
                } else {
                    // A lone dictionary without a caller's title shows its entries with no heading, as PowerShell shows a
                    // hashtable: every key, a Name key included, is a field (otherwise the heading would be its type name or a
                    // key's value).
                    bool lone = expanded.Count == 1 && title == null && ValueShaper.Prepare(run[0], shape) is System.Collections.IDictionary;
                    foreach (PSObject item in run) {
                        AddRecord(view, columns, key, item, 0, depth, (expanded.Count > 1 || title == null) && !lone, shape, budget);
                    }
                }
            }
            return view;
        }

        internal static InspectorOptions InspectorOptions(int depth, int maxItems, string[]? include, string[]? exclude, int maxNodes = 5000, object? root = null) {
            // PowerShell objects around dictionaries, collections, scalars and data values inspect as those; other objects
            // through their PowerShell properties (nested ones show only their default display set).
            InspectorOptions options = new InspectorOptions {
                MaxDepth = depth,
                MaxItems = maxItems,
                MaxNodes = maxNodes,
                Unwrap = PSObjectMemberSource.UnwrapPSObject,
                Identity = PSObjectMemberSource.Identity
            };
            options.Sources.Add(new DirectoryEntryMemberSource());
            options.Sources.Add(new PSObjectMemberSource { Root = root });
            if (include != null) {
                foreach (string name in include) {
                    options.Include.Add(name);
                }
            }
            if (exclude != null) {
                foreach (string name in exclude) {
                    options.Exclude.Add(name);
                }
            }
            return options;
        }

        internal static CultureInfo ResolveCulture(CultureInfo? culture) {
            return culture ?? Terminal.Profile.Culture ?? CultureInfo.CurrentCulture;
        }

        internal static void Write(PSCmdlet cmdlet, IWidget widget, int width, CultureInfo? culture = null) {
            TerminalProfile profile = Terminal.GetCurrentProfile();
            if (culture != null) {
                profile = profile.WithCulture(culture);
            }
            if (width > 0) {
                profile = profile.WithSize(width, profile.Height);
            }
            // A blank line after each block, as Format-Table does, so consecutive outputs stay apart.
            HostOutput.Write(cmdlet, new Rows(widget, new TextBlock(string.Empty)), profile, width > 0);
        }

        /// <summary>
        /// The output of a Format-Console* command, so every PowerShell consumer gets it:
        /// <list type="bullet">
        /// <item>-AsString: one string of plain text (colour only with $PSStyle.OutputRendering Ansi);</item>
        /// <item>followed by another command (| Out-String, | Out-File, | Set-Content, | more): the rendered lines as strings;</item>
        /// <item>PowerShell 7.2+ on a local console, typed at the prompt (or run by -Command) and last in its pipeline: a
        /// <see cref="ConsoleRendering"/> object that ConsoleMonster's format view draws in colour, while $x = ..., &gt; file,
        /// -OutVariable, *&gt; and Out-String get plain text through $PSStyle.OutputRendering;</item>
        /// <item>otherwise (inside a script or function, Windows PowerShell 5.1, jobs, remoting, other hosts,
        /// Use-ConsoleTarget): drawn through the host as before, so a function's output never gains a table object; use
        /// -AsString to capture there.</item>
        /// </list>
        /// With -PassThru the view is drawn through the host and the input objects are written instead; -AsString and
        /// -PassThru together are refused, as one stream would mix text and objects.
        /// </summary>
        internal static void Emit(PSCmdlet cmdlet, IWidget widget, int width, CultureInfo? culture, bool asString, bool passThru = false) {
            if (asString && passThru) {
                cmdlet.ThrowTerminatingError(new ErrorRecord(
                    new ArgumentException("-AsString and -PassThru cannot be used together: -AsString writes the view as text, -PassThru writes the input objects."),
                    "AsStringWithPassThru", ErrorCategory.InvalidArgument, null));
            }
            if (asString) {
                cmdlet.WriteObject(CaptureText(cmdlet, widget, width, culture));
                return;
            }
            if (!passThru && cmdlet.MyInvocation.PipelinePosition < cmdlet.MyInvocation.PipelineLength) {
                foreach (string line in CaptureText(cmdlet, widget, width, culture).Split('\n')) {
                    cmdlet.WriteObject(line);
                }
                return;
            }
            // PowerShell's formatter wraps format-view output at the host's width: an explicit -Width wider than the host
            // keeps the host route, so the requested layout is not broken up.
            int hostWidth = WidgetFormatter.HostWidth(cmdlet.Host);
            bool fits = width <= 0 || hostWidth <= 0 || width <= hostWidth;
            if (!passThru && fits && EmitsRendering(cmdlet)) {
                // A blank line after the block, as Format-Table and the host route print.
                cmdlet.WriteObject(new ConsoleRendering(new Rows(widget, new TextBlock(string.Empty)), width, culture));
                return;
            }
            Write(cmdlet, widget, width, culture);
        }

        // Text for a capturing consumer: plain unless $PSStyle.OutputRendering is Ansi.
        private static string CaptureText(PSCmdlet cmdlet, IWidget widget, int width, CultureInfo? culture) =>
            WidgetFormatter.Render(widget, cmdlet.Host, string.Equals(OutputRendering(cmdlet), "Ansi", StringComparison.OrdinalIgnoreCase) ? "Ansi" : "PlainText", width, culture);

        // $PSStyle.OutputRendering as text (PowerShell 7.2+), or null without $PSStyle (Windows PowerShell 5.1, 7.0, 7.1).
        private static string? OutputRendering(PSCmdlet cmdlet) {
            try {
                object? style = cmdlet.SessionState.PSVariable.GetValue("PSStyle");
                return style == null ? null : Convert.ToString(PSObject.AsPSObject(style).Properties["OutputRendering"]?.Value, CultureInfo.InvariantCulture);
            } catch (Exception ex) when (!(ex is PipelineStoppedException)) {
                return null;
            }
        }

        // PowerShell 7.2+ ($PSStyle strips colour for captured output) on a local interactive host, output not sent
        // elsewhere by Use-ConsoleTarget. Jobs and remoting keep the host route (an object would be serialized there).
        // Only a command typed at the prompt (CommandOrigin Runspace) writes the object: inside a script, function or
        // script block the output belongs to the caller ($x = Get-Report, $null = Show-Report, if (Show-Report)), so the
        // view is drawn on the screen as before.
        private static bool EmitsRendering(PSCmdlet cmdlet) {
            if (cmdlet.MyInvocation.CommandOrigin != CommandOrigin.Runspace) {
                return false;
            }
            if (OutputRendering(cmdlet) == null || !Terminal.IsConsole(Terminal.Target)) {
                return false;
            }
            string host = cmdlet.Host?.Name ?? string.Empty;
            return string.Equals(host, "ConsoleHost", StringComparison.Ordinal) || host.IndexOf("Visual Studio Code", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// The number of members a .NET object shows through the shared value rules, for the default tree depth: values,
        /// dictionaries and PSCustomObjects (parsed JSON, configuration) count as 0, so data keeps its full default depth.
        /// </summary>
        internal static int FieldCount(PSObject item) {
            ShapeOptions shape = Binding.PSShaping.Options();
            if (Binding.PSShaping.IsValueLike(item, shape) || item.BaseObject is PSCustomObject) {
                return 0;
            }
            object? prepared = ValueShaper.Prepare(item, shape);
            if (prepared is System.Collections.IDictionary) {
                return 0;
            }
            return prepared == null ? 0 : ValueShaper.Fields(prepared, shape).Count;
        }

        /// <summary>
        /// The view of a script's output, shaped as the Format-Console commands shape it: widgets as they are, text as lines,
        /// objects as tables (one object stays a one-row table, so a watched view keeps its shape), one dictionary as a
        /// record; an empty output is an empty line.
        /// </summary>
        internal static IWidget ViewOf(IReadOnlyList<PSObject> output, PSCmdlet cmdlet) {
            List<IWidget> parts = new List<IWidget>();
            List<PSObject> objects = new List<PSObject>();
            System.Text.StringBuilder text = new System.Text.StringBuilder();
            void Flush() {
                if (text.Length > 0) {
                    parts.Add(new TextBlock(Text.Markup.Escape(text.ToString().TrimEnd('\n'))));
                    text.Clear();
                }
                if (objects.Count > 0) {
                    parts.AddRange(BuildTables(objects.ToArray(), null, null, new TableBuildOptions { Cmdlet = cmdlet }, table => table.RecordViewRows = 0));
                    objects.Clear();
                }
            }
            foreach (PSObject item in output) {
                if (item == null) {
                    continue;
                }
                if (item.BaseObject is IWidget widget) {
                    Flush();
                    parts.Add(widget);
                } else if (item.BaseObject is string line) {
                    if (objects.Count > 0) {
                        Flush();
                    }
                    text.Append(line).Append('\n');
                } else {
                    if (text.Length > 0) {
                        Flush();
                    }
                    objects.Add(item);
                }
            }
            Flush();
            return parts.Count == 0 ? new TextBlock(string.Empty) : Combine(parts);
        }

        /// <summary>Several widgets as one: a single one as it is, more with a blank line between them.</summary>
        internal static IWidget Combine(IReadOnlyList<IWidget> widgets) =>
            widgets.Count == 1 ? widgets[0] : new Rows(widgets.ToArray()) { Gap = 1 };

        private static void AddRecord(RecordView view, IReadOnlyList<BoundColumn> columns, int key, PSObject item, int level, int depth, bool heading, ShapeOptions shape, ShapeBudget budget) {
            int fieldLevel = level;
            // Cell text and titles get an allowance per record; slow getters are guarded across the whole list.
            ShapeBudget record = ShapeBudget.For(shape);
            object?[] values = new object?[columns.Count];
            for (int c = 0; c < columns.Count; c++) {
                values[c] = Read(columns[c], item, budget);
            }
            // A key whose getter failed is not the heading: it stays a field, so its warning marker shows.
            if (key >= 0 && (values[key] is FieldError || values[key] is BudgetExceeded)) {
                key = -1;
            }
            if (heading) {
                string? title = key >= 0 ? KeyTitle(ValueShaper.Prepare(values[key], shape), IdValue(columns, key, values), shape, record) : null;
                view.AddHeading(Text.Markup.Escape(title ?? PSShaping.Title(item, shape, record)), level);
                fieldLevel = level + 1;
            }
            int shown = 0;
            int visible = 0;
            for (int c = 0; c < columns.Count; c++) {
                if (heading && c == key) {
                    continue;
                }
                object? value = values[c];
                TableColumn column = columns[c].Column;
                string label = Text.Markup.Strip(column.Header);
                shown++;
                if (!PresenterValues.IsEmpty(value is PSObject wrapped ? wrapped.BaseObject : value)) {
                    visible++;
                }
                if (value is FieldError error) {
                    view.AddFieldAt(fieldLevel, label, TableCell.ForStatus(Styling.StatusKind.Warn, error.Message));
                    continue;
                }
                if (value is BudgetExceeded) {
                    view.AddFieldAt(fieldLevel, label, new TableCell("[muted]…[/]"));
                    continue;
                }
                object? prepared = ValueShaper.Prepare(value, shape);
                if (depth > 0 && prepared != null && column.Presenter == null && NeedsNesting(prepared, shape)) {
                    view.AddHeading(Text.Markup.Escape(label), fieldLevel);
                    view.AddWidgetAt(fieldLevel + 1, InspectorBody(value!, depth));
                    continue;
                }
                object? cell = column.Presenter != null && PSShaping.IsPresentable(prepared) ? prepared : PSShaping.Cell(value, column, shape, record);
                // Status fields get their marker in records too, as status columns do in tables.
                view.AddFieldAt(fieldLevel, label, cell as TableCell ?? TableCell.FromValue(cell), column.Presenter ?? CellPresenters.ForStatus(columns[c].Name, prepared));
            }
            if (shown == 0) {
                view.AddFieldAt(fieldLevel, "fields", new TableCell("[muted]none[/]"));
            } else if (visible == 0 && view.HideEmpty) {
                // Every field is empty: the record still shows (its heading would otherwise vanish with its empty fields).
                view.AddFieldAt(fieldLevel, "fields", new TableCell("[muted]all empty[/]"));
            }
        }

        private static object? IdValue(IReadOnlyList<BoundColumn> columns, int key, object?[] values) {
            int id = IdColumn(columns, key);
            return id >= 0 ? ValueShaper.Prepare(values[id]) : null;
        }

        // Nested records and collections of records expand under their label; collections of values stay one joined line.
        private static bool NeedsNesting(object prepared, ShapeOptions shape) {
            if (ValueShaper.IsScalar(prepared)) {
                return false;
            }
            if (!ValueShaper.IsStructural(prepared) && ValueShaper.TryGetItems(prepared, out System.Collections.IEnumerable? items)) {
                foreach (object? item in items!) {
                    return item != null && !PSShaping.IsValueLike(item, shape);
                }
                return false;
            }
            return true;
        }

        // A nested inspector without its own title line (the property name is the heading).
#pragma warning disable IL2026
        private static IWidget InspectorBody(object value, int depth) {
            InspectNode node = ObjectInspector.Inspect(value, InspectorOptions(depth, 25, null, null));
            return new InspectorView(node) { ShowHeader = false };
        }
#pragma warning restore IL2026

        private static object?[] Values(IReadOnlyList<BoundColumn> columns, PSObject item, ShapeOptions shape, ShapeBudget budget) {
            object?[] values = new object?[columns.Count];
            // Cell text (joined collections, titles) gets an allowance per row, so a large table is never cut short; slow
            // getters are guarded across the whole table.
            ShapeBudget row = ShapeBudget.For(shape);
            for (int c = 0; c < columns.Count; c++) {
                object? value = Read(columns[c], item, budget);
                object? prepared = value is FieldError || value is BudgetExceeded ? value : ValueShaper.Prepare(value, shape);
                values[c] = columns[c].Column.Presenter != null && PSShaping.IsPresentable(prepared) ? prepared : PSShaping.Cell(value, columns[c].Column, shape, row);
            }
            return values;
        }

        /// <summary>One column of one item, guarded: a column whose getter keeps stalling (a remote object) stops being read once the stalls use up the budget.</summary>
        internal static object? Read(BoundColumn column, PSObject item, ShapeBudget budget) => budget.Guard(column, () => column.GetValue(item));


        // The title every object shares, or null when they differ.
        private static string? SharedTitle(IReadOnlyList<PSObject> items, ScriptBlock title) {
            string? shared = null;
            foreach (PSObject item in items) {
                string text = string.Join(" ", title.InvokeWithContext(null, new List<PSVariable> { new PSVariable("_", item) }));
                if (shared == null) {
                    shared = text;
                } else if (!string.Equals(shared, text, StringComparison.Ordinal)) {
                    return null;
                }
            }
            return shared;
        }

        // -Total sums numeric columns that have no explicit aggregate (the key column labels the row).
        private static void MarkNumericTotals(IReadOnlyList<BoundColumn> columns, List<object?[]> rows) {
            for (int c = 0; c < columns.Count; c++) {
                TableColumn column = columns[c].Column;
                if (column.IsKey || column.Aggregate != ColumnAggregate.None || column.Presenter is IdentifierPresenter) {
                    continue;
                }
                bool numeric = false;
                bool other = false;
                foreach (object?[] row in rows) {
                    object? value = row[c];
                    if (PresenterValues.IsEmpty(value)) {
                        continue;
                    }
                    if (PresenterValues.TryGetNumber(value, out _) && !(value is string)) {
                        numeric = true;
                    } else {
                        other = true;
                        break;
                    }
                }
                if (numeric && !other) {
                    column.Aggregate = ColumnAggregate.Sum;
                }
            }
        }

        private sealed class ReferenceComparer : IEqualityComparer<PSObject> {
            public static readonly ReferenceComparer Instance = new ReferenceComparer();

            public bool Equals(PSObject? x, PSObject? y) => ReferenceEquals(x, y);

            public int GetHashCode(PSObject obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
        }
    }
}
