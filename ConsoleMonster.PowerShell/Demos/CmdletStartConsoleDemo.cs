using System.IO;
using System.Management.Automation;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Presents a scripted demo one command at a time in a real shell.</para>
    /// <para type="description">Enter types and runs; Space types without running; b moves back without undoing side effects; s skips; q quits. The terminal title shows the step counter. Optional -Record saves the same child output as asciicast; without it temporary capture files are removed on exit. Stop or cancellation terminates only the owned child and restores the title.</para>
    /// </summary>
    /// <example><para>Present and record</para><code>Start-ConsoleDemo -Path ./demo.ps1 -Record ./talk.cast</code></example>
    [Cmdlet(VerbsLifecycle.Start, "ConsoleDemo")]
    [OutputType(typeof(FileInfo))]
    public sealed class CmdletStartConsoleDemo : ConsoleDemoCommand {
        /// <summary><para type="description">Optional asciicast recording path.</para></summary>
        [Parameter] public string? Record { get; set; }
        /// <inheritdoc />
        protected override void EndProcessing() => RunDemo(Record, true);
    }
}
