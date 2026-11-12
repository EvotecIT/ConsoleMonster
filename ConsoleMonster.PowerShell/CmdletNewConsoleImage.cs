using System.Management.Automation;
using ConsoleMonster.Graphics;

namespace ConsoleMonster.PowerShell {
    /// <summary>Creates an image to place inside panels, columns and dashboards.</summary>
    /// <remarks><para>Inside layouts and live dashboards the image is drawn with half blocks on terminals with 256 colours or more, so it redraws cleanly with the rest of the screen; redirected and no-colour output gets [image: name WxH]. To print an image on its own with the terminal's real pixels, use Show-ConsoleImage.</para></remarks>
    /// <example>
    ///   <para>A chart next to its numbers</para>
    ///   <code>New-ConsoleColumns -Content { New-ConsolePanel -Title 'Latency' -Content { New-ConsoleImage -Path ./latency.png -Width 40 }; New-ConsolePanel -Title 'Summary' -Content { New-ConsoleKeyValue -InputObject ([ordered] @{ P50 = '41 ms'; P99 = '180 ms' }) } } | Show-Console</code>
    /// </example>
    /// <example>
    ///   <para>Create a widget from an open stream, leaving it open for the caller</para>
    ///   <code>New-ConsoleImage -Bytes $stream -Label 'Chart' -Width 40</code>
    /// </example>
    [Cmdlet(VerbsCommon.New, "ConsoleImage", DefaultParameterSetName = "Path", HelpUri = "https://consolemonster.dev/powershell/commands/new-consoleimage/")]
    [OutputType(typeof(ConsoleImage))]
    public sealed class CmdletNewConsoleImage : ConsoleImageCommand {
        /// <inheritdoc />
        protected override void ProcessRecord() {
            ConsoleImage? image = CreateImage();
            if (image != null) {
                WriteObject(image);
            }
        }
    }
}
