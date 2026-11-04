using System;
using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Graphics;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Builds a diagram from a script block or connection rows.</para>
    /// <para type="description">Emit New-ConsoleDiagramNode, Link, Group and UseCase objects in Content. A node's label is its id unless Id is given. Pipeline rows use From, To, Label, Status and Group; selectors accept property names or script blocks using $_. Connections create undeclared endpoints. Graphics is optional for C#; the module includes it and registers its renderer.</para>
    /// </summary>
    /// <example><para>A home network.</para><code>New-ConsoleDiagram -Title 'Home' { New-ConsoleDiagramNode Laptop -To Router; New-ConsoleDiagramNode Router -Kind Gateway } | Show-Console</code></example>
    /// <example><para>Connection rows.</para><code>$rows | New-ConsoleDiagram -Title 'Replication' -From Source -To Destination</code></example>
    [Cmdlet(VerbsCommon.New, "ConsoleDiagram", DefaultParameterSetName = "Rows")]
    [OutputType(typeof(Diagram))]
    public sealed class CmdletNewConsoleDiagram : DiagramContentCmdlet {
        /// <inheritdoc />
        protected override void EndProcessing() { DiagramRendererProvider.Register(); WriteObject(BuildDiagram()); }
    }

    /// <summary>Shared binding for diagram construction and display. Model rules stay in Diagram.</summary>
    public abstract class DiagramContentCmdlet : PSCmdlet {
        private readonly List<object> _rows = new List<object>();
        private Diagram? _inputDiagram;
        /// <summary><para type="description">Emits typed diagram nodes, links, groups and use cases.</para></summary>
        [Parameter(Position = 0, ParameterSetName = "Rows")] public ScriptBlock? Content { get; set; }
        /// <summary><para type="description">Connection rows or an existing diagram.</para></summary>
        [Parameter(ValueFromPipeline = true, ParameterSetName = "Rows")] public PSObject? InputObject { get; set; }
        /// <summary><para type="description">The diagram title.</para></summary>
        [Parameter] public string Title { get; set; } = "";
        /// <summary><para type="description">The layout direction.</para></summary>
        [Parameter] public DiagramDirection Direction { get; set; }
        /// <summary><para type="description">The identifier addressed by Update-Console.</para></summary>
        [Parameter] public string WidgetId { get; set; } = "diagram";
        /// <summary><para type="description">Source property name or script block; defaults to From.</para></summary>
        [Parameter(ParameterSetName = "Rows")] public object? From { get; set; }
        /// <summary><para type="description">Destination property name or script block; defaults to To.</para></summary>
        [Parameter(ParameterSetName = "Rows")] public object? To { get; set; }
        /// <summary><para type="description">Connection label property name or script block; defaults to Label.</para></summary>
        [Parameter(ParameterSetName = "Rows")] public object? Label { get; set; }
        /// <summary><para type="description">Health property name or script block; defaults to Status.</para></summary>
        [Parameter(ParameterSetName = "Rows")] public object? Status { get; set; }
        /// <summary><para type="description">Source group property name or script block; defaults to Group.</para></summary>
        [Parameter(ParameterSetName = "Rows")] public object? Group { get; set; }
        /// <summary><para type="description">Hides the full-text attention list.</para></summary>
        [Parameter] public SwitchParameter NoSummary { get; set; }
        /// <summary><para type="description">Shows status meanings below the topology.</para></summary>
        [Parameter] public bool ShowLegend { get; set; } = true;
        /// <summary><para type="description">Shows use-case steps.</para></summary>
        [Parameter] public bool ShowStepPanel { get; set; } = true;
        /// <summary><para type="description">Shows focused-node details.</para></summary>
        [Parameter] public bool ShowDetails { get; set; } = true;
        /// <summary><para type="description">Shows keyboard hints.</para></summary>
        [Parameter] public SwitchParameter ShowKeys { get; set; }
        /// <summary><para type="description">The maximum visible payload lines.</para></summary>
        [Parameter][ValidateRange(1, int.MaxValue)] public int MaxPayloadLines { get; set; } = 8;
        /// <summary><para type="description">The maximum visible step lines.</para></summary>
        [Parameter][ValidateRange(1, int.MaxValue)] public int MaxStepLines { get; set; } = 7;
        /// <inheritdoc />
        protected override void ProcessRecord() {
            if (InputObject == null) return;
            object? value = PSText.Unwrap(InputObject);
            if (value is Diagram diagram) {
                if (_inputDiagram != null || _rows.Count > 0) throw new ArgumentException("Pass one diagram, or connection rows.");
                _inputDiagram = diagram;
            } else {
                if (_inputDiagram != null) throw new ArgumentException("Pass one diagram, or connection rows.");
                _rows.Add(InputObject);
            }
        }
        /// <summary>Collects content and rows into a single core widget.</summary>
        protected Diagram BuildDiagram() {
            Diagram diagram = _inputDiagram ?? new Diagram(Title, WidgetId);
            foreach (object item in PSItems.Items(Content)) PSDiagramItems.Add(diagram, item);
            PSDiagramItems.Rows(diagram, _rows, From, To, Label, Status, Group);
            Configure(diagram);
            return diagram;
        }
        /// <summary>Applies explicitly bound settings to an existing diagram, or defaults to a new one.</summary>
        protected void Configure(Diagram diagram) {
            if (MyInvocation.BoundParameters.ContainsKey(nameof(Title))) diagram.Title = Title;
            if (MyInvocation.BoundParameters.ContainsKey(nameof(Direction))) diagram.Direction = Direction;
            if (_inputDiagram == null || MyInvocation.BoundParameters.ContainsKey(nameof(NoSummary))) diagram.ShowSummary = !NoSummary;
            if (_inputDiagram == null || MyInvocation.BoundParameters.ContainsKey(nameof(ShowLegend))) diagram.ShowLegend = ShowLegend;
            if (_inputDiagram == null || MyInvocation.BoundParameters.ContainsKey(nameof(ShowStepPanel))) diagram.ShowStepPanel = ShowStepPanel;
            if (_inputDiagram == null || MyInvocation.BoundParameters.ContainsKey(nameof(ShowDetails))) diagram.ShowDetails = ShowDetails;
            if (_inputDiagram == null || MyInvocation.BoundParameters.ContainsKey(nameof(ShowKeys))) diagram.ShowKeys = ShowKeys;
            if (_inputDiagram == null || MyInvocation.BoundParameters.ContainsKey(nameof(MaxPayloadLines))) diagram.MaxPayloadLines = MaxPayloadLines;
            if (_inputDiagram == null || MyInvocation.BoundParameters.ContainsKey(nameof(MaxStepLines))) diagram.MaxStepLines = MaxStepLines;
        }
    }
}
