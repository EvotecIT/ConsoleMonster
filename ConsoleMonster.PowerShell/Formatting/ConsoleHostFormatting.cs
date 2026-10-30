using System;
using System.Management.Automation;

namespace ConsoleMonster.PowerShell {
    /// <summary>PowerShell session formatting configuration, shared by the cmdlets and C# runspace hosts.</summary>
    public sealed class ConsoleFormattingOptions {
        /// <summary>Features to add to the default set.</summary>
        public ConsoleFormattingFeature[]? Include { get; set; }
        /// <summary>Features to remove from the default set and disable if already installed.</summary>
        public ConsoleFormattingFeature[]? Exclude { get; set; }
        /// <summary>Allows replacing supported existing host functions.</summary>
        public bool Force { get; set; }
        /// <summary>Includes built-in type views with the output feature.</summary>
        public bool BuiltIn { get; set; } = true;
        /// <summary>Error detail: Compact, Standard or Detailed.</summary>
        public string ErrorLayout { get; set; } = "Standard";
        internal bool Detailed => string.Equals(ErrorLayout, "Detailed", StringComparison.OrdinalIgnoreCase);
        internal bool CompactErrors => !string.Equals(ErrorLayout, "Standard", StringComparison.OrdinalIgnoreCase);
        /// <summary>Returns profile setup lines through the host instead of changing the session.</summary>
        public bool ProfileSnippet { get; set; }
    }

    /// <summary>Installs and restores formatting in the source cmdlet's owning PowerShell runspace. The dependency-free core does not own PowerShell session state.</summary>
    public static class ConsoleHostFormatting {
        internal static ConsoleFormattingFeature Combine(ConsoleFormattingFeature[]? features) {
            ConsoleFormattingFeature result = ConsoleFormattingFeature.None;
            if (features != null) { foreach (ConsoleFormattingFeature feature in features) { result |= feature; } }
            return result;
        }

        internal static ConsoleFormattingFeature Selected(ConsoleFormattingOptions options) {
            return (ConsoleFormatting.Defaults | Combine(options.Include)) & ~Combine(options.Exclude);
        }
        /// <summary>Enables the requested features on the owning pipeline thread.</summary>
        /// <param name="source">The C# host cmdlet supplying its runspace, streams and module lifetime.</param>
        /// <param name="options">Feature and installation policy.</param>
        public static void Enable(PSCmdlet source, ConsoleFormattingOptions? options = null) {
            if (source == null) { throw new ArgumentNullException(nameof(source)); }
            options ??= new ConsoleFormattingOptions();
            if (!string.Equals(options.ErrorLayout, "Standard", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(options.ErrorLayout, "Compact", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(options.ErrorLayout, "Detailed", StringComparison.OrdinalIgnoreCase)) { throw new ArgumentException("ErrorLayout must be Compact, Standard or Detailed.", nameof(options)); }

            ConsoleFormattingFeature wanted = Selected(options);
            if ((options.CompactErrors || options.Detailed) && (wanted & ConsoleFormattingFeature.Errors) == 0) {
                throw new ArgumentException("ErrorLayout requires the Errors feature.");
            }
            if (options.ProfileSnippet) {
                foreach (string line in ConsoleFormatting.ProfileSnippet(Convert.ToString(source.SessionState.PSVariable.GetValue("PROFILE"), System.Globalization.CultureInfo.InvariantCulture), !options.BuiltIn, options.Force, Combine(options.Include), Combine(options.Exclude), options.ErrorLayout)) {
                    source.WriteObject(line);
                }
                return;
            }
            if (options.Exclude != null) { Disable(source, options.Exclude); }
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
            ConsoleFormattingFeature colours = wanted & cosmetic;
            if (colours != ConsoleFormattingFeature.None) {
                ApplyNativeColors(source, colours);
                ConsoleFormatting.Features |= colours;
                ConsoleFormatting.RestoreCommandsWithModule(source.MyInvocation.MyCommand.Module);
            }
            wanted &= ~cosmetic;
            if (wanted == ConsoleFormattingFeature.None) { return; }
            ConsoleModuleViews.Discover(source);
            ScriptBlock? existing = FormatCommandInstallation.GlobalFunction(source.SessionState, "Out-Default");
            bool ours = Display.DefaultDisplay.IsBrokerInstalled || (existing != null && existing.ToString().IndexOf(ConsoleFormatting.Marker, StringComparison.OrdinalIgnoreCase) >= 0);
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
            if ((wanted & ConsoleFormattingFeature.Progress) != 0) {
                ConsoleFormatting.Features |= ConsoleFormattingFeature.Progress;
                ConsoleFormatting.RestoreCommandsWithModule(source.MyInvocation.MyCommand.Module);
            }
            if ((wanted & ConsoleFormattingFeature.FormatCommands) != 0 && !FormatCommandInstallation.Install(source, options.Force)) {
                wanted &= ~ConsoleFormattingFeature.FormatCommands;
                if (wanted == ConsoleFormattingFeature.None) {
                    return;
                }
            }
            if ((wanted & ConsoleFormattingFeature.FormatCommands) != 0) {
                ConsoleFormatting.Features |= ConsoleFormattingFeature.FormatCommands;
                ConsoleFormatting.RestoreCommandsWithModule(source.MyInvocation.MyCommand.Module);
            }
            if ((wanted & ConsoleFormattingFeature.Help) != 0 && !FormatCommandInstallation.InstallHelp(source, options.Force)) {
                wanted &= ~ConsoleFormattingFeature.Help;
                if (wanted == ConsoleFormattingFeature.None) { return; }
            }
            if ((wanted & ConsoleFormattingFeature.Help) != 0) {
                ConsoleFormatting.Features |= ConsoleFormattingFeature.Help;
                ConsoleFormatting.RestoreCommandsWithModule(source.MyInvocation.MyCommand.Module);
            }
            if ((wanted & (ConsoleFormattingFeature.Output | ConsoleFormattingFeature.FormatCommands)) != 0) {
                ConsoleFormatting.TakeOverBuiltIns = options.BuiltIn;
            }
            ConsoleFormatting.Features |= wanted;
            ConsoleFormatting.RestoreCommandsWithModule(source.MyInvocation.MyCommand.Module);
            // Register the shared sink only after a feature actually installed. Refused progress/help
            // must not leave an Out-Default broker or module cleanup hook behind.
            EmbeddedDisplayIntegration.Join();
            EmbeddedDisplayIntegration.SetOutputEnabled((ConsoleFormatting.Features & ConsoleFormattingFeature.Output) != 0);
            if ((wanted & ConsoleFormattingFeature.Errors) != 0) {
                FormattingSessionState.Current.DetailedErrors = options.Detailed;
                FormattingSessionState.Current.CompactErrors = options.CompactErrors;
            }
            if (!ours && !Display.DefaultDisplay.IsBrokerInstalled) {
                FormatCommandInstallation.SetGlobalFunction(source.SessionState, "Out-Default", ScriptBlock.Create(ConsoleFormatting.ShimScript));
            }
        }

        /// <summary>Restores the requested features in the owning runspace.</summary>
        /// <param name="source">The C# host cmdlet supplying its runspace and module lifetime.</param>
        /// <param name="feature">Features to disable; null restores all installed features.</param>
        public static void Disable(PSCmdlet source, ConsoleFormattingFeature[]? feature = null) {
            if (source == null) { throw new ArgumentNullException(nameof(source)); }

            ConsoleFormattingFeature off = ConsoleFormattingFeature.None;
            foreach (ConsoleFormattingFeature selectedFeature in feature ?? new[] { ConsoleFormatting.Features }) {
                off |= selectedFeature;
            }
            off &= ConsoleFormatting.Features;
            if (off == ConsoleFormattingFeature.None) { return; }
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
            EmbeddedDisplayIntegration.SetOutputEnabled((ConsoleFormatting.Features & ConsoleFormattingFeature.Output) != 0);
            if ((off & ConsoleFormattingFeature.Errors) != 0) { FormattingSessionState.Current.DetailedErrors = false; FormattingSessionState.Current.CompactErrors = false; }
            if ((ConsoleFormatting.Features & ~(ConsoleFormattingFeature.Streams | ConsoleFormattingFeature.ReadLine | ConsoleFormattingFeature.GridView | ConsoleFormattingFeature.Prompt)) != ConsoleFormattingFeature.None) {
                return;
            }
            FormattingSessionState.Current.DisplayLease?.Dispose();
            FormattingSessionState.Current.DisplayLease = null;
            ScriptBlock? existing = FormatCommandInstallation.GlobalFunction(source.SessionState, "Out-Default");
            if (existing != null && existing.ToString().IndexOf(ConsoleFormatting.Marker, StringComparison.OrdinalIgnoreCase) >= 0) {
                if (ConsoleFormatting.SavedOutDefault != null) {
                    FormatCommandInstallation.SetGlobalFunction(source.SessionState, "Out-Default", ConsoleFormatting.SavedOutDefault);
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
