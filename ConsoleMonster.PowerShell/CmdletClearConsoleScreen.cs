using System;
using System.Management.Automation;
using System.Management.Automation.Host;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Clears the console screen, safely.</para>
    /// <para type="description">On an interactive console the screen and its scrollback are cleared and the cursor goes to the top left, like Clear-Host. It does nothing where clearing would only lose output: redirected output, CI logs, plain mode (CONSOLEMONSTER_MODE=plain), background and thread jobs, and while a live dashboard owns the console. In the ISE and other hosts with a screen buffer it clears that buffer the way Clear-Host does.</para>
    /// </summary>
    /// <example>
    ///   <para>Clear the screen before displaying a message</para>
    ///   <code>Clear-ConsoleScreen; Write-ConsoleText 'Ready'</code>
    /// </example>
    [Cmdlet(VerbsCommon.Clear, "ConsoleScreen")]
    public sealed class CmdletClearConsoleScreen : PSCmdlet {
        /// <inheritdoc />
        protected override void ProcessRecord() {
            string host = Host?.Name ?? string.Empty;
            if (string.Equals(host, "ConsoleHost", StringComparison.Ordinal)) {
                // Terminal.Clear checks redirection, CI, plain mode and a live owner itself.
                Terminal.Clear();
                return;
            }
            if (host.IndexOf("ISE", StringComparison.OrdinalIgnoreCase) < 0) {
                // Jobs, remoting and other hosts: there is no screen of ours to clear.
                return;
            }
            try {
                PSHostRawUserInterface raw = Host!.UI.RawUI;
                raw.SetBufferContents(new Rectangle(-1, -1, -1, -1), new BufferCell(' ', raw.ForegroundColor, raw.BackgroundColor, BufferCellType.Complete));
                raw.CursorPosition = new Coordinates(0, 0);
            } catch (Exception ex) when (ex is NotImplementedException || ex is NotSupportedException || ex is HostException || ex is InvalidOperationException) {
                // A host without a screen buffer: nothing to clear.
            }
        }
    }
}
