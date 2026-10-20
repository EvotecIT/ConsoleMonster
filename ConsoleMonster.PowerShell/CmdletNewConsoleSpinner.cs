using System.Management.Automation;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Creates a spinner: a one-line activity indicator for work without a known total.</para>
    /// <para type="description">While running it animates (braille on modern fonts, | / - \ on Basic and ASCII) and shows the label, a muted detail and the elapsed time. Once finished it shows the status marker in the status colour and the final duration. Update it live with Update-ConsoleSpinner.</para>
    /// <example>
    ///   <para>A spinner inside a dashboard, finished from the script</para>
    ///   <code>$d = New-ConsoleDashboard -Title 'Sync' { New-ConsoleSpinner -Label 'Resolving DNS' }
    /// Start-ConsoleDashboard $d { Start-Sleep 2; Update-ConsoleSpinner -Status Pass -Detail '42 zones' }</code>
    /// </example>
    /// </summary>
    [Cmdlet(VerbsCommon.New, "ConsoleSpinner")]
    [OutputType(typeof(Spinner))]
    public sealed class CmdletNewConsoleSpinner : PSCmdlet {
        /// <summary><para type="description">The label (markup allowed).</para></summary>
        [Parameter(Position = 0)]
        public object Label { get; set; } = string.Empty;

        /// <summary><para type="description">Muted detail after the label, for example the current item.</para></summary>
        [Parameter]
        public object? Detail { get; set; }

        /// <summary><para type="description">The initial status (default Running).</para></summary>
        [Parameter]
        [Binding.StatusKindTransformation]
        [ArgumentCompleter(typeof(Binding.StatusKindCompleter))]
        public StatusKind Status { get; set; } = StatusKind.Running;

        /// <summary><para type="description">The live name (default spinner).</para></summary>
        [Parameter]
        public string Name { get; set; } = "spinner";

        /// <summary><para type="description">Hide the elapsed time.</para></summary>
        [Parameter]
        public SwitchParameter NoElapsed { get; set; }

        /// <inheritdoc />
        protected override void ProcessRecord() {
            WriteObject(new Spinner(PSText.Markup(Label) ?? string.Empty, Name) { Detail = PSText.Markup(Detail), Status = Status, ShowElapsed = !NoElapsed.IsPresent });
        }
    }
}
