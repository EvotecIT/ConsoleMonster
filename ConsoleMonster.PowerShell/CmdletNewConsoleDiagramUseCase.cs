using System;
using System.Management.Automation;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary><para type="synopsis">Builds a playable sequence of diagram interactions.</para><para type="description">Content emits New-ConsoleDiagramStep values in playback order. Id defaults to Label. AutoPlay starts only when the keyboard view opens; static output keeps the selected step.</para></summary>
    /// <example><para>A request and reply.</para><code>New-ConsoleDiagramUseCase 'Place an order' { New-ConsoleDiagramStep Shopper Web 'POST /checkout'; New-ConsoleDiagramStep Web Shopper 'Thank you' -Reply }</code></example>
    [Cmdlet(VerbsCommon.New, "ConsoleDiagramUseCase")][OutputType(typeof(DiagramUseCase))]
    public sealed class CmdletNewConsoleDiagramUseCase : PSCmdlet {
        /// <summary><para type="description">The use-case label and default id.</para></summary>
        [Parameter(Mandatory = true, Position = 0)][ValidateNotNullOrEmpty] public string Label { get; set; } = "";
        /// <summary><para type="description">Emits interactions in order.</para></summary>
        [Parameter(Position = 1)] public ScriptBlock? Content { get; set; }
        /// <summary><para type="description">The explicit selection key.</para></summary>
        [Parameter][ValidateNotNullOrEmpty] public string? Id { get; set; }
        /// <summary><para type="description">Playback milliseconds per step.</para></summary>
        [Parameter][ValidateRange(200, 60000)] public int Delay { get; set; } = 900;
        /// <summary><para type="description">Repeats playback.</para></summary>
        [Parameter] public SwitchParameter Loop { get; set; }
        /// <summary><para type="description">Starts playback on opening the keyboard view.</para></summary>
        [Parameter] public SwitchParameter AutoPlay { get; set; }
        /// <inheritdoc />
        protected override void ProcessRecord() {
            var useCase = new DiagramUseCase(Label) { Delay = Delay, Loop = Loop, AutoPlay = AutoPlay };
            if (Id != null) useCase.Id = Id;
            foreach (object item in PSItems.Items(Content)) useCase.Steps.Add(item as DiagramStep ?? throw new ArgumentException("A use case accepts New-ConsoleDiagramStep values."));
            WriteObject(useCase);
        }
    }
}
