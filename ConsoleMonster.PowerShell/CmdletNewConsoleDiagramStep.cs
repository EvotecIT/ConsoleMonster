using System;
using System.Collections;
using System.Management.Automation;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary><para type="synopsis">Creates a request, message, reply or failed interaction.</para><para type="description">From and To identify diagram nodes. Payload is a hashtable of fields shown with the active step. Reply and Failed are shorthand for Kind; choose one interaction kind.</para></summary>
    /// <example><para>A request payload.</para><code>New-ConsoleDiagramStep Web 'Orders API' 'create order' -Payload @{ sku = 'MUG-01'; qty = 2 }</code></example>
    [Cmdlet(VerbsCommon.New, "ConsoleDiagramStep")][OutputType(typeof(DiagramStep))]
    public sealed class CmdletNewConsoleDiagramStep : PSCmdlet {
        /// <summary><para type="description">The source node id.</para></summary>
        [Parameter(Mandatory = true, Position = 0)][ValidateNotNullOrEmpty] public string From { get; set; } = "";
        /// <summary><para type="description">The destination node id.</para></summary>
        [Parameter(Mandatory = true, Position = 1)][ValidateNotNullOrEmpty] public string To { get; set; } = "";
        /// <summary><para type="description">The interaction text.</para></summary>
        [Parameter(Mandatory = true, Position = 2)] public string Label { get; set; } = "";
        /// <summary><para type="description">Request, Message, Reply or Failed.</para></summary>
        [Parameter] public DiagramStepKind Kind { get; set; }
        /// <summary><para type="description">Marks a reply.</para></summary>
        [Parameter] public SwitchParameter Reply { get; set; }
        /// <summary><para type="description">Marks a failed interaction.</para></summary>
        [Parameter] public SwitchParameter Failed { get; set; }
        /// <summary><para type="description">Named payload fields.</para></summary>
        [Parameter] public Hashtable? Payload { get; set; }
        /// <inheritdoc />
        protected override void ProcessRecord() {
            if (Reply && Failed || (Reply || Failed) && MyInvocation.BoundParameters.ContainsKey(nameof(Kind))) throw new ArgumentException("Choose Kind, Reply or Failed once.");
            var step = new DiagramStep(From, To, Label) { Kind = Reply ? DiagramStepKind.Reply : Failed ? DiagramStepKind.Failed : Kind };
            if (Payload != null) foreach (DictionaryEntry entry in Payload) step.Payload[PSWidgets.ToText(entry.Key)] = PSText.Unwrap(entry.Value);
            WriteObject(step);
        }
    }
}
