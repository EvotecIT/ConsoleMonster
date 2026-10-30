using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Widgets;
using ConsoleMonster.Interactive;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Creates a console table from pipeline objects.</para>
    /// <para type="description">Columns come from -Property or from the first object's properties (or hashtable keys). Property descriptors accept presenter names or configured C# ICellPresenter instances. Columns size to their content, numbers are right-aligned, long values wrap. A -StatusProperty column shows status markers (pass, fail, warn...) as the row's one colour signal.</para>
    /// </summary>
    /// <example>
    /// <para>Build a table with a status column</para>
    /// <code>@(
    ///     [pscustomobject]@{ Name = 'web01'; Status = 'Pass'; Region = 'East' }
    ///     [pscustomobject]@{ Name = 'web02'; Status = 'Warn'; Region = 'West' }
    /// ) | New-ConsoleTable -Property Name, Status, Region -StatusProperty Status -Title 'Servers' | Show-Console</code>
    /// </example>
    /// <example>
    /// <para>Filter objects from current application state</para>
    /// <code>$items = @(
    ///     [pscustomobject]@{ Name = 'web01'; Status = 'Pass' }
    ///     [pscustomobject]@{ Name = 'web02'; Status = 'Warn' }
    /// )
    /// Show-ConsoleApp -Content {
    ///     New-ConsoleTextInput -Key Query
    ///     New-ConsoleTable -Bind { $items | Where-Object Name -Like "*$($State.Query)*" } -Property Name, Status -StatusProperty Status
    /// }</code>
    /// </example>
    [Cmdlet(VerbsCommon.New, "ConsoleTable", DefaultParameterSetName = "Objects")]
    [OutputType(typeof(Table), typeof(AppBinding))]
    public partial class CmdletNewConsoleTable : TableConfigurationCmdlet {
        private readonly List<PSObject> _items = new List<PSObject>();

        /// <summary><para type="description">The objects to show, one row each.</para></summary>
        [Parameter(ValueFromPipeline = true, Position = 0, ParameterSetName = "Objects")]
        [Parameter(ValueFromPipeline = true, Position = 1, ParameterSetName = "Live")]
        public PSObject[]? InputObject { get; set; }

        /// <summary><para type="description">Application-dependent object producer. Receives param($State) and injected $State as a native hashtable on the owning input thread. Emit objects, not widgets; columns and formatting use the same builder as ordinary tables.</para></summary>
        [Parameter(Mandatory = true, ParameterSetName = "Bind")]
        public ScriptBlock? Bind { get; set; }

        /// <summary><para type="description">Properties to show, in order.</para></summary>
        [Parameter]
        public object[]? Property { get; set; }

        /// <summary><para type="description">Groups rows by a property or script block.</para></summary>
        [Parameter] public object? GroupBy { get; set; }
        /// <summary><para type="description">Adds totals for numeric columns.</para></summary>
        [Parameter] public SwitchParameter ShowTotals { get; set; }
        /// <summary><para type="description">Uses cards below this width; zero keeps a grid.</para></summary>
        [Parameter, ValidateRange(0, int.MaxValue)] public int CardsBelow { get; set; } = 60;
        /// <summary><para type="description">Keeps groups in input order.</para></summary>
        [Parameter] public SwitchParameter NoSort { get; set; }
        /// <summary><para type="description">Wraps long cell values without limiting their line count.</para></summary>
        [Parameter] public SwitchParameter Wrap { get; set; }
        /// <summary><para type="description">Keeps the grouping property in the table columns.</para></summary>
        [Parameter] public SwitchParameter ShowGroupColumn { get; set; }

        /// <summary><para type="description">A property whose values are statuses (pass/fail/warn/skip/running).</para></summary>
        [Parameter]
        public string? StatusProperty { get; set; }

        /// <summary><para type="description">A title above the table.</para></summary>
        [Parameter]
        public object? Title { get; set; }

        /// <summary><para type="description">A muted caption below the table.</para></summary>
        [Parameter]
        public object? Caption { get; set; }

        /// <summary><para type="description">Border: Rounded, Square, Double, Minimal or None.</para></summary>
        [Parameter]
        public TableBorder Border { get; set; } = TableBorder.Rounded;

        /// <summary><para type="description">Underline the header instead of drawing a separator row under it.</para></summary>
        [Parameter]
        public SwitchParameter NoHeaderSeparator { get; set; }

        /// <summary><para type="description">Fill the available width. The text columns share the spare width in proportion to their widths, while number, date and status columns keep their natural width; a column hashtable's Weight (or Flexible) decides the share instead, and Width or MaxWidth holds.</para></summary>
        [Parameter]
        public SwitchParameter Expand { get; set; }

        /// <summary><para type="description">Creates a keyed live table using the same column and row configuration.</para></summary>
        [Parameter(Mandatory = true, ParameterSetName = "Live")] public SwitchParameter Live { get; set; }
        /// <summary><para type="description">The live widget identifier; default table.</para></summary>
        [Parameter(Position = 0, ParameterSetName = "Live")] public string WidgetId { get; set; } = "table";
        /// <summary><para type="description">For Live, the input property used as the stable row key; omitted uses the first column.</para></summary>
        [Parameter(ParameterSetName = "Live")] public string? KeyProperty { get; set; }
        /// <summary><para type="description">For Live, a table model supplying columns and presentation options.</para></summary>
        [Parameter(ParameterSetName = "Live")] public Table? Table { get; set; }
        /// <summary><para type="description">For Live, keeps this many recent rows; zero keeps all.</para></summary>
        [Parameter, ValidateRange(0, int.MaxValue)] public int MaxRows { get; set; }
        /// <summary><para type="description">For Live, the column index or header used for sorting.</para></summary>
        [Parameter(ParameterSetName = "Live")] public object? SortBy { get; set; }
        /// <summary><para type="description">For Live, sorts larger values first.</para></summary>
        [Parameter(ParameterSetName = "Live")] public SwitchParameter Descending { get; set; }

        /// <inheritdoc />
        protected override void ProcessRecord() {
            if (InputObject != null) {
                _items.AddRange(InputObject);
            }
        }

        /// <inheritdoc />
        protected override void EndProcessing() {
            if (Bind != null) {
                PSAppScript script = new PSAppScript(Bind);
                WriteObject(script.CreateBinding(state => BuildTable(script.Invoke(state), script.Cmdlet)));
                return;
            }
            if (Property != null && Table != null) { throw new PSArgumentException("Use Property or Table, not both."); }
            var boundRows = Live.IsPresent && Table == null ? new List<(PSObject Input, object?[] Values, object? Identity)>() : null;
            Table table = Table ?? BuildTable(_items, boundRows: boundRows);
            if (Live.IsPresent) {
                if (table.Columns.Count == 0) { throw new PSArgumentException("Live tables require columns or input objects that define them."); }
                var live = new LiveTable(table, WidgetId) { MaxRows = MaxRows, SortDescending = Descending.IsPresent, KeyProperty = KeyProperty };
                if (SortBy != null) { live.SortColumn = ResolveColumn(table, PSWidgets.ToText(SortBy)); }
                if (boundRows == null) { PSItems.AddAll(this, live, _items, KeyProperty); }
                else {
                    foreach (var row in boundRows) {
                        object? key = row.Identity;
                        if (KeyProperty != null && (!Values.ValueShaper.TryGetField(row.Input, KeyProperty, out key, Binding.PSShaping.Options()) || key == null || key is Values.FieldError)) {
                            throw new PSArgumentException("The row has no '" + KeyProperty + "' value to use as its key.");
                        }
                        if (key == null || key is Values.FieldError || key is Values.BudgetExceeded) { throw new PSArgumentException("The row needs an unformatted first-column value or an explicit KeyProperty for its stable key."); }
                        if (key is TableCell cell) { key = cell.HasValue ? cell.Value : Text.Markup.Strip(cell.Markup); }
                        live.Set(PSWidgets.ToText(key), row.Values);
                    }
                }
                WriteObject(live);
            } else { Emit(table); }
        }

        /// <summary>Builds the table from the collected input.</summary>
        /// <returns>The table.</returns>
        protected Table BuildTable() => BuildTable(_items);

        private Table BuildTable(IReadOnlyList<PSObject> items, PSCmdlet? cmdlet = null, List<(PSObject Input, object?[] Values, object? Identity)>? boundRows = null) {
            // The same columns and cell values as Show-ConsoleTable (registered views, PowerShell's table views, data keys,
            // shared value rules); -StatusProperty shows that column as status markers.
            object[]? property = Property != null && Property.Length > 0 ? Property : null;
            Values.ShapeOptions shape = Binding.PSShaping.Options();
            Table table;
            if (items.Count == 0 && property != null) {
                table = new Table();
                foreach (object descriptor in property) {
                    object? column = PSText.Unwrap(descriptor);
                    table.AddColumn(column is System.Collections.IDictionary record ? Binding.PropertyBinder.ColumnFrom(record) : new TableColumn(Text.Markup.Escape(PSWidgets.ToText(column))));
                }
            } else { table = ConsoleHostViews.TableModelCore(items, new ConsoleTableViewOptions { Property = property, GroupBy = GroupBy, ShowTotals = ShowTotals.IsPresent, NoSort = NoSort.IsPresent, ShowGroupColumn = ShowGroupColumn.IsPresent }, cmdlet ?? this,
                boundRows == null ? null : (row, values, identity) => boundRows.Add((row, values, identity))); }
            if (StatusProperty != null) {
                foreach (TableColumn column in table.Columns) {
                    if (string.Equals(column.Key, StatusProperty, System.StringComparison.OrdinalIgnoreCase) || string.Equals(Text.Markup.Strip(column.Header), StatusProperty, System.StringComparison.OrdinalIgnoreCase)) {
                        column.Presenter = Presenters.CellPresenters.FromName("StatusWord", null);
                    }
                }
            }
            if (Title != null) {
                table.Title = PSText.Markup(Title);
            }
            table.MaxRows = MaxRows;
            table.Caption = PSText.Markup(Caption);
            table.Border = Border;
            table.Expand = Expand.IsPresent;
            table.HeaderSeparator = !NoHeaderSeparator.IsPresent;
            table.CardsBelow = CardsBelow;
            if (Wrap.IsPresent) { table.WrapLines = 0; }
            ApplyTableConfiguration(table);
            return table;
        }

        private static int ResolveColumn(Table table, string name) {
            if (int.TryParse(name, out int index)) {
                if (index < 0 || index >= table.Columns.Count) { throw new PSArgumentOutOfRangeException(nameof(SortBy)); }
                return index;
            }
            for (int i = 0; i < table.Columns.Count; i++) {
                if (string.Equals(Text.Markup.Strip(table.Columns[i].Header), name, System.StringComparison.OrdinalIgnoreCase)) { return i; }
            }
            throw new PSArgumentException("The table has no column '" + name + "'.");
        }

        /// <summary>Emits the built table (Show-ConsoleTable overrides this to write it).</summary>
        /// <param name="table">The table.</param>
        protected virtual void Emit(Table table) => WriteObject(table);


    }
}
