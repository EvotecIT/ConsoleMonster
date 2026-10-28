using System;
using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Creates a progress bar or an indeterminate activity indicator.</para>
    /// <para type="description">The default bar represents a known total. Indeterminate creates a spinner. Update either through Update-Console. Structural parameter sets reject bar-only options on a spinner and spinner-only options on a bar; the shared C# Progress factory validates the same contract.</para>
    /// </summary>
    /// <example><para>Show completed work</para><code>New-ConsoleProgress -Label 'Upload' -Value 64 -Total 100 | Show-Console</code></example>
    /// <example><para>Show work with no known total</para><code>New-ConsoleProgress resolve -Indeterminate -Label 'Resolving DNS' -Detail 'zone 4' | Show-Console</code></example>
    [Cmdlet(VerbsCommon.New, "ConsoleProgress", DefaultParameterSetName = "Bar")]
    [OutputType(typeof(ProgressBar), typeof(Spinner))]
    public sealed class CmdletNewConsoleProgress : NoPipelineInputCmdlet {
        /// <summary><para type="description">The live identifier; default progress, or spinner with Indeterminate.</para></summary>
        [Parameter(Position = 0)]
        public string? WidgetId { get; set; }
        /// <summary><para type="description">The activity label, including markup.</para></summary>
        [Parameter(Position = 1)]
        public object Label { get; set; } = string.Empty;
        /// <summary><para type="description">The initial state; default Running.</para></summary>
        [Parameter]
        [Binding.StatusKindTransformation]
        [ArgumentCompleter(typeof(Binding.StatusKindCompleter))]
        public Styling.StatusKind Status { get; set; } = Styling.StatusKind.Running;
        /// <summary><para type="description">Creates a spinner for work without a known total.</para></summary>
        [Parameter(Mandatory = true, ParameterSetName = "Indeterminate")]
        public SwitchParameter Indeterminate { get; set; }
        /// <summary><para type="description">The muted indeterminate activity detail.</para></summary>
        [Parameter(ParameterSetName = "Indeterminate")]
        public object? Detail { get; set; }
        /// <summary><para type="description">Hides the default-on indeterminate elapsed time.</para></summary>
        [Parameter(ParameterSetName = "Indeterminate")]
        public SwitchParameter NoElapsed { get; set; }
        /// <summary><para type="description">The completed value; default zero.</para></summary>
        [Parameter(ParameterSetName = "Bar")]
        public double Value { get; set; }
        /// <summary><para type="description">The planned total; default 100.</para></summary>
        [Parameter(ParameterSetName = "Bar")]
        public double Total { get; set; } = 100;
        /// <summary><para type="description">The progress bar shape; default Line.</para></summary>
        [Parameter(ParameterSetName = "Bar")]
        public BarShape Shape { get; set; } = BarShape.Line;
        /// <summary><para type="description">Ordered builtin column names, Type/Alignment/Priority/MaxWidth records or typed ProgressColumn objects.</para></summary>
        [Parameter(ParameterSetName = "Bar")]
        public object[]? Columns { get; set; }
        /// <summary><para type="description">The shared count column format.</para></summary>
        [Parameter(ParameterSetName = "Bar")]
        public Layout.ColumnFormat? CountColumn { get; set; }
        /// <summary><para type="description">Hides the default-on count in the classic bar layout.</para></summary>
        [Parameter(ParameterSetName = "Bar")]
        public SwitchParameter NoCount { get; set; }
        /// <summary><para type="description">The fixed label width.</para></summary>
        [Parameter(ParameterSetName = "Bar"), ValidateRange(0, int.MaxValue)]
        public int? LabelWidth { get; set; }
        /// <summary><para type="description">Rate smoothing window in seconds; default ten.</para></summary>
        [Parameter(ParameterSetName = "Bar"), ValidateRange(0, double.MaxValue)]
        public double RateWindowSeconds { get; set; } = 10;
        /// <summary><para type="description">Initial indeterminate start time on the session clock, in seconds.</para></summary>
        [Parameter(ParameterSetName = "Indeterminate")] public double? StartedSeconds { get; set; }
        /// <summary><para type="description">Initial indeterminate completion time on the session clock, in seconds.</para></summary>
        [Parameter(ParameterSetName = "Indeterminate")] public double? FinishedSeconds { get; set; }
        /// <inheritdoc />
        protected override void ProcessWithoutPipelineInput() {
            var options = new ProgressOptions { Indeterminate = Indeterminate.IsPresent };
            if (MyInvocation.BoundParameters.ContainsKey(nameof(StartedSeconds))) { options.Started = StartedSeconds.HasValue ? (TimeSpan?)TimeSpan.FromSeconds(StartedSeconds.Value) : null; }
            if (MyInvocation.BoundParameters.ContainsKey(nameof(FinishedSeconds))) { options.Finished = FinishedSeconds.HasValue ? (TimeSpan?)TimeSpan.FromSeconds(FinishedSeconds.Value) : null; }
            if (MyInvocation.BoundParameters.ContainsKey(nameof(WidgetId))) { options.WidgetId = WidgetId; }
            if (MyInvocation.BoundParameters.ContainsKey(nameof(Label))) { options.Label = PSText.Markup(Label) ?? string.Empty; }
            if (MyInvocation.BoundParameters.ContainsKey(nameof(Status))) { options.Status = Status; }
            if (MyInvocation.BoundParameters.ContainsKey(nameof(Detail))) { options.Detail = PSText.Markup(Detail); }
            if (MyInvocation.BoundParameters.ContainsKey(nameof(NoElapsed))) { options.ShowElapsed = !NoElapsed.IsPresent; }
            if (MyInvocation.BoundParameters.ContainsKey(nameof(Value))) { options.Value = Value; }
            if (MyInvocation.BoundParameters.ContainsKey(nameof(Total))) { options.Total = Total; }
            if (MyInvocation.BoundParameters.ContainsKey(nameof(Shape))) { options.Shape = Shape; }
            if (MyInvocation.BoundParameters.ContainsKey(nameof(Columns))) { options.Columns = PSProgressColumns.Build(Columns); }
            if (MyInvocation.BoundParameters.ContainsKey(nameof(CountColumn))) { options.CountColumn = CountColumn; }
            if (MyInvocation.BoundParameters.ContainsKey(nameof(NoCount))) { options.ShowCount = !NoCount.IsPresent; }
            if (MyInvocation.BoundParameters.ContainsKey(nameof(LabelWidth))) { options.LabelWidth = LabelWidth; }
            if (MyInvocation.BoundParameters.ContainsKey(nameof(RateWindowSeconds))) { options.RateWindow = TimeSpan.FromSeconds(RateWindowSeconds); }
            WriteObject(Widgets.Progress.Create(options));
        }

    }
}
