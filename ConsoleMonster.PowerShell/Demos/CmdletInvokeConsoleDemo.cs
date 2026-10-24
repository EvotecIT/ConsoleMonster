using System.IO;
using System.Management.Automation;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Types a scripted PowerShell demo into a real terminal and records it.</para>
    /// <para type="description">Runs plain PowerShell with optional chapter, pause, setup, type-only, narration, WPM and output-wait directives. Uses OSC 133 readiness marks independent of visible prompt text and operating system. Character jitter uses a fixed seed; use fixed data and environment for repeatable output.</para>
    /// </summary>
    /// <example><para>Record a talk</para><code>Invoke-ConsoleDemo -Path ./demo.ps1 -Destination ./talk.cast -Seed 42</code></example>
    [Cmdlet(VerbsLifecycle.Invoke, "ConsoleDemo")]
    [OutputType(typeof(FileInfo))]
    public sealed class CmdletInvokeConsoleDemo : ConsoleDemoCommand {
        /// <summary><para type="description">Destination asciicast v2 recording. Existing recordings are never overwritten.</para></summary>
        [Parameter(Mandatory = true, Position = 1)] public string Destination { get; set; } = string.Empty;
        /// <inheritdoc />
        protected override void EndProcessing() => RunDemo(Destination, false);
    }
}
