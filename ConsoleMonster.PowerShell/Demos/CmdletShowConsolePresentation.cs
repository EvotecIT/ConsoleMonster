using System.IO;
using System.Management.Automation;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Presents a scripted demo one command at a time in a real shell.</para>
    /// <para type="description">Enter types and runs; Space types without running; b moves back without undoing side effects; s skips; q quits. The terminal title shows the step counter. ScriptPath selects the input demo script. Optional -Path saves the same child output as asciicast; without it temporary capture files are removed on exit. Stop or cancellation terminates only the owned child and restores the title.</para>
    /// </summary>
    /// <example>
    /// <para>Preview presenting and recording a demo script</para>
    /// <code># WhatIf previews the operation without executing the script or writing the recording.
    /// Show-ConsolePresentation -ScriptPath ./demo.ps1 -Path ./talk.cast -WhatIf</code>
    /// </example>
    [Cmdlet(VerbsCommon.Show, "ConsolePresentation", SupportsShouldProcess = true)]
    [OutputType(typeof(FileInfo))]
    public sealed class CmdletShowConsolePresentation : ConsoleDemoCommand {
        /// <summary><para type="description">The asciicast output file. Supplying Path records the complete presentation unattended. Without it, keys control the presentation and temporary output is removed.</para></summary>
        [Parameter] public string? Path { get; set; }
        /// <inheritdoc />
        protected override void EndProcessing() {
            if (ShouldProcess(ScriptPath, Path == null ? "Present and run commands" : "Run commands and save the presentation")) {
                RunDemo(Path, Path == null);
            }
        }
    }
}
