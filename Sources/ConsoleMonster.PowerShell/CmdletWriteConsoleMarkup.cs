using System.Management.Automation;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Writes Spectre-compatible markup as styled text.</para>
    /// <para type="description">Supports [bold], [italic], [underline], colours ([red], [#ff8800], [rgb(1,2,3)], [white on navy]) and theme tokens ([pass], [fail], [warn], [muted], [accent]). Escape brackets by doubling them: [[ and ]]. Colours degrade to 256 or 16 colours, or none, as the terminal requires.</para>
    /// <example>
    ///   <para>A status line</para>
    ///   <code>Write-ConsoleMarkup '[pass]Deployed[/] 14 servers in [muted]3m 12s[/]'</code>
    /// </example>
    /// </summary>
    [Cmdlet(VerbsCommunications.Write, "ConsoleMarkup")]
    public sealed class CmdletWriteConsoleMarkup : PSCmdlet {
        /// <summary><para type="description">The markup.</para></summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true)]
        [AllowEmptyString]
        public string Text { get; set; } = string.Empty;

        /// <inheritdoc />
        protected override void ProcessRecord() {
            HostOutput.WriteMarkup(this, Text);
        }
    }
}
