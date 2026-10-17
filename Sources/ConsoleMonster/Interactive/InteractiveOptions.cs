using System;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Input;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Interactive {
    /// <summary>
    /// Settings shared by prompts, menus and forms: how to interact (detected by default), where input comes from, the
    /// terminal profile, theme and labels. Every property is optional.
    /// </summary>
    public sealed class InteractiveOptions {
        private static InteractiveOptions _default = new InteractiveOptions();

        /// <summary>Process-wide defaults used when a prompt gets no options.</summary>
        public static InteractiveOptions Default {
            get => _default;
            set => _default = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>Forces an interaction mode; null detects it (see <see cref="ResolveMode"/>).</summary>
        public InteractionMode? Mode { get; set; }

        /// <summary>Treats the session as non-interactive (PowerShell's <c>-NonInteractive</c>): prompts use defaults or fail.</summary>
        public bool NonInteractive { get; set; }

        /// <summary>An input source instead of the console (a script in tests and demos).</summary>
        public IInputSource? Input { get; set; }

        /// <summary>The terminal profile; null uses <see cref="Terminal.GetCurrentProfile"/>.</summary>
        public TerminalProfile? Profile { get; set; }

        /// <summary>The theme; null uses <see cref="Terminal.Theme"/>.</summary>
        public ITheme? Theme { get; set; }

        /// <summary>The built-in words; null uses <see cref="PromptLabels.Default"/>.</summary>
        public PromptLabels? Labels { get; set; }

        /// <summary>Line input and output for the line-based and accessible modes; null uses the console.</summary>
        public ILineConsole? LineConsole { get; set; }

        /// <summary>The most rows an inline prompt may take (default: the window height minus two, at most 20).</summary>
        public int? MaxInlineHeight { get; set; }

        /// <summary>
        /// Where prompts draw in <see cref="InteractionMode.Rich"/> mode (tests use a recording surface). Null picks the
        /// right one: inline below the cursor, or an overlay over a running live dashboard.
        /// </summary>
        internal IPromptSurface? Surface { get; set; }

        internal PromptLabels ResolveLabels() => Labels ?? PromptLabels.Default;

        internal ILineConsole ResolveLineConsole() => LineConsole ?? ConsoleLineConsole.Instance;

        internal TerminalProfile ResolveProfile() => Profile ?? Terminal.GetCurrentProfile();

        internal ITheme ResolveTheme() => Theme ?? Terminal.Theme;

        /// <summary>
        /// Picks the interaction mode: an explicit <see cref="Mode"/> wins; then non-interactive sessions (the
        /// <see cref="NonInteractive"/> flag, <c>CONSOLEMONSTER_NONINTERACTIVE=1</c>, CI, redirected input without an input
        /// source) get <see cref="InteractionMode.None"/>; <c>CONSOLEMONSTER_ACCESSIBLE=1</c> gets
        /// <see cref="InteractionMode.Accessible"/>; consoles without VT get <see cref="InteractionMode.Line"/>; everything
        /// else <see cref="InteractionMode.Rich"/>.
        /// </summary>
        /// <returns>The mode.</returns>
        public InteractionMode ResolveMode() {
            if (Mode.HasValue) {
                return Mode.Value;
            }
            if (NonInteractive || IsSet("CONSOLEMONSTER_NONINTERACTIVE")) {
                return InteractionMode.None;
            }
            bool accessible = IsSet("CONSOLEMONSTER_ACCESSIBLE");
            if (Input != null || ConsoleInput.Override != null) {
                return accessible ? InteractionMode.Accessible : InteractionMode.Rich;
            }
            TerminalProfile profile = ResolveProfile();
            if (profile.IsContinuousIntegration) {
                return InteractionMode.None;
            }
            bool redirected;
            try {
                redirected = Console.IsInputRedirected;
            } catch (System.IO.IOException) {
                redirected = true;
            }
            if (redirected) {
                return LineConsole != null ? InteractionMode.Line : InteractionMode.None;
            }
            if (accessible) {
                return InteractionMode.Accessible;
            }
            if (profile.Mode != OutputMode.Interactive || !profile.SupportsVirtualTerminal || profile.IsOutputRedirected) {
                return InteractionMode.Line;
            }
            return InteractionMode.Rich;
        }

        /// <summary>Returns a copy.</summary>
        /// <returns>The copy.</returns>
        public InteractiveOptions Clone() {
            return (InteractiveOptions)MemberwiseClone();
        }

        private static bool IsSet(string name) {
            string? value = Environment.GetEnvironmentVariable(name);
            return value == "1" || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) || string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase);
        }
    }
}
