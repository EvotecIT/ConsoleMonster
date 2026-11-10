using System;
using System.Management.Automation;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>Builds a playable sequence of diagram interactions.</summary>
    /// <remarks><para>Content emits New-ConsoleDiagramStep values in playback order. Id defaults to Label. AutoPlay starts only when the keyboard view opens; static output keeps the selected step.</para></remarks>
    /// <example><para>A request and reply.</para><code>New-ConsoleDiagramUseCase 'Place an order' { New-ConsoleDiagramStep Shopper Web 'POST /checkout'; New-ConsoleDiagramStep Web Shopper 'Thank you' -Reply }</code></example>
    /// <example>
    /// <para>Compose an inventory diagram use case</para>
    /// <code>New-ConsoleDiagram -Title 'Order processing' -Content {
    ///     New-ConsoleDiagramNode -Id api -Label API
    ///     New-ConsoleDiagramNode -Id db -Label Database
    ///     New-ConsoleDiagramUseCase -Id save -Label 'Save order' -Content { New-ConsoleDiagramStep -From api -To db -Label Insert }
    /// } | Show-Console
    /// </code>
    /// </example>
    [Cmdlet(VerbsCommon.New, "ConsoleDiagramUseCase", HelpUri = "https://consolemonster.dev/powershell/commands/new-consolediagramusecase/")][OutputType(typeof(DiagramUseCase))]
    public sealed class CmdletNewConsoleDiagramUseCase : PSCmdlet {
        /// <summary><para type="description">The use-case label and default id.</para></summary>
        [Parameter(Mandatory = true, Position = 0)][ValidateNotNullOrEmpty] public string Label { get; set; } = "";
        /// <summary><para type="description">Emits interactions in order.</para></summary>
        [Parameter(Position = 1)] public ScriptBlock? Content { get; set; }
        /// <summary><para type="description">The explicit selection key.</para></summary>
        [Parameter][ValidateNotNullOrEmpty] public string? Id { get; set; }
        /// <summary><para type="description">Playback milliseconds per step.</para></summary>
        [Parameter][ValidateRange(200, 60000)] [PSDefaultValue(Help = "900")]
        public int Delay { get; set; } = 900;
        /// <summary><para type="description">Repeats playback.</para></summary>
        [Parameter] [PSDefaultValue(Help = "$false")]
        public SwitchParameter Loop { get; set; }
        /// <summary><para type="description">Starts playback on opening the keyboard view.</para></summary>
        [Parameter] [PSDefaultValue(Help = "$false")]
        public SwitchParameter AutoPlay { get; set; }
        /// <inheritdoc />
        protected override void ProcessRecord() {
            var useCase = new DiagramUseCase(Label) { Delay = Delay, Loop = Loop, AutoPlay = AutoPlay };
            if (Id != null) useCase.Id = Id;
            foreach (object item in PSItems.Items(Content)) useCase.Steps.Add(item as DiagramStep ?? throw new ArgumentException("A use case accepts New-ConsoleDiagramStep values."));
            WriteObject(useCase);
        }
    }
}
