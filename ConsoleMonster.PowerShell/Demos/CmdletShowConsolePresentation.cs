using System.IO;
using System.Management.Automation;

namespace ConsoleMonster.PowerShell {
    /// <summary>Presents a scripted demo one command at a time in a real shell.</summary>
    /// <remarks>
    /// <para>ScriptPath selects the demo script that runs in a child PowerShell process.</para>
    /// <para>### Step-through presentation</para>
    /// <para>Run without Path to control the steps from the terminal. Enter types and runs the current command. Space types it without running it, b moves back, s skips a step and q quits. The title shows the step counter. Moving back preserves effects from commands already run. Stop or cancellation ends the owned child process and restores the terminal title. Temporary capture files are removed on exit.</para>
    /// <para>### Unattended recording</para>
    /// <para>Supplying Path runs the complete presentation unattended, saves the child output as an asciicast, and returns a FileInfo for the recording.</para>
    /// </remarks>
    /// <example>
    /// <para>Preview presenting and recording a demo script</para>
    /// <code># WhatIf previews the operation without executing the script or writing the recording.
    /// $presentationOptions = @{
    ///     WhatIf = $true
    ///     ScriptPath = './demo.ps1'
    ///     Path = './talk.cast'
    /// }
    /// Show-ConsolePresentation @presentationOptions</code>
    /// </example>
    /// <example>
    /// <para>Preview presenting an inventory demonstration</para>
    /// <code>$presentationOptions = @{
    ///     WhatIf = $true
    ///     ScriptPath = './inventory-demo.ps1'
    /// }
    /// Show-ConsolePresentation @presentationOptions</code>
    /// </example>
    [Cmdlet(VerbsCommon.Show, "ConsolePresentation", SupportsShouldProcess = true, HelpUri = "https://consolemonster.dev/powershell/commands/show-consolepresentation/")]
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
