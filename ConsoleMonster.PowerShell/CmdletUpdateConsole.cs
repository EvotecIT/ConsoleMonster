using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Management.Automation;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;

namespace ConsoleMonster.PowerShell {
    /// <summary>Updates a live widget or a caller-managed run through one validated operation.</summary>
    /// <remarks>
    /// <para>For a dashboard, pass the WidgetId assigned when the widget was built. Inside Invoke-ConsoleDashboard the command uses the current session. To update a caller-managed run, pass the handle returned by Start-ConsoleRun.</para>
    /// <para>The selected parameter set determines what the update addresses. Key identifies a row for RemoveRow. Row takes a record and projects it through the live table's KeyProperty and columns. Index is an integer array in both the Cells and Run sets. The Cells set applies one status to all the specified zero-based cell positions. The Run set uses exactly one entry in that array as the zero-based item position in the run's input.</para>
    /// <para>Fields are checked against the resolved widget before any events are posted. Unknown targets and unsupported fields are terminating errors.</para>
    /// </remarks>
    /// <example>
    /// <para>Advance a progress widget and change its label. The WidgetId in the update matches the WidgetId on New-ConsoleProgress. Value sets the absolute progress to 5 out of the 20 steps already configured on the widget. The dashboard runner supplies the session.</para>
    /// <code>$dashboardOptions = @{
    ///     Title = 'Build'
    ///     Content = {
    ///         New-ConsoleProgress -WidgetId 'build' -Total 20
    ///     }
    /// }
    /// $dashboard = New-ConsoleDashboard @dashboardOptions
    /// $invokeOptions = @{
    ///     Dashboard = $dashboard
    ///     ScriptBlock = {
    ///         $update = @{
    ///             WidgetId = 'build'
    ///             Value = 5
    ///             Label = 'Compile'
    ///         }
    ///         Update-Console @update
    ///     }
    /// }
    /// Invoke-ConsoleDashboard @invokeOptions</code>
    /// </example>
    /// <example>
    /// <para>Add a live table row, then replace it. Property declares the display columns in order. KeyProperty selects Server as the stable row identity. The two Row records both have Server set to web01, so the second update changes that row to Pass with 12 milliseconds.</para>
    /// <code>$dashboardOptions = @{
    ///     Title = 'Results'
    ///     Content = {
    ///         $tableOptions = @{
    ///             WidgetId = 'results'
    ///             Live = $true
    ///             Property = @('Server', 'State', 'Ms')
    ///             KeyProperty = 'Server'
    ///         }
    ///         New-ConsoleTable @tableOptions
    ///     }
    /// }
    /// $dashboard = New-ConsoleDashboard @dashboardOptions
    /// $invokeOptions = @{
    ///     Dashboard = $dashboard
    ///     ScriptBlock = {
    ///         $update = @{
    ///             WidgetId = 'results'
    ///             Row = [pscustomobject]@{ Server = 'web01'; State = 'Running'; Ms = 0 }
    ///         }
    ///         Update-Console @update
    ///
    ///         $update.Row = [pscustomobject]@{ Server = 'web01'; State = 'Pass'; Ms = 12 }
    ///         Update-Console @update
    ///     }
    /// }
    /// Invoke-ConsoleDashboard @invokeOptions</code>
    /// </example>
    /// <example>
    /// <para>Set two cells in a dashboard to Pass. This example uses the grid arrangement of a cell map, as in the parallel-patch gallery demo. A four-cell map with two columns places zero-based positions 0 and 1 in the first row. The update applies the same status to both cells and leaves the second row unchanged.</para>
    /// <code>$dashboardOptions = @{
    ///     Title = 'Checks'
    ///     Content = {
    ///         $mapOptions = @{
    ///             WidgetId = 'checks'
    ///             Count = 4
    ///             Columns = 2
    ///             RowLabels = @('web01', 'web02')
    ///         }
    ///         New-ConsoleCellMap @mapOptions
    ///     }
    /// }
    /// $dashboard = New-ConsoleDashboard @dashboardOptions
    /// $invokeOptions = @{
    ///     Dashboard = $dashboard
    ///     ScriptBlock = {
    ///         $update = @{
    ///             WidgetId = 'checks'
    ///             Index = @(0, 1)
    ///             Status = 'Pass'
    ///         }
    ///         Update-Console @update
    ///     }
    /// }
    /// Invoke-ConsoleDashboard @invokeOptions</code>
    /// </example>
    /// <example>
    /// <para>Finish one caller-managed run item. Start-ConsoleRun receives an array containing one input record. The Run parameter set of Update-Console requires a one-element integer array for Index. Its value, 0, addresses the first input record, Report. The example records the result of a simulated upload and then completes the run.</para>
    /// <code>$items = @(
    ///     [pscustomobject]@{ Name = 'Report' }
    /// )
    /// $runOptions = @{
    ///     InputObject = $items
    ///     Label = 'Name'
    ///     NoLive = $true
    /// }
    /// $run = Start-ConsoleRun @runOptions
    /// $update = @{
    ///     Run = $run
    ///     Index = @(0)
    ///     Status = 'Pass'
    ///     Detail = 'Uploaded'
    /// }
    /// Update-Console @update
    /// Complete-ConsoleRun -Run $run</code>
    /// </example>
    [Cmdlet(VerbsData.Update, "Console", DefaultParameterSetName = "Fields", HelpUri = "https://consolemonster.dev/powershell/commands/update-console/")]
    [PowerForge.Fluent.FluentCmdlet(typeof(global::ConsoleMonster.Live.WidgetUpdate), Include = new[] { "NodeKey", "EdgeKey", "Key", "Total", "Increment", "Status", "Result", "ParentKey", "Index", "Count", "CellCount" })]
    public sealed partial class CmdletUpdateConsole : PSCmdlet {
        /// <summary>Unsupported pipeline input for update modes that do not take rows or content.</summary>
        [Parameter(ValueFromPipeline = true, DontShow = true, ParameterSetName = "Fields")]
        [Parameter(ValueFromPipeline = true, DontShow = true, ParameterSetName = "Append")]
        [Parameter(ValueFromPipeline = true, DontShow = true, ParameterSetName = "RemoveRow")]
        [Parameter(ValueFromPipeline = true, DontShow = true, ParameterSetName = "ClearRows")]
        [Parameter(ValueFromPipeline = true, DontShow = true, ParameterSetName = "Cells")]
        [Parameter(ValueFromPipeline = true, DontShow = true, ParameterSetName = "AppendCells")]
        [Parameter(ValueFromPipeline = true, DontShow = true, ParameterSetName = "ResizeCells")]
        [Parameter(ValueFromPipeline = true, DontShow = true, ParameterSetName = "FlowNode")]
        [Parameter(ValueFromPipeline = true, DontShow = true, ParameterSetName = "FlowEdge")]
        [Parameter(ValueFromPipeline = true, DontShow = true, ParameterSetName = "Run")]
        public PSObject? PipelineInput { get; set; }
        /// <summary><para type="description">The live widget identifier.</para></summary>
        [Parameter(ParameterSetName = "Fields", Mandatory = true, Position = 0)]
        [Parameter(ParameterSetName = "Append", Mandatory = true, Position = 0)]
        [Parameter(ParameterSetName = "Row", Mandatory = true, Position = 0)]
        [Parameter(ParameterSetName = "RemoveRow", Mandatory = true, Position = 0)]
        [Parameter(ParameterSetName = "ClearRows", Mandatory = true, Position = 0)]
        [Parameter(ParameterSetName = "Cells", Mandatory = true, Position = 0)]
        [Parameter(ParameterSetName = "AppendCells", Mandatory = true, Position = 0)]
        [Parameter(ParameterSetName = "ResizeCells", Mandatory = true, Position = 0)]
        [Parameter(ParameterSetName = "Content", Mandatory = true, Position = 0)]
        [Parameter(ParameterSetName = "FlowNode", Mandatory = true, Position = 0)]
        [Parameter(ParameterSetName = "FlowEdge", Mandatory = true, Position = 0)]
        public string WidgetId { get; set; } = string.Empty;

        /// <summary><para type="description">The session, otherwise ConsoleSession or the current session.</para></summary>
        [Parameter(ParameterSetName = "Fields")]
        [Parameter(ParameterSetName = "Append")]
        [Parameter(ParameterSetName = "Row")]
        [Parameter(ParameterSetName = "RemoveRow")]
        [Parameter(ParameterSetName = "ClearRows")]
        [Parameter(ParameterSetName = "Cells")]
        [Parameter(ParameterSetName = "AppendCells")]
        [Parameter(ParameterSetName = "ResizeCells")]
        [Parameter(ParameterSetName = "Content")]
        [Parameter(ParameterSetName = "FlowNode")]
        [Parameter(ParameterSetName = "FlowEdge")]
        public LiveSession? Session { get; set; }

        /// <summary><para type="description">The caller-managed run handle.</para></summary>
        [Parameter(ParameterSetName = "Run", Mandatory = true, Position = 0)]
        public LiveRun Run { get; set; } = null!;

        /// <summary><para type="description">The item key; RemoveRow requires it.</para></summary>
        [Parameter(ParameterSetName = "Fields")]
        [Parameter(ParameterSetName = "Append")]
        [Parameter(ParameterSetName = "RemoveRow", Mandatory = true)]
        public string? Key { get; set; }

        /// <summary><para type="description">The replacement label or current run phase.</para></summary>
        [Parameter(ParameterSetName = "Fields")]
        [Parameter(ParameterSetName = "Run")]
        [AllowNull]
        public object? Label { get; set; }

        /// <summary><para type="description">Detail text replacing the current message on the addressed live item.</para></summary>
        [Parameter(ParameterSetName = "Fields")]
        [Parameter(ParameterSetName = "Run")]
        [AllowNull]
        public object? Detail { get; set; }

        /// <summary><para type="description">The absolute value or appended numeric sample.</para></summary>
        [Parameter(ParameterSetName = "Fields")]
        [Parameter(ParameterSetName = "Append", Mandatory = true)]
        [AllowNull]
        public object? Value { get; set; }

        /// <summary><para type="description">The planned total; zero means unknown.</para></summary>
        [Parameter(ParameterSetName = "Fields")]
        [PSDefaultValue(Help = "0")]
        public double Total { get; set; }

        /// <summary><para type="description">The additive change; cannot be combined with Value.</para></summary>
        [Parameter(ParameterSetName = "Fields")]
        [PSDefaultValue(Help = "0")]
        public double Increment { get; set; }

        /// <summary><para type="description">The replacement live status, such as Running, Pass, Warn or Fail.</para></summary>
        [Parameter(ParameterSetName = "Fields")]
        [Parameter(ParameterSetName = "Cells", Mandatory = true)]
        [Parameter(ParameterSetName = "AppendCells")]
        [Parameter(ParameterSetName = "Run", Mandatory = true)]
        [Parameter(ParameterSetName = "FlowNode", Mandatory = true)]
        [Parameter(ParameterSetName = "FlowEdge", Mandatory = true)]
        [Binding.StatusKindTransformation]
        [ArgumentCompleter(typeof(Binding.StatusKindCompleter))]
        [PSDefaultValue(Help = "Pending")]
        public StatusKind Status { get; set; }

        /// <summary><para type="description">The flow node whose status changes.</para></summary>
        [Parameter(ParameterSetName = "FlowNode", Mandatory = true)]
        public string NodeKey { get; set; } = string.Empty;

        /// <summary><para type="description">The flow edge whose status or connectivity changes.</para></summary>
        [Parameter(ParameterSetName = "FlowEdge", Mandatory = true)]
        public string EdgeKey { get; set; } = string.Empty;

        /// <summary><para type="description">Severs the edge; Cut:$false reconnects it. Omission preserves connectivity.</para></summary>
        [Parameter(ParameterSetName = "FlowEdge")]
        [PSDefaultValue(Help = "$false")]
        public SwitchParameter Cut { get; set; }

        /// <summary><para type="description">The result of completed lane steps.</para></summary>
        [Parameter(ParameterSetName = "Fields")]
        [Binding.StatusKindTransformation]
        [ArgumentCompleter(typeof(Binding.StatusKindCompleter))]
        [PSDefaultValue(Help = "Pending")]
        public StatusKind Result { get; set; }

        /// <summary><para type="description">The parent activity key, or the phase an item belongs to in a phase strip (Key is then the item).</para></summary>
        [Parameter(ParameterSetName = "Fields")]
        public string? ParentKey { get; set; }

        /// <summary><para type="description">The estimated remaining time, in seconds.</para></summary>
        [Parameter(ParameterSetName = "Fields")]
        [PSDefaultValue(Help = "0")]
        public double RemainingSeconds { get; set; }

        /// <summary><para type="description">Summary labels mapped to nonnegative integer counts.</para></summary>
        [Parameter(ParameterSetName = "Fields")]
        public IDictionary? Counter { get; set; }

        /// <summary><para type="description">Restarts an indeterminate progress indicator.</para></summary>
        [Parameter(ParameterSetName = "Fields")]
        [PSDefaultValue(Help = "$false")]
        public SwitchParameter Restart { get; set; }

        /// <summary><para type="description">Finishes the activity or lane.</para></summary>
        [Parameter(ParameterSetName = "Fields")]
        [PSDefaultValue(Help = "$false")]
        public SwitchParameter Complete { get; set; }

        /// <summary><para type="description">Appends Value as a chart sample.</para></summary>
        [Parameter(ParameterSetName = "Append", Mandatory = true)]
        [PSDefaultValue(Help = "$false")]
        public SwitchParameter Append { get; set; }

        /// <summary><para type="description">A record with the live table's column properties (keyed by its KeyProperty), or a Key and Values descriptor that retains typed cells.</para></summary>
        [Parameter(ParameterSetName = "Row", Mandatory = true, ValueFromPipeline = true)]
        public object Row { get; set; } = null!;

        /// <summary><para type="description">Removes the row addressed by Key.</para></summary>
        [Parameter(ParameterSetName = "RemoveRow", Mandatory = true)]
        [PSDefaultValue(Help = "$false")]
        public SwitchParameter RemoveRow { get; set; }

        /// <summary><para type="description">Clears all live table rows.</para></summary>
        [Parameter(ParameterSetName = "ClearRows", Mandatory = true)]
        [PSDefaultValue(Help = "$false")]
        public SwitchParameter ClearRows { get; set; }

        /// <summary><para type="description">Array of zero-based positions. In the Cells set, each entry identifies a cell to receive Status. In the Run set, supply exactly one entry to identify an item by its position in the run input. Use @(0) for the first item.</para></summary>
        [Parameter(ParameterSetName = "Cells", Mandatory = true)]
        [Parameter(ParameterSetName = "Run", Mandatory = true)]
        public int[] Index { get; set; } = Array.Empty<int>();

        /// <summary><para type="description">Appends outcome cells.</para></summary>
        [Parameter(ParameterSetName = "AppendCells", Mandatory = true)]
        [PSDefaultValue(Help = "$false")]
        public SwitchParameter AppendCells { get; set; }

        /// <summary><para type="description">The number of appended cells; default one.</para></summary>
        [Parameter(ParameterSetName = "AppendCells")]
        [PSDefaultValue(Help = "1")]
        public int Count { get; set; } = 1;

        /// <summary><para type="description">The new cell count, at least one.</para></summary>
        [Parameter(ParameterSetName = "ResizeCells", Mandatory = true)]
        [PSDefaultValue(Help = "0")]
        public int CellCount { get; set; }

        /// <summary><para type="description">The replacement widget or a script block producing widgets; null clears the region.</para></summary>
        [Parameter(ParameterSetName = "Content", Mandatory = true, ValueFromPipeline = true)]
        [AllowNull]
        public object? Content { get; set; }

        /// <summary><para type="description">The measured run duration in seconds.</para></summary>
        [Parameter(ParameterSetName = "Run")]
        public double? DurationSeconds { get; set; }

        /// <inheritdoc />
        protected override void ProcessRecord() {
            if (PipelineInput != null) { NoPipelineInputCmdlet.Reject(this, PipelineInput); return; }
            try {
                if (ParameterSetName == "Run") {
                    if (Index.Length != 1) { throw new ArgumentException("The Run set requires exactly one Index."); }
                    if (MyInvocation.BoundParameters.ContainsKey(nameof(Session))) { throw new ArgumentException("Session cannot be used with Run."); }
                    ConsoleUpdate.Apply(Run, new RunUpdate { Index = Index[0], Status = Status, Label = PSText.PlainText(Label), Detail = PSText.PlainText(Detail), Duration = DurationSeconds.HasValue ? TimeSpan.FromSeconds(DurationSeconds.Value) : (TimeSpan?)null });
                    LiveRunHostQueues.Drain(Run, this);
                    return;
                }
                LiveSession? session = Session;
                if (session == null) { session = PSText.Unwrap(SessionState.PSVariable.GetValue("ConsoleSession")) as LiveSession; }
                session ??= LiveSession.Current;
                if (session == null) { throw new InvalidOperationException("No console dashboard is running. Use Invoke-ConsoleDashboard first."); }
                var update = new WidgetUpdate();
                ApplyParameters(update);
                if (MyInvocation.BoundParameters.ContainsKey(nameof(Cut))) { update.Cut = Cut.IsPresent; }
                if (MyInvocation.BoundParameters.ContainsKey(nameof(Label))) { update.Label = PSText.EventText(Label, out _); }
                if (MyInvocation.BoundParameters.ContainsKey(nameof(Detail))) { update.Detail = PSText.EventText(Detail, out _); }
                if (MyInvocation.BoundParameters.ContainsKey(nameof(Value))) { update.Value = PSText.Unwrap(Value); }
                if (MyInvocation.BoundParameters.ContainsKey(nameof(RemainingSeconds))) { update.Remaining = TimeSpan.FromSeconds(RemainingSeconds); }
                if (MyInvocation.BoundParameters.ContainsKey(nameof(Counter))) { update.Counter = Counters(Counter); }
                if (MyInvocation.BoundParameters.ContainsKey(nameof(Restart))) { update.Restart = Restart.IsPresent; }
                if (MyInvocation.BoundParameters.ContainsKey(nameof(Complete))) { update.Complete = Complete.IsPresent; }
                if (MyInvocation.BoundParameters.ContainsKey(nameof(Append))) { update.Append = Append.IsPresent; }
                if (MyInvocation.BoundParameters.ContainsKey(nameof(Row))) { update.Row = TableRow(Row, session, WidgetId); }
                if (MyInvocation.BoundParameters.ContainsKey(nameof(RemoveRow))) { update.RemoveRow = RemoveRow.IsPresent; }
                if (MyInvocation.BoundParameters.ContainsKey(nameof(ClearRows))) { update.ClearRows = ClearRows.IsPresent; }
                if (MyInvocation.BoundParameters.ContainsKey(nameof(AppendCells))) { update.AppendCells = AppendCells.IsPresent; }
                if (MyInvocation.BoundParameters.ContainsKey(nameof(Content))) { update.Content = ContentWidget(Content); }
                ConsoleUpdate.Apply(session, WidgetId, update);
            } catch (ArgumentException error) {
                ThrowTerminatingError(new ErrorRecord(error, error is ConsoleUpdateException updateError ? updateError.ErrorId : "ConsoleMonsterInvalidUpdate", ErrorCategory.InvalidArgument, WidgetId));
            } catch (InvalidOperationException error) {
                ThrowTerminatingError(new ErrorRecord(error, "ConsoleMonsterNoSession", ErrorCategory.InvalidOperation, WidgetId));
            }
        }

        private static IDictionary<string, int>? Counters(IDictionary? source) {
            if (source == null) { return null; }
            var counters = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (DictionaryEntry entry in source) { counters.Add(PSWidgets.ToText(entry.Key), LanguagePrimitives.ConvertTo<int>(PSText.Unwrap(entry.Value))); }
            return counters;
        }

        private static IWidget? ContentWidget(object? content) {
            object? value = PSText.Unwrap(content);
            if (value == null) { return null; }
            if (value is IWidget widget) { return widget; }
            if (value is ScriptBlock block) {
                List<IWidget> children = PSWidgets.Collect(block);
                return children.Count == 0 ? null : children.Count == 1 ? children[0] : new Rows(children.ToArray());
            }
            throw new ArgumentException("Content must be a widget or a script block producing widgets.");
        }

        // A Key/Values descriptor keeps its typed cells; any other record is read through the live table's own columns and KeyProperty.
        private static ConsoleTableRow TableRow(object source, LiveSession session, string widgetId) {
            object? value = PSText.Unwrap(source);
            if (value is ConsoleTableRow row) { return row; }
            PSObject record = PSObject.AsPSObject(source);
            object? key = value is IDictionary dictionary ? dictionary["Key"] : record.Properties["Key"]?.Value;
            object? values = value is IDictionary map ? map["Values"] : record.Properties["Values"]?.Value;
            if (key == null || values == null) {
                // Not a live table (or an unknown id): an empty row lets the shared validator name the real problem.
                if (value == null || !session.TryGetTarget(widgetId, out ILiveTarget? target) || !(target is Widgets.LiveTable table)) { return new ConsoleTableRow(); }
                return Binding.TableBindings.Project(table, source);
            }
            var cells = new List<object?>();
            if (values is IEnumerable sequence && !(values is string)) { foreach (object? cell in sequence) { cells.Add(PSText.Unwrap(cell)); } }
            else { cells.Add(PSText.Unwrap(values)); }
            return new ConsoleTableRow { Key = PSWidgets.ToText(key), Values = cells.ToArray() };
        }
    }
}
