using System;
using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Reflection;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Layout;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// Where cmdlet output goes. On a real local interactive console it is drawn directly (colour, live dashboards).
    /// Everywhere else (background and thread jobs, remoting, SSH, the ISE, redirected output, an active transcript) it is
    /// written the way Write-Host writes: information records tagged PSHOST, one per line, on the pipeline thread, as plain
    /// text without escape sequences. Jobs and remoting then receive it, transcripts record it and <c>6&gt;&amp;1</c>
    /// captures it; like Write-Host, assigning the output to a variable captures nothing.
    /// </summary>
    internal static class HostOutput {
        private static readonly string[] HostTags = { "PSHOST" };

        /// <summary>Writes a widget once for a cmdlet.</summary>
        /// <param name="cmdlet">The calling cmdlet (its pipeline thread writes host output).</param>
        /// <param name="widget">The widget.</param>
        /// <param name="profile">The profile, or null for <see cref="Terminal.GetCurrentProfile"/>.</param>
        /// <param name="explicitWidth">Whether the profile width was set by the caller (-Width) and must be kept.</param>
        internal static void Write(PSCmdlet cmdlet, IWidget widget, TerminalProfile? profile = null, bool explicitWidth = false) {
            TerminalProfile p = profile ?? Terminal.GetCurrentProfile();
            if (!UseHost(cmdlet, p)) {
                Terminal.Write(widget, p);
                return;
            }
            using (HostInformationWriter writer = new HostInformationWriter(cmdlet)) {
                Terminal.Write(widget, HostProfile(cmdlet, p, explicitWidth), writer);
            }
        }

        /// <summary>Writes one line of markup for a cmdlet.</summary>
        /// <param name="cmdlet">The calling cmdlet.</param>
        /// <param name="markup">The markup.</param>
        internal static void WriteMarkup(PSCmdlet cmdlet, string markup) {
            TerminalProfile p = Terminal.GetCurrentProfile();
            if (!UseHost(cmdlet, p)) {
                Terminal.WriteMarkup(markup, p, null);
                return;
            }
            using (HostInformationWriter writer = new HostInformationWriter(cmdlet)) {
                Terminal.WriteMarkup(markup, HostProfile(cmdlet, p, false), writer);
            }
        }

        /// <summary>Whether output must go through the PowerShell host instead of the console handle.</summary>
        /// <param name="cmdlet">The calling cmdlet.</param>
        /// <param name="profile">The detected profile.</param>
        /// <returns>True for anything but a real local interactive console.</returns>
        internal static bool UseHost(PSCmdlet cmdlet, TerminalProfile profile) {
            // Background jobs ("ServerRemoteHost"), thread jobs and dashboard runspaces ("Default Host"), remoting and the ISE.
            if (!string.Equals(cmdlet.Host?.Name, "ConsoleHost", StringComparison.Ordinal)) {
                return true;
            }
            bool windows = Environment.OSVersion.Platform == PlatformID.Win32NT;
            if (Console.IsOutputRedirected || (windows && !profile.IsConsoleHandle)) {
                return true;
            }
            return TranscriptActive(cmdlet.Host);
        }

        /// <summary>The profile for host output: line output without colour, as wide as the host reports when no width was given.</summary>
        /// <param name="cmdlet">The calling cmdlet.</param>
        /// <param name="profile">The detected profile.</param>
        /// <param name="explicitWidth">Whether to keep the profile width.</param>
        /// <returns>The host profile.</returns>
        internal static TerminalProfile HostProfile(PSCmdlet cmdlet, TerminalProfile profile, bool explicitWidth) {
            TerminalProfile p = profile.WithColorDepth(ColorDepth.None);
            if (p.Mode == OutputMode.Interactive) {
                p = p.WithMode(OutputMode.Stream);
            }
            if (!explicitWidth) {
                int width = HostWidth(cmdlet.Host);
                if (width > 0) {
                    p = p.WithSize(width, p.Height);
                }
            }
            return p;
        }

        /// <summary>Writes one line as Write-Host does (an information record tagged PSHOST).</summary>
        /// <param name="cmdlet">The cmdlet on whose pipeline thread this runs.</param>
        /// <param name="line">The line.</param>
        internal static void WriteHostLine(PSCmdlet cmdlet, string line) {
            cmdlet.WriteInformation(new HostInformationMessage { Message = line, NoNewLine = false }, HostTags);
#if NETFRAMEWORK
            // Windows PowerShell 5.1 does not transcribe information records by itself; its Write-Host calls
            // TranscribeResult as well (PowerShell 7 transcribes PSHOST records centrally, so this would duplicate there).
            // TranscribeResult is internal in 5.1 (as is the transcription state read below); if it is not there, skip it.
            try {
                PSHostUserInterface? ui = cmdlet.Host?.UI;
                if (ui != null && TranscribeResult != null) {
                    TranscribeResult.Invoke(ui, new object[] { line });
                }
            } catch (Exception) {
                // Best effort: a host without transcription support.
            }
#endif
        }

#if NETFRAMEWORK
        private static readonly MethodInfo? TranscribeResult = typeof(PSHostUserInterface).GetMethod(
            "TranscribeResult", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, new[] { typeof(string) }, null);
#endif

        // The ISE and remoting report the client's width; jobs often have no raw UI at all.
        private static int HostWidth(PSHost? host) {
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

        // Start-Transcript records host output but not direct console writes. The transcription state is internal to
        // PowerShell (PSHostUserInterface.TranscriptionData.Transcripts on 5.1 and 7); if it cannot be read, assume none.
        private static bool TranscriptActive(PSHost? host) {
            try {
                PSHostUserInterface? ui = host?.UI;
                if (ui == null) {
                    return false;
                }
                const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
                object? data = typeof(PSHostUserInterface).GetProperty("TranscriptionData", Flags)?.GetValue(ui, null);
                object? transcripts = data?.GetType().GetProperty("Transcripts", Flags)?.GetValue(data, null);
                return transcripts is ICollection list && list.Count > 0;
            } catch (Exception) {
                return false;
            }
        }
    }
}
