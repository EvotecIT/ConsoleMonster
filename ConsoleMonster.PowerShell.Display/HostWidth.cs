using System;
using System.Management.Automation.Host;
using ConsoleMonster.Capabilities;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// The width rule for PowerShell output without an explicit -Width: <c>COLUMNS</c> when output is redirected and it is
    /// set (files, pipes, CI logs), otherwise the width the PowerShell host reports (the console window, the ISE, a
    /// remoting client), otherwise the detected width. An assigned <see cref="Terminal.Profile"/> and another target keep
    /// their own width. Every cmdlet and Get-ConsoleCapability use it.
    /// </summary>
    internal static class HostWidth {
        /// <summary>The automatic width for output written through a host.</summary>
        /// <param name="host">The host, or null.</param>
        /// <param name="profile">The detected profile.</param>
        /// <returns>A positive width in cells.</returns>
        internal static int Resolve(PSHost? host, TerminalProfile profile) {
            // COLUMNS for redirected output, an assigned profile and another target (Use-ConsoleTarget) are kept as they are.
            if (profile.WidthFromColumns || Terminal.HasExplicitProfile || !Terminal.IsConsole(Terminal.Target)) {
                return profile.Width;
            }
            int width = Visible(host);
            return width > 0 ? width : profile.Width;
        }

        /// <summary>The visible width the host reports (the buffer width when it has no window), or 0 when it has none.</summary>
        /// <param name="host">The host, or null.</param>
        /// <returns>The width, or 0.</returns>
        internal static int Visible(PSHost? host) {
            // The ISE and remoting report the client's width; jobs often have no raw UI at all.
            try {
                int width = host?.UI?.RawUI?.WindowSize.Width ?? 0;
                if (width <= 0) {
                    width = host?.UI?.RawUI?.BufferSize.Width ?? 0;
                }
                return width;
            } catch (Exception) {
                return 0;
            }
        }
    }
}
