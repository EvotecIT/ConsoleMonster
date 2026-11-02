using System;
using System.Threading;
using System.Collections.Generic;
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
    public static partial class Terminal {
        private static readonly object Gate = new object();
        // Rendering and writer callbacks must not hold the profile/theme state lock: session callbacks can resolve
        // their profile while another print-once renderer requests the session's thread-safe Snapshot().
        private static readonly object WriteGate = new object();
        private static TerminalProfile? _profile;
        private static ITheme? _theme;
        private static ITheme? _detectedTheme;
        private static ICapabilityProvider _provider = new CapabilityDetector();
        private static bool _defaultProvider = true;
        private static bool _explicitProfile;

        // Do not mark Terminal beforefieldinit: the .NET Framework Release JIT may otherwise run capability
        // capture before LiveSession.Start saves the native mode, even when its first Terminal call follows the guard.
        static Terminal() { }

        /// <summary>Whether <see cref="Profile"/> was assigned rather than detected, so its size is kept as it is.</summary>
        internal static bool HasExplicitProfile {
            get {
                lock (Gate) {
                    return _explicitProfile;
                }
            }
        }

        // Test hook: returns { width, height } of the visible window, or null when unknown. Null uses the console.
        internal static Func<int[]?>? WindowSizeSource { get; set; }

        /// <summary>The capability provider used for detection.</summary>
        public static ICapabilityProvider CapabilityProvider {
            get => _provider;
            set {
                lock (Gate) {
                    _provider = value ?? throw new ArgumentNullException(nameof(value));
                    _defaultProvider = false;
                    _profile = null;
                    _explicitProfile = false;
                    _detectedTheme = null;
                }
            }
        }

        /// <summary>
        /// The active profile; detected on first use and cached (including the size at detection time). Assign to override
        /// detection for the process. Print-once writes use <see cref="GetCurrentProfile"/>, which refreshes the size.
        /// </summary>
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
                    _explicitProfile = true;
                    _detectedTheme = null;
                }
            }
        }

        /// <summary>
        /// Chooses the light or dark theme: <c>CONSOLEMONSTER_THEME=light|dark</c> wins; otherwise interactive consoles use
        /// the detected background (console colour table, startup OSC 11 query, COLORFGBG) and everything else the dark theme.
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
        /// <summary>The active theme: the target's (see <see cref="Target"/>), or the console's.</summary>
        public static ITheme Theme {
            get {
                ITerminalTarget target = Target;
                if (!(target is ConsoleTarget)) {
                    return target.Theme ?? ResolveTheme(ApplyProfilePolicy(target.Profile));
                }
                TerminalProfile profile;
                ITheme detected;
                lock (Gate) {
                    if (_theme != null) return _theme;
                    profile = Profile;
                    detected = _detectedTheme ?? (_detectedTheme = ResolveTheme(profile.WithThemePreference(ThemePreference.Auto)));
                }
                // Host callbacks and flow-local preferences must not be cached or invoked while holding Gate.
                switch (ApplyProfilePolicy(profile).ThemePreference) {
                    case ThemePreference.Light: return Styling.Theme.Light;
                    case ThemePreference.Dark: return Styling.Theme.Dark;
                    default: return detected;
                }
            }
            set {
                lock (Gate) { _theme = value ?? throw new ArgumentNullException(nameof(value)); }
            }
        }

        /// <summary>Discards the cached profile so the next use detects the terminal again (after a resize or font change).</summary>
        public static void Refresh() {
            lock (Gate) {
                if (_defaultProvider) {
                    _provider = new CapabilityDetector();
                }
                _profile = null;
                _explicitProfile = false;
                _detectedTheme = null;
            }
        }

        /// <summary>Re-evaluates automatic appearance after an owned input reader learns the terminal background.</summary>
        internal static void RefreshBackground() {
            lock (Gate) { _detectedTheme = null; }
        }

        /// <summary>
        /// The cached <see cref="Profile"/> with the current window size: after the user resizes the console, print-once
        /// output lays out for the new width while every other detected capability stays cached. An assigned profile,
        /// redirected output and non-interactive modes keep their size.
        /// </summary>
        /// <returns>The profile for a write happening now.</returns>
        /// <remarks>With a non-console <see cref="Target"/> in effect, this is that target's profile.</remarks>
        public static TerminalProfile GetCurrentProfile() {
            ITerminalTarget target = Target;
            return GetTargetProfile(target);
        }

        // The console's cached profile with the current window size (see GetCurrentProfile).
        internal static TerminalProfile GetConsoleProfile() {
            TerminalProfile p = Profile;
            bool explicitProfile;
            lock (Gate) {
                explicitProfile = _explicitProfile;
            }
            if (explicitProfile || p.Mode != OutputMode.Interactive || p.IsOutputRedirected) {
                return p;
            }
            int width;
            int height;
            Func<int[]?>? source = WindowSizeSource;
            if (source != null) {
                int[]? size = source();
                if (size == null || size.Length != 2) {
                    return p;
                }
                width = size[0];
                height = size[1];
            } else if (!TerminalSize.TryGet(out width, out height)) {
                return p;
            }
            return width > 0 && height > 0 && (width != p.Width || height != p.Height) ? p.WithSize(width, height) : p;
        }

        private static Live.LiveSession? _consoleOwner;

        /// <summary>The live session drawing on the console, if any (see <see cref="ClaimConsole"/>).</summary>
        internal static Live.LiveSession? ConsoleOwner => Volatile.Read(ref _consoleOwner);

        /// <summary>
        /// A live session that draws on the console claims it: while it runs, one-shot writes to the console (Write,
        /// WriteMarkup, WriteRich, WriteLines without a writer) go through the session, above an inline area or after the
        /// full screen, instead of over the frame. The most recent claim wins.
        /// </summary>
        /// <param name="session">The session.</param>
        internal static void ClaimConsole(Live.LiveSession session) => Volatile.Write(ref _consoleOwner, session);

        /// <summary>Releases the console claimed by a session (a later claim by another session is kept).</summary>
        /// <param name="session">The session.</param>
        internal static void ReleaseConsole(Live.LiveSession session) => Interlocked.CompareExchange(ref _consoleOwner, null, session);

        /// <summary>Writes a widget once, as wide as the terminal.</summary>
        /// <param name="widget">The widget.</param>
        public static void Write(IWidget widget) => Write(widget, null);

        /// <summary>Writes a widget once with an explicit profile.</summary>
        /// <param name="widget">The widget.</param>
        /// <param name="profile">The profile, or <c>null</c> for the detected one.</param>
        public static void Write(IWidget widget, TerminalProfile? profile) => Write(widget, profile, null);

        /// <summary>
        /// Writes a widget once to an explicit sink: a PowerShell host, a log, a string. With a writer, the output is laid out
        /// for the profile but goes to the writer instead of the console.
        /// </summary>
        /// <param name="widget">The widget.</param>
        /// <param name="profile">The profile, or <c>null</c> for <see cref="GetCurrentProfile"/>.</param>
        /// <param name="writer">The sink, or <c>null</c> for the console. The caller keeps ownership of a given writer.</param>
        public static void Write(IWidget widget, TerminalProfile? profile, ITerminalWriter? writer) {
            if (widget == null) {
                throw new ArgumentNullException(nameof(widget));
            }
            TerminalProfile p = profile ?? GetCurrentProfile();
            writer = writer ?? Target.Writer;
            RenderContext context = CreateContext(p);
            Live.LiveSession? owner = writer == null ? ConsoleOwner : null;
            if (owner != null) {
                Live.ConsoleWriteBlock prepared = new Live.ConsoleWriteBlock(RenderChunks(widget, p.Width, context));
                if (!owner.TryWriteAbove(prepared) && !owner.TryWriteFallback(prepared)) { WritePrepared(prepared, p, writer); }
                return;
            }
            lock (WriteGate) {
                using (ITerminalOutput output = CreateOutput(p, writer)) {
                    foreach (CellBuffer block in RenderChunks(widget, p.Width, context)) {
                        output.WriteBlock(block);
                    }
                }
                writer?.Flush();
            }
        }

        /// <summary>
        /// The rows above which a table is printed in blocks (default 2000): column widths are still computed from every
        /// row, but cells are rendered and written a block at a time, so memory stays flat for very long tables.
        /// </summary>
        public static int StreamingRowThreshold { get; set; } = 2000;

        /// <summary>
        /// Renders a widget for print-once output as one or more blocks written one after another: a long table comes as a
        /// sequence of blocks with stable column widths (see <see cref="StreamingRowThreshold"/>), anything else as one block.
        /// </summary>
        /// <param name="widget">The widget.</param>
        /// <param name="width">The width in cells.</param>
        /// <param name="context">The render context.</param>
        /// <param name="chunkLines">Lines per block for long tables (default 500).</param>
        /// <returns>The blocks, top to bottom.</returns>
        public static IEnumerable<CellBuffer> RenderChunks(IWidget widget, int width, RenderContext context, int chunkLines = 500) {
            if (widget == null) {
                throw new ArgumentNullException(nameof(widget));
            }
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }
            if (widget is Widgets.Table table && table.Rows.Count > StreamingRowThreshold) {
                return table.RenderChunks(width, context, Math.Max(1, chunkLines));
            }
            return new[] { FrameComposer.RenderBlock(widget, width, context) };
        }

        /// <summary>
        /// Writes Spectre-compatible markup without ending the line (Spectre's <c>Markup</c>), for example a prompt prefix;
        /// line breaks inside the markup still start new lines.
        /// </summary>
        /// <param name="markup">The markup.</param>
        public static void WriteMarkup(string markup) => WriteMarkup(markup, false, null, null);

        /// <summary>Writes one line of Spectre-compatible markup (Spectre's <c>MarkupLine</c>), for example <c>[pass]Done[/] in [muted]3s[/]</c>.</summary>
        /// <param name="markup">The markup.</param>
        public static void WriteMarkupLine(string markup) => WriteMarkup(markup, true, null, null);

        /// <summary>Writes one line of markup to an explicit sink.</summary>
        /// <param name="markup">The markup.</param>
        /// <param name="profile">The profile, or <c>null</c> for <see cref="GetCurrentProfile"/>.</param>
        /// <param name="writer">The sink, or <c>null</c> for the console. The caller keeps ownership of a given writer.</param>
        public static void WriteMarkupLine(string markup, TerminalProfile? profile, ITerminalWriter? writer) => WriteMarkup(markup, true, profile, writer);

        /// <summary>
        /// Writes markup to an explicit sink; with <paramref name="newLine"/> false the last line is left open. Redirected and
        /// CI output gets plain text either way.
        /// </summary>
        /// <param name="markup">The markup.</param>
        /// <param name="newLine">Whether to end the last line.</param>
        /// <param name="profile">The profile, or <c>null</c> for <see cref="GetCurrentProfile"/>.</param>
        /// <param name="writer">The sink, or <c>null</c> for the console. The caller keeps ownership of a given writer.</param>
        public static void WriteMarkup(string markup, bool newLine, TerminalProfile? profile = null, ITerminalWriter? writer = null) {
            TerminalProfile p = profile ?? GetCurrentProfile();
            IReadOnlyList<StyledLine> lines = CreateContext(p).Markup(markup).Lines;
            if (!p.IsOutputRedirected && p.Mode == OutputMode.Interactive) {
                // Long markup wraps at word boundaries with its styles, like a text block, instead of breaking mid-word
                // at the window edge. Redirected, CI and plain output keep whole lines for logs.
                List<StyledLine> wrapped = new List<StyledLine>(lines.Count);
                foreach (StyledLine line in lines) {
                    wrapped.AddRange(line.Width > p.Width ? TextWrapper.Wrap(line, Math.Max(1, p.Width)) : new[] { line });
                }
                lines = wrapped;
            }
            WriteLines(lines, newLine, p, writer);
        }

        /// <summary>
        /// Writes rich text as lines (not as a laid-out block): line breaks inside it start new lines, nothing is wrapped
        /// or padded, and with <paramref name="newLine"/> false the last line is left open, as Write-Host -NoNewline does.
        /// </summary>
        /// <param name="text">The rich text.</param>
        /// <param name="newLine">Whether to end the last line.</param>
        /// <param name="profile">The profile, or <c>null</c> for <see cref="GetCurrentProfile"/>.</param>
        /// <param name="writer">The sink, or <c>null</c> for the console. The caller keeps ownership of a given writer.</param>
        public static void WriteRich(RichText text, bool newLine = true, TerminalProfile? profile = null, ITerminalWriter? writer = null) {
            if (text == null) {
                throw new ArgumentNullException(nameof(text));
            }
            TerminalProfile p = profile ?? GetCurrentProfile();
            WriteLines(text.ToStyledText(CreateContext(p)).Lines, newLine, p, writer);
        }

        /// <summary>Writes styled lines; with <paramref name="newLine"/> false the last line is left open.</summary>
        /// <param name="lines">The lines.</param>
        /// <param name="newLine">Whether to end the last line.</param>
        /// <param name="profile">The profile, or <c>null</c> for <see cref="GetCurrentProfile"/>.</param>
        /// <param name="writer">The sink, or <c>null</c> for the console. The caller keeps ownership of a given writer.</param>
        public static void WriteLines(IReadOnlyList<StyledLine> lines, bool newLine = true, TerminalProfile? profile = null, ITerminalWriter? writer = null) {
            if (lines == null) {
                throw new ArgumentNullException(nameof(lines));
            }
            TerminalProfile p = profile ?? GetCurrentProfile();
            writer = writer ?? Target.Writer;
            Live.LiveSession? owner = writer == null ? ConsoleOwner : null;
            if (owner != null) {
                Live.ConsoleWriteBlock prepared = new Live.ConsoleWriteBlock(lines, newLine);
                if (!owner.TryWriteAbove(prepared) && !owner.TryWriteFallback(prepared)) { WritePrepared(prepared, p, writer); }
                return;
            }
            lock (WriteGate) {
                using (ITerminalOutput output = CreateOutput(p, writer)) {
                    for (int i = 0; i < lines.Count; i++) {
                        if (i < lines.Count - 1 || newLine) { output.WriteLine(lines[i]); }
                        else { output.WriteInline(lines[i]); }
                    }
                }
                writer?.Flush();
            }
        }

        private static void WritePrepared(Live.ConsoleWriteBlock prepared, TerminalProfile profile, ITerminalWriter? writer) {
            lock (WriteGate) {
                using (ITerminalOutput output = CreateOutput(profile, writer)) { prepared.Write(output); }
                writer?.Flush();
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

        /// <summary>
        /// Creates the output for an inline live area (<see cref="Rendering.InlineOutput"/>) on a VT console, or stream lines
        /// where in-place drawing is not possible (CI, redirected output, no VT).
        /// </summary>
        /// <param name="profile">The profile, or <c>null</c> for <see cref="Profile"/>.</param>
        /// <param name="writer">The sink, or <c>null</c> for the console (then owned by the output).</param>
        /// <returns>The output.</returns>
        public static ITerminalOutput CreateInlineOutput(TerminalProfile? profile = null, ITerminalWriter? writer = null) {
            TerminalProfile p = profile ?? Profile;
            bool owns = writer == null;
            ITerminalWriter w = writer ?? TerminalWriterFactory.Create(p);
            if (p.Mode == OutputMode.Interactive && p.SupportsVirtualTerminal) {
                return new InlineOutput(w, p, owns);
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
