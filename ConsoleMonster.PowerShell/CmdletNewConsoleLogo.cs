using System.Management.Automation;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Creates banner text drawn with a pixel font and a colour gradient.</para>
    /// <para type="description">Letters A-Z, digits and - . : ! / _ are drawn with half blocks (four rows tall) on modern and Consolas fonts, and with # in ASCII mode. Colours accept names, #hex or rgb().</para>
    /// <example>
    ///   <para>A banner with a tagline</para>
    ///   <code>New-ConsoleLogo -Text 'DEPLOY' -Tagline 'production · 14 servers' -From '#22d3ee' -To '#a78bfa' | Write-ConsoleWidget</code>
    /// </example>
    /// </summary>
    [Cmdlet(VerbsCommon.New, "ConsoleLogo")]
    [OutputType(typeof(Logo))]
    public sealed class CmdletNewConsoleLogo : PSCmdlet {
        /// <summary><para type="description">The banner text.</para></summary>
        [Parameter(Position = 0, Mandatory = true)]
        public string Text { get; set; } = string.Empty;

        /// <summary><para type="description">A muted tagline under the banner (markup allowed).</para></summary>
        [Parameter]
        public object? Tagline { get; set; }

        /// <summary><para type="description">Gradient start colour.</para></summary>
        [Parameter]
        public string? From { get; set; }

        /// <summary><para type="description">Gradient end colour.</para></summary>
        [Parameter]
        public string? To { get; set; }

        /// <summary><para type="description">Alignment: Left, Center or Right.</para></summary>
        [Parameter]
        public HorizontalAlignment Alignment { get; set; } = HorizontalAlignment.Left;

        /// <inheritdoc />
        protected override void ProcessRecord() {
            Logo logo = new Logo(Text, PSText.Markup(Tagline)) { Alignment = Alignment };
            if (From != null && Color.TryParse(From, out Color from)) {
                logo.From = from;
            }
            if (To != null && Color.TryParse(To, out Color to)) {
                logo.To = to;
            }
            WriteObject(logo);
        }
    }
}
