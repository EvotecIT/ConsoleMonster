using System.Management.Automation;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Writes Spectre-compatible markup as styled text.</para>
    /// <para type="description">Supports [bold], [italic], [underline], the 256-colour palette names ([red], [cadetblue], [grey50], [deepskyblue3]; in markup [darkred] is the palette colour, [console.darkred] the ConsoleColor as Write-Host shows it), [#ff8800], [rgb(1,2,3)], [white on navy], links ([link=https://example.com]text[/] or [link]https://example.com[/], clickable in Windows Terminal and VS Code) and theme tokens ([pass], [fail], [warn], [muted], [accent]). Escape brackets by doubling them ([[ and ]]) or with [ConsoleMonster.Text.Markup]::Escape($value). A tag that is not a known style is removed and its text kept when a [/] closes it; an unclosed unknown tag stays literal. Colours degrade to 256 or 16 colours, or none, as the terminal requires.</para>
    /// </summary>
    /// <example>
    ///   <para>A status line</para>
    ///   <code>Write-ConsoleMarkup '[pass]Deployed[/] 14 servers in [muted]3m 12s[/]'</code>
    /// </example>
    /// <example>
    ///   <para>A prefix and the rest of the line from another call</para>
    ///   <code>Write-ConsoleMarkup '[cadetblue]Checking[/] ' -NoNewLine; Write-ConsoleMarkup '[pass]done[/]'</code>
    /// </example>
    [Cmdlet(VerbsCommunications.Write, "ConsoleMarkup")]
    public sealed class CmdletWriteConsoleMarkup : PSCmdlet {
        /// <summary><para type="description">The markup.</para></summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true)]
        [AllowEmptyString]
        public string Text { get; set; } = string.Empty;

        /// <summary><para type="description">Leaves the line open, as Write-Host -NoNewline does. Redirected output still gets plain text.</para></summary>
        [Parameter]
        public SwitchParameter NoNewLine { get; set; }

        /// <inheritdoc />
        protected override void ProcessRecord() {
            HostOutput.WriteMarkup(this, Text, !NoNewLine.IsPresent);
        }
    }
}
