using System;
using System.Management.Automation;

namespace ConsoleMonster.PowerShell {
    /// <summary>PowerShell session formatting configuration, shared by the cmdlets and C# runspace hosts.</summary>
    public sealed class ConsoleFormattingOptions {
        /// <summary>The features to enable or disable; null selects the operation's default.</summary>
        public ConsoleFormattingFeature[]? Feature { get; set; }
        /// <summary>Allows replacing supported existing host functions.</summary>
        public bool Force { get; set; }
        /// <summary>Includes built-in type views with the output feature.</summary>
        public bool BuiltIn { get; set; } = true;
        /// <summary>Includes detailed error stacks.</summary>
        public bool Detailed { get; set; }
        /// <summary>Returns profile setup lines through the host instead of changing the session.</summary>
        public bool ProfileSnippet { get; set; }
    }

    /// <summary>Installs and restores formatting in the source cmdlet's owning PowerShell runspace. The dependency-free core does not own PowerShell session state.</summary>
    public static class ConsoleHostFormatting {
        /// <summary>Enables the requested features on the owning pipeline thread.</summary>
        /// <param name="source">The C# host cmdlet supplying its runspace, streams and module lifetime.</param>
        /// <param name="options">Feature and installation policy.</param>
        public static void Enable(PSCmdlet source, ConsoleFormattingOptions? options = null) {
            if (source == null) { throw new ArgumentNullException(nameof(source)); }
            options ??= new ConsoleFormattingOptions();

            ConsoleFormattingFeature wanted = ConsoleFormattingFeature.None;
            foreach (ConsoleFormattingFeature feature in options.Feature ?? new[] { ConsoleFormattingFeature.Output }) {
                wanted |= feature;
            }
            if (options.ProfileSnippet) {
                foreach (string line in ConsoleFormatting.ProfileSnippet(Convert.ToString(source.SessionState.PSVariable.GetValue("PROFILE"), System.Globalization.CultureInfo.InvariantCulture), !options.BuiltIn, options.Force, wanted)) {
                    source.WriteObject(options.Detailed && line.StartsWith("Enable-ConsoleFormatting", StringComparison.Ordinal) ? line + " -Detailed" : line);
                }
                return;
            }
            if (wanted == ConsoleFormattingFeature.None) {
                return;
            }
            if ((wanted & ConsoleFormattingFeature.Prompt) != 0) {
                if (PromptInstallation.Install(source, options.Force)) {
                    ConsoleFormatting.Features |= ConsoleFormattingFeature.Prompt;
                    ConsoleFormatting.RestoreCommandsWithModule(source.MyInvocation.MyCommand.Module);
                }
                wanted &= ~ConsoleFormattingFeature.Prompt;
                if (wanted == ConsoleFormattingFeature.None) { return; }
            }
            if ((wanted & ConsoleFormattingFeature.GridView) != 0) {
                if (GridViewInstallation.Install(source, options.Force)) {
                    ConsoleFormatting.Features |= ConsoleFormattingFeature.GridView;
                    ConsoleFormatting.RestoreCommandsWithModule(source.MyInvocation.MyCommand.Module);
                }
                wanted &= ~ConsoleFormattingFeature.GridView;
                if (wanted == ConsoleFormattingFeature.None) { return; }
            }
            const ConsoleFormattingFeature cosmetic = ConsoleFormattingFeature.Streams | ConsoleFormattingFeature.ReadLine;
            if ((wanted & ~cosmetic) == ConsoleFormattingFeature.None) {
                ApplyNativeColors(source, wanted);
                ConsoleFormatting.Features |= wanted;
                ConsoleFormatting.RestoreCommandsWithModule(source.MyInvocation.MyCommand.Module);
                return;
            }
            ConsoleModuleViews.Discover(source);
            ScriptBlock? existing = FormatCommandInstallation.GlobalFunction(source.SessionState, "Out-Default");
            bool ours = existing != null && existing.ToString().IndexOf(ConsoleFormatting.Marker, StringComparison.OrdinalIgnoreCase) >= 0;
            if (existing != null && !ours) {
                if (!options.Force) {
                    source.WriteWarning("An Out-Default function already exists in source session; it was kept. Use -Force to replace it.");
                    return;
                }
                ConsoleFormatting.SavedOutDefault = existing;
            }
            if ((wanted & ConsoleFormattingFeature.Progress) != 0 && !FormatCommandInstallation.InstallProgress(source, options.Force)) {
                wanted &= ~ConsoleFormattingFeature.Progress;
                if (wanted == ConsoleFormattingFeature.None) {
                    return;
                }
            }
            if ((wanted & ConsoleFormattingFeature.FormatCommands) != 0 && !FormatCommandInstallation.Install(source, options.Force)) {
                wanted &= ~ConsoleFormattingFeature.FormatCommands;
                if (wanted == ConsoleFormattingFeature.None) {
                    return;
                }
            }
            if ((wanted & ConsoleFormattingFeature.Help) != 0 && !FormatCommandInstallation.InstallHelp(source, options.Force)) {
                wanted &= ~ConsoleFormattingFeature.Help;
                if (wanted == ConsoleFormattingFeature.None) { return; }
            }
            if ((wanted & ConsoleFormattingFeature.Output) != 0) {
                ConsoleFormatting.TakeOverBuiltIns = options.BuiltIn;
            }
            ApplyNativeColors(source, wanted);
            ConsoleFormatting.Features |= wanted;
            if ((wanted & ConsoleFormattingFeature.Errors) != 0) {
                FormattingSessionState.Current.DetailedErrors = options.Detailed;
            }
            if (!ours) {
                source.SessionState.InvokeProvider.Item.Set("function:global:Out-Default", ScriptBlock.Create(ConsoleFormatting.ShimScript));
            }
            ConsoleFormatting.RestoreCommandsWithModule(source.MyInvocation.MyCommand.Module);
                }

        /// <summary>Restores the requested features in the owning runspace.</summary>
        /// <param name="source">The C# host cmdlet supplying its runspace and module lifetime.</param>
        /// <param name="feature">Features to disable; null restores all installed features.</param>
        public static void Disable(PSCmdlet source, ConsoleFormattingFeature[]? feature = null) {
            if (source == null) { throw new ArgumentNullException(nameof(source)); }

            ConsoleFormattingFeature off = ConsoleFormattingFeature.None;
            foreach (ConsoleFormattingFeature selectedFeature in feature ?? new[] { ConsoleFormattingFeature.All | ConsoleFormattingFeature.Progress | ConsoleFormattingFeature.FormatCommands | ConsoleFormattingFeature.Help | ConsoleFormattingFeature.Errors | ConsoleFormattingFeature.GridView | ConsoleFormattingFeature.Prompt }) {
                off |= selectedFeature;
            }
            if ((off & ConsoleFormattingFeature.Prompt) != 0) { PromptInstallation.Remove(FormattingSessionState.Current); }
            if ((off & ConsoleFormattingFeature.GridView) != 0) {
                GridViewInstallation.Remove(FormattingSessionState.Current);
            }
            if ((off & ConsoleFormattingFeature.Progress) != 0) {
                ConsoleFormatting.RemoveProgress();
            }
            if ((off & ConsoleFormattingFeature.FormatCommands) != 0) {
                FormatCommandInstallation.Remove(FormattingSessionState.Current);
            }
            if ((off & ConsoleFormattingFeature.Help) != 0) {
                FormatCommandInstallation.RemoveHelp(FormattingSessionState.Current);
            }
            if ((off & ConsoleFormattingFeature.Streams) != 0) {
                FormattingSessionState.Current.Streams.Restore();
            }
            if ((off & ConsoleFormattingFeature.ReadLine) != 0) {
                FormattingSessionState.Current.ReadLine.Restore();
            }
            ConsoleFormatting.Features &= ~off;
            if ((off & ConsoleFormattingFeature.Errors) != 0) { FormattingSessionState.Current.DetailedErrors = false; }
            if ((ConsoleFormatting.Features & ~(ConsoleFormattingFeature.Streams | ConsoleFormattingFeature.ReadLine | ConsoleFormattingFeature.GridView | ConsoleFormattingFeature.Prompt)) != ConsoleFormattingFeature.None) {
                return;
            }
            ScriptBlock? existing = FormatCommandInstallation.GlobalFunction(source.SessionState, "Out-Default");
            if (existing != null && existing.ToString().IndexOf(ConsoleFormatting.Marker, StringComparison.OrdinalIgnoreCase) >= 0) {
                if (ConsoleFormatting.SavedOutDefault != null) {
                    source.SessionState.InvokeProvider.Item.Set("function:global:Out-Default", ConsoleFormatting.SavedOutDefault);
                } else {
                    FormatCommandInstallation.RemoveGlobalFunction(source.MyInvocation.MyCommand.Module, "Out-Default");
                }
            }
            ConsoleFormatting.SavedOutDefault = null;
                }

        private static void ApplyNativeColors(PSCmdlet source, ConsoleFormattingFeature wanted) {

            FormattingSessionState state = FormattingSessionState.Current;
            if ((wanted & ConsoleFormattingFeature.Streams) != 0) { state.Streams.Apply(source); }
            if ((wanted & ConsoleFormattingFeature.ReadLine) != 0) { state.ReadLine.Apply(source); }
                }
    }
}
