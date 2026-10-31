using System.Management.Automation;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Graphics;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Shows an image in the console, at the size you ask for in cells.</para>
    /// <para type="description">Draws real pixels in terminals with an image protocol: Kitty and Ghostty (Kitty graphics), iTerm2 and WezTerm (iTerm2 inline images) and Windows Terminal (Sixel). Other terminals with 256 colours or more get half blocks. Redirected, CI, remote, 16-colour and no-colour output, the ISE and sessions with a transcript running get one line, [image: name WxH]. Get-ConsoleCapability shows the protocol in ImageProtocol. The aspect ratio is always kept. To put an image inside a panel or dashboard, use New-ConsoleImage.</para>
    /// </summary>
    /// <example>
    ///   <para>Show a chart 40 cells wide</para>
    ///   <code>Show-ConsoleImage -Path ./chart.png -Width 40</code>
    /// </example>
    /// <example>
    ///   <para>Preview every screenshot in a folder</para>
    ///   <code>Get-ChildItem ./screens -Filter *.png | Show-ConsoleImage -Height 12</code>
    /// </example>
    /// <example>
    ///   <para>Use half blocks, for example over a connection that drops image sequences</para>
    ///   <code>Show-ConsoleImage -Path ./logo.png -Width 24 -Protocol HalfBlock</code>
    /// </example>
    /// <example>
    ///   <para>Use Kitty graphics in a terminal that supports it but isn't detected</para>
    ///   <code>Show-ConsoleImage -Path ./diagram.png -Protocol Kitty</code>
    /// </example>
    [Cmdlet(VerbsCommon.Show, "ConsoleImage", DefaultParameterSetName = "Path")]
    public sealed class CmdletShowConsoleImage : ConsoleImageCommand {
        /// <summary><para type="description">The protocol your terminal supports, when detection chose another one: Kitty, ITerm2, Sixel, HalfBlock or None (the placeholder). Redirected and CI output keep the placeholder.</para></summary>
        [Parameter]
        public ImageProtocol? Protocol { get; set; }

        /// <inheritdoc />
        protected override void ProcessRecord() {
            ConsoleImage? image = CreateImage();
            if (image == null) {
                return;
            }
            TerminalProfile profile = Terminal.GetCurrentProfile();
            if (Protocol.HasValue) {
                profile = profile.WithImageProtocol(Protocol.Value);
            }
            if (HostOutput.UseHost(this, profile)) {
                // Host output (transcripts, jobs, remoting) maps colours to the 16 console colours: the placeholder reads better.
                HostOutput.Write(this, image, profile.WithImageProtocol(ImageProtocol.None));
                return;
            }
            image.Write(profile);
        }
    }
}
