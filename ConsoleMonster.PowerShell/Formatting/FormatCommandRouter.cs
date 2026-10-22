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
            if (cmdlet == null) { throw new ArgumentNullException(nameof(cmdlet)); }
            if ((ConsoleFormatting.Features & ConsoleFormattingFeature.FormatCommands) == 0 || !ConsoleFormatting.Displayed(cmdlet)
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
                bool shim = command is FunctionInfo function && function.Definition.IndexOf(ConsoleFormatting.Marker, StringComparison.OrdinalIgnoreCase) >= 0;
                if (command == null || !string.Equals(command.Name, "Out-Default", StringComparison.OrdinalIgnoreCase) || (!native && !shim)) {
                    return false;
                }
                return IsUnrecordedHost(cmdlet);
            } catch (Exception ex) when (!(ex is PipelineStoppedException)) {
                return false;
            }
        }

        // Shared by host-only formatting surfaces. Unknown transcription state must retain native rendering.
        internal static bool IsUnrecordedHost(PSCmdlet cmdlet) {
            if (!ConsoleFormatting.Displayed(cmdlet) || Terminal.GetCurrentProfile().IsContinuousIntegration) { return false; }
            try {
                object? data = typeof(PSHostUserInterface).GetProperty("TranscriptionData", Flags)?.GetValue(cmdlet.Host.UI, null);
                object? transcripts = Member(data, "Transcripts");
                return transcripts is ICollection list && list.Count == 0 && Member(data, "SystemTranscript") == null;
            } catch (Exception ex) when (!(ex is PipelineStoppedException)) { return false; }
        }

        private static object? Member(object? value, string name) {
            if (value == null) { return null; }
            Type type = value.GetType();
            return type.GetProperty(name, Flags)?.GetValue(value, null) ?? type.GetField(name, Flags)?.GetValue(value);
        }
    }
}
