using System.Management.Automation;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>Creates banner text drawn with a pixel font and a colour gradient.</summary>
    /// <remarks><para>Letters A-Z, digits and - . : ! / _ are drawn with half blocks (four rows tall) on modern and Consolas fonts, and with # in ASCII mode. Colours accept names, #hex or rgb().</para></remarks>
    /// <example>
    ///   <para>A banner with a tagline</para>
    ///   <code>New-ConsoleLogo -Text 'DEPLOY' -Tagline 'production · 14 servers' -StartColor '#22d3ee' -EndColor '#a78bfa' | Show-Console</code>
    /// </example>
    /// <example>
    /// <para>Print a fleet heading and tagline</para>
    /// <code>New-ConsoleLogo -Text 'Fleet' -Tagline 'Inventory checks' | Show-Console
    /// </code>
    /// </example>
    [Cmdlet(VerbsCommon.New, "ConsoleLogo", HelpUri = "https://consolemonster.dev/powershell/commands/new-consolelogo/")]
    [OutputType(typeof(Logo))]
    public sealed class CmdletNewConsoleLogo : NoPipelineInputCmdlet {
        /// <summary><para type="description">The literal heading drawn with the selected banner font.</para></summary>
        [Parameter(Position = 0, Mandatory = true)]
        public string Text { get; set; } = string.Empty;

        /// <summary><para type="description">A muted tagline under the banner (markup allowed).</para></summary>
        [Parameter]
        public object? Tagline { get; set; }

        /// <summary><para type="description">Gradient start colour.</para></summary>
        [Parameter]
        public string? StartColor { get; set; }

        /// <summary><para type="description">Gradient end colour.</para></summary>
        [Parameter]
        public string? EndColor { get; set; }

        /// <summary><para type="description">Alignment: Left, Center or Right.</para></summary>
        [Parameter]
        [PSDefaultValue(Help = "Left")]
        public HorizontalAlignment Alignment { get; set; } = HorizontalAlignment.Left;

        /// <inheritdoc />
        protected override void ProcessWithoutPipelineInput() {
            Logo logo = new Logo(Text, PSText.Markup(Tagline)) { Alignment = Alignment };
            if (StartColor != null && Color.TryParse(StartColor, out Color from)) {
                logo.StartColor = from;
            }
            if (EndColor != null && Color.TryParse(EndColor, out Color to)) {
                logo.EndColor = to;
            }
            WriteObject(logo);
        }
    }
}
