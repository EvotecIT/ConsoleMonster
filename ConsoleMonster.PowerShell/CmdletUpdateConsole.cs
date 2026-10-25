using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Management.Automation;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Updates a live widget or a caller-managed run through one validated operation.</para>
    /// <para type="description">WidgetId is position zero. Fields are checked against the resolved widget before any events are posted. Structural sets separate table rows, cell batches, content replacement and run items. Unknown targets and unsupported fields are terminating errors.</para>
    /// </summary>
    /// <example><para>Advance a build and change its label</para><code>Update-Console build -Value 5 -Total 20 -Label 'Compile'</code></example>
    /// <example><para>Finish one run item</para><code>Update-Console -Run $run -Index 0 -Status Pass -Detail 'Uploaded'</code></example>
    [Cmdlet(VerbsData.Update, "Console", DefaultParameterSetName = "Fields")]
    public sealed class CmdletUpdateConsole : PSCmdlet {
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

        /// <summary><para type="description">The replacement detail.</para></summary>
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
        public double Total { get; set; }

        /// <summary><para type="description">The additive change; cannot be combined with Value.</para></summary>
        [Parameter(ParameterSetName = "Fields")]
        public double Increment { get; set; }

        /// <summary><para type="description">The new state.</para></summary>
        [Parameter(ParameterSetName = "Fields")]
        [Parameter(ParameterSetName = "Cells", Mandatory = true)]
        [Parameter(ParameterSetName = "AppendCells")]
        [Parameter(ParameterSetName = "Run", Mandatory = true)]
        [Binding.StatusKindTransformation]
        [ArgumentCompleter(typeof(Binding.StatusKindCompleter))]
        public StatusKind Status { get; set; }

        /// <summary><para type="description">The result of completed lane steps.</para></summary>
        [Parameter(ParameterSetName = "Fields")]
        [Binding.StatusKindTransformation]
        [ArgumentCompleter(typeof(Binding.StatusKindCompleter))]
        public StatusKind Result { get; set; }

        /// <summary><para type="description">The parent activity key.</para></summary>
        [Parameter(ParameterSetName = "Fields")]
        public string? ParentKey { get; set; }

        /// <summary><para type="description">The estimated remaining time, in seconds.</para></summary>
        [Parameter(ParameterSetName = "Fields")]
        public double RemainingSeconds { get; set; }

        /// <summary><para type="description">Summary labels mapped to nonnegative integer counts.</para></summary>
        [Parameter(ParameterSetName = "Fields")]
        public IDictionary? Counter { get; set; }

        /// <summary><para type="description">Restarts an indeterminate progress indicator.</para></summary>
        [Parameter(ParameterSetName = "Fields")]
        public SwitchParameter Restart { get; set; }

        /// <summary><para type="description">Finishes the activity or lane.</para></summary>
        [Parameter(ParameterSetName = "Fields")]
        public SwitchParameter Complete { get; set; }

        /// <summary><para type="description">Appends Value as a chart sample.</para></summary>
        [Parameter(ParameterSetName = "Append", Mandatory = true)]
        public SwitchParameter Append { get; set; }

        /// <summary><para type="description">A row with Key and Values properties, retaining typed cells.</para></summary>
        [Parameter(ParameterSetName = "Row", Mandatory = true, ValueFromPipeline = true)]
        public object Row { get; set; } = null!;

        /// <summary><para type="description">Removes the row addressed by Key.</para></summary>
        [Parameter(ParameterSetName = "RemoveRow", Mandatory = true)]
        public SwitchParameter RemoveRow { get; set; }

        /// <summary><para type="description">Clears all live table rows.</para></summary>
        [Parameter(ParameterSetName = "ClearRows", Mandatory = true)]
        public SwitchParameter ClearRows { get; set; }

        /// <summary><para type="description">Zero-based cells; the Run set requires exactly one index.</para></summary>
        [Parameter(ParameterSetName = "Cells", Mandatory = true)]
        [Parameter(ParameterSetName = "Run", Mandatory = true)]
        public int[] Index { get; set; } = Array.Empty<int>();

        /// <summary><para type="description">Appends outcome cells.</para></summary>
        [Parameter(ParameterSetName = "AppendCells", Mandatory = true)]
        public SwitchParameter AppendCells { get; set; }

        /// <summary><para type="description">The number of appended cells; default one.</para></summary>
        [Parameter(ParameterSetName = "AppendCells")]
        public int Count { get; set; } = 1;

        /// <summary><para type="description">The new cell count, at least one.</para></summary>
        [Parameter(ParameterSetName = "ResizeCells", Mandatory = true)]
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
                if (MyInvocation.BoundParameters.ContainsKey(nameof(Key))) { update.Key = Key; }
                if (MyInvocation.BoundParameters.ContainsKey(nameof(Label))) { update.Label = PSText.EventText(Label, out _); }
                if (MyInvocation.BoundParameters.ContainsKey(nameof(Detail))) { update.Detail = PSText.EventText(Detail, out _); }
                if (MyInvocation.BoundParameters.ContainsKey(nameof(Value))) { update.Value = PSText.Unwrap(Value); }
                if (MyInvocation.BoundParameters.ContainsKey(nameof(Total))) { update.Total = Total; }
                if (MyInvocation.BoundParameters.ContainsKey(nameof(Increment))) { update.Increment = Increment; }
                if (MyInvocation.BoundParameters.ContainsKey(nameof(Status))) { update.Status = Status; }
                if (MyInvocation.BoundParameters.ContainsKey(nameof(Result))) { update.Result = Result; }
                if (MyInvocation.BoundParameters.ContainsKey(nameof(ParentKey))) { update.ParentKey = ParentKey; }
                if (MyInvocation.BoundParameters.ContainsKey(nameof(RemainingSeconds))) { update.Remaining = TimeSpan.FromSeconds(RemainingSeconds); }
                if (MyInvocation.BoundParameters.ContainsKey(nameof(Counter))) { update.Counter = Counters(Counter); }
                if (MyInvocation.BoundParameters.ContainsKey(nameof(Restart))) { update.Restart = Restart.IsPresent; }
                if (MyInvocation.BoundParameters.ContainsKey(nameof(Complete))) { update.Complete = Complete.IsPresent; }
                if (MyInvocation.BoundParameters.ContainsKey(nameof(Append))) { update.Append = Append.IsPresent; }
                if (MyInvocation.BoundParameters.ContainsKey(nameof(Row))) { update.Row = TableRow(Row); }
                if (MyInvocation.BoundParameters.ContainsKey(nameof(RemoveRow))) { update.RemoveRow = RemoveRow.IsPresent; }
                if (MyInvocation.BoundParameters.ContainsKey(nameof(ClearRows))) { update.ClearRows = ClearRows.IsPresent; }
                if (MyInvocation.BoundParameters.ContainsKey(nameof(Index))) { update.Index = Index; }
                if (MyInvocation.BoundParameters.ContainsKey(nameof(AppendCells))) { update.AppendCells = AppendCells.IsPresent; }
                if (MyInvocation.BoundParameters.ContainsKey(nameof(Count))) { update.Count = Count; }
                if (MyInvocation.BoundParameters.ContainsKey(nameof(CellCount))) { update.CellCount = CellCount; }
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

        private static ConsoleTableRow TableRow(object source) {
            object? value = PSText.Unwrap(source);
            if (value is ConsoleTableRow row) { return row; }
            PSObject record = PSObject.AsPSObject(source);
            object? key = value is IDictionary dictionary ? dictionary["Key"] : record.Properties["Key"]?.Value;
            object? values = value is IDictionary map ? map["Values"] : record.Properties["Values"]?.Value;
            if (key == null || values == null) { throw new ArgumentException("Row requires Key and Values properties."); }
            var cells = new List<object?>();
            if (values is IEnumerable sequence && !(values is string)) { foreach (object? cell in sequence) { cells.Add(PSText.Unwrap(cell)); } }
            else { cells.Add(PSText.Unwrap(values)); }
            return new ConsoleTableRow { Key = PSWidgets.ToText(key), Values = cells.ToArray() };
        }
    }
}
