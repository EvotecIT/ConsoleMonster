using System.Management.Automation;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Writes a horizontal rule, optionally with a title.</para>
    /// <para type="description">A quick section separator for scripts.</para>
    /// <example>
    ///   <para>A section header</para>
    ///   <code>Write-ConsoleRule 'Step 2: collect'</code>
    /// </example>
    /// </summary>
    [Cmdlet(VerbsCommunications.Write, "ConsoleRule")]
    public sealed class CmdletWriteConsoleRule : PSCmdlet {
        /// <summary><para type="description">The title (markup allowed).</para></summary>
        [Parameter(Position = 0)]
        public object? Title { get; set; }

        /// <summary><para type="description">The width in cells (default the window), to match other output laid out with -Width.</para></summary>
        [Parameter]
        public int Width { get; set; }

        /// <inheritdoc />
        protected override void ProcessRecord() {
            Capabilities.TerminalProfile? profile = Width > 0 ? Terminal.GetCurrentProfile().WithSize(Width, Terminal.GetCurrentProfile().Height) : null;
            HostOutput.Write(this, new Rule(PSText.Markup(Title)), profile, Width > 0);
        }
    }
}
