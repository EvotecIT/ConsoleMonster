using System;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Layout;
using ConsoleMonster.Output;
using ConsoleMonster.Rendering;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster {
    /// <summary>
    /// The print-once entry point: <c>Terminal.Write(table)</c>. Detects the terminal once (overridable through
    /// <see cref="Profile"/> or the <c>CONSOLEMONSTER_*</c> environment variables) and writes widgets in the right mode.
    /// Calls are serialized, so concurrent writers never interleave inside a block.
    /// </summary>
    public static class Terminal {
        private static readonly object Gate = new object();
        private static TerminalProfile? _profile;
        private static ITheme? _theme;
        private static ICapabilityProvider _provider = new CapabilityDetector();
        private static bool _defaultProvider = true;

        /// <summary>The capability provider used for detection.</summary>
        public static ICapabilityProvider CapabilityProvider {
            get => _provider;
            set {
                lock (Gate) {
                    _provider = value ?? throw new ArgumentNullException(nameof(value));
                    _defaultProvider = false;
                    _profile = null;
                }
            }
        }

        /// <summary>The active profile; detected on first use. Assign to override detection for the process.</summary>
        public static TerminalProfile Profile {
            get {
                lock (Gate) {
                    if (_profile == null) {
                        _profile = _provider.Detect();
                        if (_profile.WidthProfile.Source == "environment") {
                            DisplayWidth.Profile = _profile.WidthProfile;
                        }
                    }
                    return _profile;
                }
            }
            set {
                lock (Gate) {
                    _profile = value ?? throw new ArgumentNullException(nameof(value));
                }
            }
        }

        /// <summary>
        /// Chooses the light or dark theme: <c>CONSOLEMONSTER_THEME=light|dark</c> wins; otherwise interactive consoles use
        /// the detected background (console colour table, COLORFGBG) and everything else the dark theme.
        /// </summary>
        /// <param name="profile">The terminal profile.</param>
        /// <returns>The theme.</returns>
        public static ITheme ResolveTheme(TerminalProfile profile) {
            if (profile == null) {
                throw new ArgumentNullException(nameof(profile));
            }
            switch (profile.ThemePreference) {
                case ThemePreference.Light:
                    return Styling.Theme.Light;
                case ThemePreference.Dark:
                    return Styling.Theme.Dark;
            }
            if (profile.Mode == OutputMode.Interactive && Output.TerminalBackground.TryDetect(out Output.TerminalBackgroundInfo info) && info.IsLight) {
                return Styling.Theme.Light;
            }
            return Styling.Theme.Dark;
        }
        /// <summary>The active theme.</summary>
        public static ITheme Theme {
            get {
                lock (Gate) {
                    return _theme ?? (_theme = ResolveTheme(Profile));
                }
            }
            set => _theme = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>Discards the cached profile so the next use detects the terminal again (after a resize or font change).</summary>
        public static void Refresh() {
            lock (Gate) {
                if (_defaultProvider) {
                    _provider = new CapabilityDetector();
                }
                _profile = null;
            }
        }

        /// <summary>Writes a widget once, as wide as the terminal.</summary>
        /// <param name="widget">The widget.</param>
        public static void Write(IWidget widget) => Write(widget, null);

        /// <summary>Writes a widget once with an explicit profile.</summary>
        /// <param name="widget">The widget.</param>
        /// <param name="profile">The profile, or <c>null</c> for the detected one.</param>
        public static void Write(IWidget widget, TerminalProfile? profile) {
            if (widget == null) {
                throw new ArgumentNullException(nameof(widget));
            }
            TerminalProfile p = profile ?? Profile;
            RenderContext context = CreateContext(p);
            CellBuffer block = FrameComposer.RenderBlock(widget, p.Width, context);
            lock (Gate) {
                using (ITerminalOutput output = CreateOutput(p)) {
                    output.WriteBlock(block);
                }
            }
        }

        /// <summary>Writes one line of Spectre-compatible markup, for example <c>[pass]Done[/] in [muted]3s[/]</c>.</summary>
        /// <param name="markup">The markup.</param>
        public static void WriteMarkup(string markup) {
            TerminalProfile p = Profile;
            StyledText text = Markup.Parse(markup, Theme, p.ColorDepth);
            lock (Gate) {
                using (ITerminalOutput output = CreateOutput(p)) {
                    foreach (StyledLine line in text.Lines) {
                        output.WriteLine(line);
                    }
                }
            }
        }

        /// <summary>Creates a render context for a profile and theme.</summary>
        /// <param name="profile">The profile, or <c>null</c> for the detected one.</param>
        /// <param name="theme">The theme, or <c>null</c> for <see cref="Theme"/>.</param>
        /// <returns>The context.</returns>
        public static RenderContext CreateContext(TerminalProfile? profile = null, ITheme? theme = null) {
            return new RenderContext(profile ?? Profile, theme ?? Theme);
        }

        /// <summary>Creates the output for a profile: VT when live rendering is possible, line-based otherwise.</summary>
        /// <param name="profile">The profile, or <c>null</c> for the detected one.</param>
        /// <param name="writer">The sink, or <c>null</c> for the console (the output then owns it).</param>
        /// <returns>The output; dispose it when done.</returns>
        public static ITerminalOutput CreateOutput(TerminalProfile? profile = null, ITerminalWriter? writer = null) {
            TerminalProfile p = profile ?? Profile;
            bool owns = writer == null;
            ITerminalWriter w = writer ?? TerminalWriterFactory.Create(p);
            if (p.Mode == OutputMode.Interactive && p.SupportsVirtualTerminal) {
                return new VtOutput(w, p, owns);
            }
            return new LineOutput(w, p, owns);
        }

        /// <summary>Renders a widget to a cell buffer (for tests, snapshots and custom outputs).</summary>
        /// <param name="widget">The widget.</param>
        /// <param name="profile">The profile.</param>
        /// <param name="theme">The theme, or <c>null</c> for <see cref="Theme"/>.</param>
        /// <returns>The rendered block.</returns>
        public static CellBuffer Render(IWidget widget, TerminalProfile profile, ITheme? theme = null) {
            return FrameComposer.RenderBlock(widget, profile.Width, new RenderContext(profile, theme ?? Theme));
        }

        /// <summary>Renders a widget to plain text lines (trailing spaces trimmed).</summary>
        /// <param name="widget">The widget.</param>
        /// <param name="profile">The profile.</param>
        /// <param name="theme">The theme, or <c>null</c>.</param>
        /// <returns>The text.</returns>
        public static string RenderText(IWidget widget, TerminalProfile profile, ITheme? theme = null) {
            return Render(widget, profile, theme).ToPlainText();
        }

        /// <summary>Renders a widget to the exact text the output for <paramref name="profile"/> would write (including VT sequences).</summary>
        /// <param name="widget">The widget.</param>
        /// <param name="profile">The profile.</param>
        /// <param name="theme">The theme, or <c>null</c>.</param>
        /// <returns>The encoded text.</returns>
        public static string RenderEncoded(IWidget widget, TerminalProfile profile, ITheme? theme = null) {
            StringTerminalWriter writer = new StringTerminalWriter();
            using (ITerminalOutput output = CreateOutput(profile, writer)) {
                output.WriteBlock(Render(widget, profile, theme));
            }
            return writer.ToString();
        }
    }
}
