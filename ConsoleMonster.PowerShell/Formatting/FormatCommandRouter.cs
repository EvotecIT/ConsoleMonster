using System;
using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Reflection;

namespace ConsoleMonster.PowerShell {
    /// <summary>Conservative output-route check used by the runtime-native Format-* proxies.</summary>
    public static class FormatCommandRouter {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        /// <summary>Whether this command feeds the installed Out-Default directly on a local, non-transcribed console.</summary>
        /// <param name="cmdlet">The executing proxy function.</param>
        /// <returns>False for captures, another pipeline command, jobs, remoting, transcripts, or unknown engine state.</returns>
        public static bool IsDirectHost(PSCmdlet cmdlet) {
            return IsDirectHost(cmdlet, ConsoleFormattingFeature.FormatCommands);
        }

        /// <summary>Checks the conservative host route for an explicitly enabled formatting feature.</summary>
        /// <param name="cmdlet">The command whose success output is being routed.</param>
        /// <param name="feature">The feature required for this route.</param>
        /// <returns>Whether output reaches the local host without capture or transcription.</returns>
        public static bool IsDirectHost(PSCmdlet cmdlet, ConsoleFormattingFeature feature) {
            if (cmdlet == null) { throw new ArgumentNullException(nameof(cmdlet)); }
            return (ConsoleFormatting.Features & feature) != 0 && IsDirectHostOutput(cmdlet);
        }

        /// <summary>Checks the uncaptured local host route without requiring a formatting opt-in, for explicit interactive readers.</summary>
        internal static bool IsDirectHostOutput(PSCmdlet cmdlet) {
            if (!ConsoleFormatting.Displayed(cmdlet)
                || cmdlet.MyInvocation.PipelinePosition < cmdlet.MyInvocation.PipelineLength
                || Terminal.GetCurrentProfile().IsContinuousIntegration || cmdlet.MyInvocation.BoundParameters.ContainsKey("OutVariable")
                || cmdlet.MyInvocation.BoundParameters.ContainsKey("PipelineVariable")) {
                return false;
            }
            try {
                object? pipe = Member(cmdlet.CommandRuntime, "OutputPipe");
                object? downstream = Member(pipe, "DownstreamCmdlet");
                CommandInfo? command = Member(downstream, "CommandInfo") as CommandInfo;
                bool native = command is CmdletInfo && string.Equals(command.ModuleName, "Microsoft.PowerShell.Core", StringComparison.OrdinalIgnoreCase);
                bool shim = Display.DefaultDisplay.IsOwnedBroker(command)
                    || command is FunctionInfo function && function.Definition.IndexOf(ConsoleFormatting.Marker, StringComparison.OrdinalIgnoreCase) >= 0;
                if (command == null || !string.Equals(command.Name, "Out-Default", StringComparison.OrdinalIgnoreCase) || (!native && !shim)) {
                    return false;
                }
                return IsUntranscribed(cmdlet);
            } catch (Exception ex) when (!(ex is PipelineStoppedException)) {
                return false;
            }
        }

        /// <summary>Requires readable, inactive native transcription state before changing host presentation.</summary>
        internal static bool IsUntranscribed(PSCmdlet cmdlet) => IsUnrecorded(cmdlet.Host);

        internal static bool IsTranscribing(PSCmdlet cmdlet) => !IsUnrecorded(cmdlet.Host);

        internal static bool IsUnrecordedHost(PSCmdlet cmdlet) => ConsoleFormatting.Displayed(cmdlet)
            && !Terminal.GetCurrentProfile().IsContinuousIntegration && IsUnrecorded(cmdlet.Host);

        /// <summary>Unknown transcription state retains native output.</summary>
        internal static bool IsUnrecorded(PSHost host) {
            try {
                object? data = typeof(PSHostUserInterface).GetProperty("TranscriptionData", Flags)?.GetValue(host.UI, null);
                object? transcripts = Member(data, "Transcripts");
                return transcripts is ICollection list && list.Count == 0 && Member(data, "SystemTranscript") == null;
            } catch (Exception error) when (!(error is PipelineStoppedException)) { return false; }
        }

        internal static object? Member(object? value, string name) {
            if (value == null) { return null; }
            Type type = value.GetType();
            return type.GetProperty(name, Flags)?.GetValue(value, null) ?? type.GetField(name, Flags)?.GetValue(value);
        }
    }
}
