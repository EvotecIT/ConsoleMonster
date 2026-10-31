using System;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Reflection;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// Where cmdlet output goes. On a real local interactive console it is drawn directly (colour, live dashboards).
    /// Everywhere else (background and thread jobs, remoting, SSH, the ISE, redirected output, an active transcript) it is
    /// written through the host on the pipeline thread, with cell colours mapped to the 16 console colours:
    /// <list type="bullet">
    /// <item>A real console with a transcript running, and the ISE: coloured runs through Host.UI.Write(foreground,
    /// background, text) and WriteLine; the transcript receives the same line as plain text.</item>
    /// <item>Jobs and remoting (ServerRemoteHost): information records tagged PSHOST with foreground and background, as
    /// Write-Host -ForegroundColor -NoNewline writes them, so Receive-Job and the remote client show the colours.</item>
    /// <item>Redirected output, thread jobs, dashboard runspaces and other hosts: one plain information record per line, so
    /// redirected files, transcripts and <c>6&gt;&amp;1</c> get whole lines.</item>
    /// </list>
    /// Like Write-Host, assigning the output to a variable captures nothing. NO_COLOR, CONSOLEMONSTER_COLOR=none and plain
    /// mode use the plain route everywhere.
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
            HostRoute route = RouteFor(cmdlet, p);
            TerminalProfile host = HostProfile(cmdlet, p, explicitWidth, route);
            RenderContext context = Terminal.CreateContext(host);
            ConsoleColorMap colors = new ConsoleColorMap(context, route != HostRoute.Plain);
            List<Segment> segments = new List<Segment>();
            foreach (CellBuffer block in Terminal.RenderChunks(widget, host.Width, context)) {
                for (int y = 0; y < block.Height; y++) {
                    CollectRow(block, y, colors, segments);
                    EmitLine(cmdlet, route, segments);
                }
            }
        }

        // Runs of cells whose colours map to the same console colours become one segment. Blank cells without a background
        // join the current run, so spacing never splits a colour; trailing blanks are dropped.
        private static void CollectRow(CellBuffer block, int y, ConsoleColorMap colors, List<Segment> segments) {
            segments.Clear();
            int end = block.Width;
            while (end > 0) {
                Cell last = block[end - 1, y];
                if (!last.IsContinuation && (last.Glyph != " " || !last.Style.BackgroundColor.IsDefault)) {
                    break;
                }
                end--;
            }
            System.Text.StringBuilder run = new System.Text.StringBuilder();
            ConsoleColor? foreground = null;
            ConsoleColor? background = null;
            for (int x = 0; x < end; x++) {
                Cell cell = block[x, y];
                if (cell.IsContinuation) {
                    continue;
                }
                ConsoleColor? cellBackground = colors.Background(cell.Style.BackgroundColor);
                bool blank = cell.Glyph == " ";
                ConsoleColor? cellForeground = blank && cellBackground == background ? foreground : colors.Foreground(cell.Style.Color);
                if (run.Length > 0 && (cellForeground != foreground || cellBackground != background)) {
                    segments.Add(new Segment(run.ToString(), foreground, background));
                    run.Clear();
                }
                foreground = cellForeground;
                background = cellBackground;
                run.Append(cell.Glyph);
            }
            if (run.Length > 0) {
                segments.Add(new Segment(run.ToString(), foreground, background));
            }
        }

        /// <summary>Writes one line of markup for a cmdlet.</summary>
        /// <param name="cmdlet">The calling cmdlet.</param>
        /// <param name="markup">The markup.</param>
        /// <param name="newLine">Whether to end the last line (false: -NoNewline).</param>
        internal static void WriteMarkup(PSCmdlet cmdlet, string markup, bool newLine = true) {
            TerminalProfile p = Terminal.GetCurrentProfile();
            if (!UseHost(cmdlet, p)) {
                Terminal.WriteMarkup(markup, newLine, p, null);
                return;
            }
            HostRoute route = RouteFor(cmdlet, p);
            TerminalProfile host = HostProfile(cmdlet, p, false, route);
            RenderContext context = Terminal.CreateContext(host);
            ConsoleColorMap colors = new ConsoleColorMap(context, route != HostRoute.Plain);
            List<Segment> segments = new List<Segment>();
            IReadOnlyList<StyledLine> lines = context.Markup(markup).Lines;
            for (int i = 0; i < lines.Count; i++) {
                segments.Clear();
                foreach (TextSpan span in lines[i]) {
                    string text = Clean(span.Text);
                    if (text.Length > 0) {
                        segments.Add(new Segment(text, colors.Foreground(span.Style.Color), colors.Background(span.Style.BackgroundColor)));
                    }
                }
                EmitLine(cmdlet, route, segments, i < lines.Count - 1 || newLine);
            }
        }

        /// <summary>
        /// Writes lines (not a laid-out block) for a cmdlet, built for the route's profile: <paramref name="build"/> receives
        /// the render context and the line width. With <paramref name="newLine"/> false the last line stays open.
        /// </summary>
        /// <param name="cmdlet">The calling cmdlet.</param>
        /// <param name="build">Builds the styled lines for a context and width.</param>
        /// <param name="newLine">Whether to end the last line.</param>
        /// <param name="width">An explicit width, or 0 for the terminal or host width.</param>
        internal static void WriteLines(PSCmdlet cmdlet, Func<RenderContext, int, IReadOnlyList<StyledLine>> build, bool newLine, int width = 0) {
            TerminalProfile p = Terminal.GetCurrentProfile();
            if (width > 0) {
                p = p.WithSize(width, p.Height);
            }
            if (!UseHost(cmdlet, p)) {
                Terminal.WriteLines(build(Terminal.CreateContext(p), p.Width), newLine, p, null);
                return;
            }
            HostRoute route = RouteFor(cmdlet, p);
            TerminalProfile host = HostProfile(cmdlet, p, width > 0, route);
            RenderContext context = Terminal.CreateContext(host);
            ConsoleColorMap colors = new ConsoleColorMap(context, route != HostRoute.Plain);
            IReadOnlyList<StyledLine> lines = build(context, host.Width);
            List<Segment> segments = new List<Segment>();
            for (int i = 0; i < lines.Count; i++) {
                CollectSpans(lines[i], colors, segments);
                EmitLine(cmdlet, route, segments, i < lines.Count - 1 || newLine);
            }
        }

        // Spans whose colours map to the same console colours become one segment; spaces join the current run.
        private static void CollectSpans(StyledLine line, ConsoleColorMap colors, List<Segment> segments) {
            segments.Clear();
            System.Text.StringBuilder run = new System.Text.StringBuilder();
            ConsoleColor? foreground = null;
            ConsoleColor? background = null;
            foreach (TextSpan span in line) {
                string text = Clean(span.Text);
                if (text.Length == 0) {
                    continue;
                }
                ConsoleColor? spanBackground = colors.Background(span.Style.BackgroundColor);
                bool blank = text.Trim().Length == 0;
                ConsoleColor? spanForeground = blank && spanBackground == background ? foreground : colors.Foreground(span.Style.Color);
                if (run.Length > 0 && (spanForeground != foreground || spanBackground != background)) {
                    segments.Add(new Segment(run.ToString(), foreground, background));
                    run.Clear();
                }
                foreground = spanForeground;
                background = spanBackground;
                run.Append(text);
            }
            if (run.Length > 0) {
                segments.Add(new Segment(run.ToString(), foreground, background));
            }
        }

        // Markup spans are not cell-rendered, so control characters are dropped here, as the cell path does.
        private static string Clean(string text) {
            System.Text.StringBuilder sb = new System.Text.StringBuilder(text.Length);
            foreach (char c in text) {
                if (c == '\t') {
                    sb.Append("    ");
                } else if (c >= 0x20 && c != 0x7F && (c < 0x80 || c > 0x9F)) {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        /// <summary>Whether output must go through the PowerShell host instead of the console handle.</summary>
        /// <param name="cmdlet">The calling cmdlet.</param>
        /// <param name="profile">The detected profile.</param>
        /// <returns>True for anything but a real local interactive console.</returns>
        internal static bool UseHost(PSCmdlet cmdlet, TerminalProfile profile) {
            // Use-ConsoleTarget (or a host) sent the output to another target: it goes there, not to the host.
            if (!Terminal.IsConsole(Terminal.Target)) {
                return false;
            }
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

        /// <summary>
        /// The profile for host output: line output, rendered with colour unless NO_COLOR, CONSOLEMONSTER_COLOR=none or plain
        /// mode turn it off (the colours are mapped to console colours, never encoded), as wide as <see cref="HostWidth"/>
        /// resolves when no width was given.
        /// </summary>
        /// <param name="cmdlet">The calling cmdlet.</param>
        /// <param name="profile">The detected profile.</param>
        /// <param name="explicitWidth">Whether to keep the profile width.</param>
        /// <returns>The host profile.</returns>
        internal static TerminalProfile HostProfile(PSCmdlet cmdlet, TerminalProfile profile, bool explicitWidth) {
            return HostProfile(cmdlet, profile, explicitWidth, HostRoute.Plain);
        }

        private static TerminalProfile HostProfile(PSCmdlet cmdlet, TerminalProfile profile, bool explicitWidth, HostRoute route) {
            TerminalProfile p = profile.WithColorDepth(route == HostRoute.Plain ? ColorDepth.None : ColorDepth.TrueColor);
            if (p.Mode == OutputMode.Interactive) {
                p = p.WithMode(OutputMode.Stream);
            }
            return explicitWidth ? p : p.WithSize(HostWidth.Resolve(cmdlet.Host, p), p.Height);
        }

        /// <summary>Writes one uncoloured line as Write-Host does (an information record tagged PSHOST).</summary>
        /// <param name="cmdlet">The cmdlet on whose pipeline thread this runs.</param>
        /// <param name="line">The line.</param>
        /// <param name="endLine">Whether the line ends (false leaves it open, like -NoNewline).</param>
        internal static void WriteHostLine(PSCmdlet cmdlet, string line, bool endLine = true) {
            cmdlet.WriteInformation(new HostInformationMessage { Message = line, NoNewLine = !endLine }, HostTags);
            Transcribe(cmdlet, line);
        }

        private enum HostRoute {
            Plain,
            ColourRecords,
            ColourInterface
        }

        // See the class remarks. Records split at every -NoNewline segment in redirected output and in PowerShell 7
        // transcripts, and dashboard runspaces log each record as a line, so coloured records are only used where the
        // receiving side joins them (Receive-Job, remoting clients).
        private static HostRoute RouteFor(PSCmdlet cmdlet, TerminalProfile profile) {
            if (ConsoleColorMap.ColorDisabled(profile)) {
                return HostRoute.Plain;
            }
            string name = cmdlet.Host?.Name ?? string.Empty;
            if (string.Equals(name, "ConsoleHost", StringComparison.Ordinal)) {
                bool windows = Environment.OSVersion.Platform == PlatformID.Win32NT;
                return !Console.IsOutputRedirected && (!windows || profile.IsConsoleHandle) ? HostRoute.ColourInterface : HostRoute.Plain;
            }
            if (name.IndexOf("ISE", StringComparison.OrdinalIgnoreCase) >= 0) {
                return HostRoute.ColourInterface;
            }
            if (string.Equals(name, "ServerRemoteHost", StringComparison.Ordinal)) {
                return HostRoute.ColourRecords;
            }
            return HostRoute.Plain;
        }

        private static void EmitLine(PSCmdlet cmdlet, HostRoute route, List<Segment> segments, bool endLine = true) {
            switch (route) {
                case HostRoute.ColourInterface:
                    WriteInterface(cmdlet, segments, endLine);
                    break;
                case HostRoute.ColourRecords:
                    WriteSegments(cmdlet, segments, endLine);
                    break;
                default:
                    System.Text.StringBuilder line = new System.Text.StringBuilder();
                    foreach (Segment segment in segments) {
                        line.Append(segment.Text);
                    }
                    WriteHostLine(cmdlet, line.ToString(), endLine);
                    break;
            }
        }

        // Coloured runs straight to the host UI (one Write per run, then WriteLine); the UI does not transcribe these
        // writes, so the plain line goes to the transcript separately.
        private static void WriteInterface(PSCmdlet cmdlet, List<Segment> segments, bool endLine = true) {
            PSHostUserInterface? ui = cmdlet.Host?.UI;
            if (ui == null) {
                WriteSegments(cmdlet, segments, endLine);
                return;
            }
            ConsoleColor defaultForeground = ConsoleColor.Gray;
            ConsoleColor defaultBackground = ConsoleColor.Black;
            try {
                defaultForeground = ui.RawUI.ForegroundColor;
                defaultBackground = ui.RawUI.BackgroundColor;
            } catch (Exception) {
                // A host without raw UI colours: uncoloured runs use the fallbacks above.
            }
            System.Text.StringBuilder line = new System.Text.StringBuilder();
            foreach (Segment segment in segments) {
                if (segment.Foreground == null && segment.Background == null) {
                    ui.Write(segment.Text);
                } else {
                    ui.Write(segment.Foreground ?? defaultForeground, segment.Background ?? defaultBackground, segment.Text);
                }
                line.Append(segment.Text);
            }
            if (endLine) {
                ui.WriteLine();
            }
            TranscribeLine(ui, line.ToString());
        }

        // One line as coloured segments, as Write-Host -ForegroundColor -NoNewline writes them; the last segment ends the line.
        private static void WriteSegments(PSCmdlet cmdlet, List<Segment> segments, bool endLine = true) {
            if (segments.Count == 0) {
                WriteHostLine(cmdlet, string.Empty, endLine);
                return;
            }
            System.Text.StringBuilder line = new System.Text.StringBuilder();
            for (int i = 0; i < segments.Count; i++) {
                Segment segment = segments[i];
                HostInformationMessage message = new HostInformationMessage {
                    Message = segment.Text,
                    NoNewLine = i < segments.Count - 1 || !endLine,
                    ForegroundColor = segment.Foreground,
                    BackgroundColor = segment.Background
                };
                cmdlet.WriteInformation(message, HostTags);
                line.Append(segment.Text);
            }
            Transcribe(cmdlet, line.ToString());
        }

        // PSHostUserInterface.TranscribeResult(string) is internal on 5.1 and 7; it does nothing when no transcript runs.
        private static readonly MethodInfo? TranscribeResultMethod = typeof(PSHostUserInterface).GetMethod(
            "TranscribeResult", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, new[] { typeof(string) }, null);

        private static void TranscribeLine(PSHostUserInterface ui, string line) {
            try {
                TranscribeResultMethod?.Invoke(ui, new object[] { line });
            } catch (Exception) {
                // Best effort: a host without transcription support.
            }
        }

        private static void Transcribe(PSCmdlet cmdlet, string line) {
#if NETFRAMEWORK
            // Windows PowerShell 5.1 does not transcribe information records by itself; its Write-Host calls
            // TranscribeResult as well (PowerShell 7 transcribes PSHOST records centrally, so this would duplicate there).
            PSHostUserInterface? ui = cmdlet.Host?.UI;
            if (ui != null) {
                TranscribeLine(ui, line);
            }
#endif
        }

        private readonly struct Segment {
            public Segment(string text, ConsoleColor? foreground, ConsoleColor? background) {
                Text = text;
                Foreground = foreground;
                Background = background;
            }

            public string Text { get; }

            public ConsoleColor? Foreground { get; }

            public ConsoleColor? Background { get; }
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
