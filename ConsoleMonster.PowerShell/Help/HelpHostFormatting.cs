using System;
using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ConsoleMonster.PowerShell {
    /// <summary>Connects native help to the existing help view after proving its direct console destination.</summary>
    internal static class HelpHostFormatting {
        private sealed class PagingState { internal bool Quit; }
        private static readonly ConditionalWeakTable<object, PagingState> PagedPipes = new ConditionalWeakTable<object, PagingState>();
        /// <summary>Finds the native Get-Help command physically connected to the implicit Out-Default shim.</summary>
        /// <param name="sink">The running Out-Default function.</param>
        /// <returns>The upstream native cmdlet, or null for captures, jobs, other commands, or unknown engine state.</returns>
        public static PSCmdlet? FindSource(PSCmdlet sink) {
            if (sink == null) { throw new ArgumentNullException(nameof(sink)); }
            if ((ConsoleFormatting.Features & ConsoleFormattingFeature.Help) == 0) { return null; }
            try {
                object? input = FormatCommandRouter.Member(sink.CommandRuntime, "InputPipe");
                Runspace? runspace = Runspace.DefaultRunspace;
                object? local = runspace?.GetType().GetMethod("GetCurrentlyRunningPipeline", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.Invoke(runspace, null);
                object? stopper = FormatCommandRouter.Member(local, "Stopper");
                if (input == null || !(FormatCommandRouter.Member(stopper, "_stack") is IEnumerable pipelines)) { return null; }
                foreach (object pipeline in pipelines) {
                    if (!(FormatCommandRouter.Member(pipeline, "Commands") is IEnumerable commands)) { continue; }
                    foreach (object processor in commands) {
                        if (!(FormatCommandRouter.Member(processor, "Command") is PSCmdlet source)
                            || !(source.MyInvocation.MyCommand is CmdletInfo info)
                            || info.ImplementingType.FullName != "Microsoft.PowerShell.Commands.GetHelpCommand"
                            || !ReferenceEquals(FormatCommandRouter.Member(source.CommandRuntime, "OutputPipe"), input)) { continue; }
                        return source;
                    }
                }
            } catch (Exception ex) when (!(ex is PipelineStoppedException)) {
                // Unreadable engine internals always retain PowerShell's own output.
            }
            return null;
        }

        /// <summary>Gets the native runtime help body for the installed adapter's pass-through path.</summary>
        /// <returns>The original built-in body, independent of subsequent function-drive mutations.</returns>
        public static ScriptBlock NativeHelp() => FormattingSessionState.Current.Help?.NativeHelp
            ?? throw new InvalidOperationException(HelpMessages.Get("AdapterNotInstalled"));

        /// <summary>Marks the proven direct help function's output pipe for paging while its native producer runs.</summary>
        /// <param name="source">The installed help function, after its host route has been checked.</param>
        public static void BeginPaging(PSCmdlet source) {
            if (source == null) { throw new ArgumentNullException(nameof(source)); }
            if (!FormatCommandRouter.IsDirectHost(source, ConsoleFormattingFeature.Help)) { return; }
            object? pipe = FormatCommandRouter.Member(source.CommandRuntime, "OutputPipe");
            if (pipe != null) { PagedPipes.GetValue(pipe, key => new PagingState()); }
        }

        /// <summary>Releases the help function's paging marker after its steppable pipeline completes.</summary>
        /// <param name="source">The help function whose native producer completed.</param>
        public static void EndPaging(PSCmdlet source) {
            if (source == null) { throw new ArgumentNullException(nameof(source)); }
            object? pipe = FormatCommandRouter.Member(source.CommandRuntime, "OutputPipe");
            if (pipe != null) { PagedPipes.Remove(pipe); }
        }

        internal static bool IsPaged(PSCmdlet source) {
            object? pipe = FormatCommandRouter.Member(source.CommandRuntime, "OutputPipe");
            return pipe != null && PagedPipes.TryGetValue(pipe, out _);
        }

        internal static bool IsQuit(PSCmdlet source) {
            object? pipe = FormatCommandRouter.Member(source.CommandRuntime, "OutputPipe");
            return pipe != null && PagedPipes.TryGetValue(pipe, out PagingState? state) && state.Quit;
        }

        internal static void Quit(PSCmdlet source) {
            object? pipe = FormatCommandRouter.Member(source.CommandRuntime, "OutputPipe");
            if (pipe != null && PagedPipes.TryGetValue(pipe, out PagingState? state)) { state.Quit = true; }
        }

        // End/Stop also run when a PowerShell 5.1 function is interrupted before its end block.
        internal static void EndSink(PSCmdlet? sink) {
            object? pipe = sink == null ? null : FormatCommandRouter.Member(sink.CommandRuntime, "InputPipe");
            if (pipe != null) { PagedPipes.Remove(pipe); }
        }

        internal static Layout.IWidget? CreateView(PSCmdlet source, PSObject help) {
            if (source == null) { throw new ArgumentNullException(nameof(source)); }
            if (help == null) { return null; }
            if (!FormatCommandRouter.IsDirectHost(source, ConsoleFormattingFeature.Help)
                || source.MyInvocation.BoundParameters.ContainsKey("Online") || source.MyInvocation.BoundParameters.ContainsKey("ShowWindow")) { return null; }
            if (help.BaseObject is string text) {
                // About-topic text uses the same reader as Show-Console -View Markdown; native captured routes return above.
                return global::ConsoleMonster.Markdown.MarkdownConsole.Parse(text);
            }
            bool parameter = help.TypeNames.Contains("MamlCommandHelpInfo#parameter")
                || help.TypeNames.Contains("ExtendedCmdletHelpInfo#parameter") || help.TypeNames.Contains("CmdletHelpInfo#parameter");
            if (!parameter && !help.TypeNames.Contains("MamlCommandHelpInfo") && !help.TypeNames.Contains("CmdletHelpInfo")) { return null; }
            string? name = Convert.ToString(source.MyInvocation.BoundParameters.TryGetValue("Name", out object? requested) ? requested : help.Properties["Name"]?.Value,
                System.Globalization.CultureInfo.InvariantCulture);
            CommandInfo? command = string.IsNullOrEmpty(name) ? null : source.InvokeCommand.GetCommand(name, CommandTypes.All);
            if (parameter) {
                // Native -Parameter returns parameter entries, not the enclosing topic. Wrap only for the reader.
                PSObject container = new PSObject();
                container.Properties.Add(new PSNoteProperty("parameter", help));
                PSObject topic = new PSObject();
                topic.Properties.Add(new PSNoteProperty("Name", command?.Name ?? name));
                topic.Properties.Add(new PSNoteProperty("parameters", container));
                return PSHelp.CreateView(topic, command, parametersOnly: true);
            }
            return PSHelp.CreateView(help, command, nativeSections: true);
        }
    }
}
