using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using ConsoleMonster.Canvas;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    /// <summary>
    /// An exception or PowerShell error, readable: the fail symbol and the type, the message, the PowerShell category,
    /// error id and position message, the script stack, the .NET frames as "Type.Method()" with shortened paths and
    /// file:line as links, framework frames collapsed into "+n framework frames", and the inner errors as "Caused by".
    /// </summary>
    public sealed class ExceptionView : IWidget {
        private readonly ExceptionInfo _info;

        /// <summary>Creates the view of an error description.</summary>
        /// <param name="info">The description.</param>
        public ExceptionView(ExceptionInfo info) {
            _info = info ?? throw new ArgumentNullException(nameof(info));
        }

        /// <summary>Creates the view of a .NET exception.</summary>
        /// <param name="exception">The exception.</param>
        public ExceptionView(Exception exception) : this(ExceptionInfo.FromException(exception)) {
        }

        /// <summary>Shows framework frames instead of collapsing them.</summary>
        public bool ShowFrameworkFrames { get; set; }

        /// <summary>The most app frames shown per error (default 8); the rest are counted.</summary>
        public int MaxFrames { get; set; } = 8;

        /// <summary>Shows the stack frames (default true); off shows the type, message and position only.</summary>
        public bool ShowStack { get; set; } = true;

        /// <summary>How many inner errors are shown (default 4).</summary>
        public int MaxInner { get; set; } = 4;

        /// <summary>Shows the source lines around the first app frame whose file can be read (default true), highlighted.</summary>
        public bool ShowSource { get; set; } = true;

        /// <summary>The folder paths are shown relative to (default the current directory).</summary>
        public string? BasePath { get; set; }

        /// <inheritdoc />
        public Size Measure(Constraints constraints, RenderContext context) {
            int rows = Lines(constraints.MaxWidth, context).Count;
            return new Size(constraints.MaxWidth, constraints.IsHeightUnbounded ? rows : Math.Min(rows, constraints.MaxHeight));
        }

        /// <inheritdoc />
        public void Render(Region region, RenderContext context) {
            List<StyledLine> lines = Lines(region.Width, context);
            for (int y = 0; y < lines.Count && y < region.Height; y++) {
                region.Write(0, y, TextWrapper.Truncate(lines[y], region.Width, context.Ellipsis));
            }
        }

        /// <summary>The lines of the view at a width.</summary>
        /// <param name="width">The width.</param>
        /// <param name="context">The render context.</param>
        /// <returns>The lines.</returns>
        public List<StyledLine> Lines(int width, RenderContext context) {
            List<StyledLine> lines = new List<StyledLine>();
            ExceptionInfo? info = _info;
            int indent = 0;
            for (int level = 0; info != null && level <= MaxInner; level++, info = info.Inner) {
                AddError(lines, info, indent, level > 0, width, context);
                indent += 2;
            }
            return lines;
        }

        private void AddError(List<StyledLine> lines, ExceptionInfo info, int indent, bool inner, int width, RenderContext context) {
            string pad = new string(' ', indent);
            Style fail = context.Style(StatusKind.Fail);
            Style muted = context.Style(ThemeToken.Muted);
            Style text = context.Style(ThemeToken.Text);
            List<TextSpan> head = new List<TextSpan> { new TextSpan(pad) };
            if (inner) {
                head.Add(new TextSpan(context.Labels.CausedBy + " ", muted));
            } else {
                head.Add(new TextSpan(context.Glyphs.StatusSymbol(StatusKind.Fail) + " ", fail));
            }
            head.Add(new TextSpan(info.Type, fail.With(TextDecoration.Bold)));
            lines.Add(new StyledLine(head));
            string body = pad + "  ";
            foreach (string paragraph in info.Message.Replace("\r\n", "\n").Trim('\n').Split('\n')) {
                foreach (StyledLine wrapped in TextWrapper.Wrap(StyledLine.From(paragraph.TrimEnd(), text), Math.Max(10, width - body.Length))) {
                    lines.Add(Prefixed(body, wrapped));
                }
            }
            string facts = Join(info.Category, info.ErrorId, context);
            if (facts.Length > 0) {
                foreach (StyledLine wrapped in TextWrapper.Wrap(StyledLine.From(facts, muted), Math.Max(10, width - body.Length))) {
                    lines.Add(Prefixed(body, wrapped));
                }
            }
            if (!string.IsNullOrEmpty(info.PositionMessage)) {
                foreach (string raw in info.PositionMessage!.Replace("\r\n", "\n").Trim('\n').Split('\n')) {
                    string line = raw.TrimEnd();
                    bool marker = line.TrimStart('+', ' ').Length > 0 && line.TrimStart('+', ' ').Trim('~').Length == 0;
                    lines.Add(StyledLine.From(body + line, marker ? fail : muted));
                }
            }
            if (ShowSource && !inner) {
                AddSource(lines, info, body, width, context);
            }
            if (!ShowStack) {
                return;
            }
            AddFrames(lines, info.ScriptFrames, body, context);
            AddFrames(lines, info.Frames, body, context);
        }

        private List<StyledLine>? _sourceCache;
        private int _sourceWidth = -1;

        // The lines around the first app frame with a readable file: line numbers, the failing line marked, highlighted.
        private void AddSource(List<StyledLine> lines, ExceptionInfo info, string body, int width, RenderContext context) {
            if (_sourceCache == null || _sourceWidth != width) {
                _sourceCache = new List<StyledLine>();
                _sourceWidth = width;
                ExceptionFrame? frame = null;
                foreach (ExceptionFrame candidate in info.ScriptFrames) {
                    if (candidate.File != null && candidate.Line > 0 && File.Exists(candidate.File)) {
                        frame = candidate;
                        break;
                    }
                }
                if (frame == null) {
                    foreach (ExceptionFrame candidate in info.Frames) {
                        if (!candidate.IsFramework && candidate.File != null && candidate.Line > 0 && File.Exists(candidate.File)) {
                            frame = candidate;
                            break;
                        }
                    }
                }
                if (frame != null) {
                    try {
                        string[] all = File.ReadAllLines(frame.File!);
                        int first = Math.Max(1, frame.Line - 1);
                        int last = Math.Min(all.Length, frame.Line + 1);
                        if (first <= last) {
                            string excerpt = string.Join("\n", all, first - 1, last - first + 1);
                            CodeView view = new CodeView(excerpt, Highlighting.Highlighters.ForPath(frame.File)) { LineNumbers = true, FirstLine = first }.HighlightLines(frame.Line);
                            _sourceCache.AddRange(view.Rows(Math.Max(20, width - body.Length), context));
                        }
                    } catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is System.Security.SecurityException || ex is NotSupportedException || ex is ArgumentException) {
                    }
                }
            }
            foreach (StyledLine row in _sourceCache) {
                lines.Add(Prefixed(body, row));
            }
        }
        private void AddFrames(List<StyledLine> lines, List<ExceptionFrame> frames, string body, RenderContext context) {
            Style muted = context.Style(ThemeToken.Muted);
            Style text = context.Style(ThemeToken.Text);
            int shown = 0;
            int hiddenFramework = 0;
            int hiddenApp = 0;
            foreach (ExceptionFrame frame in frames) {
                if (frame.IsFramework && !ShowFrameworkFrames) {
                    hiddenFramework++;
                    continue;
                }
                if (shown >= Math.Max(1, MaxFrames)) {
                    hiddenApp++;
                    continue;
                }
                FlushFramework(lines, body, ref hiddenFramework, muted);
                List<TextSpan> spans = new List<TextSpan> { new TextSpan(body + "at ", muted), new TextSpan(ShortMethod(frame.Method), text) };
                if (frame.File != null) {
                    string location = ShortPath(frame.File) + (frame.Line > 0 ? ":" + frame.Line.ToString(CultureInfo.InvariantCulture) : string.Empty);
                    string? link = Link(frame.File);
                    spans.Add(new TextSpan("  "));
                    spans.Add(new TextSpan(location, link == null ? muted : muted.With(TextDecoration.Underline).WithLink(link)));
                }
                lines.Add(new StyledLine(spans));
                shown++;
            }
            FlushFramework(lines, body, ref hiddenFramework, muted);
            if (hiddenApp > 0) {
                lines.Add(StyledLine.From(body + Localization.WidgetLabels.Format(hiddenApp == 1 ? Localization.WidgetLabels.Current.OneMoreFrame : Localization.WidgetLabels.Current.ManyMoreFrames, hiddenApp.ToString(CultureInfo.InvariantCulture)), muted));
            }
        }

        private static void FlushFramework(List<StyledLine> lines, string body, ref int hidden, Style muted) {
            if (hidden == 0) {
                return;
            }
            lines.Add(StyledLine.From(body + Localization.WidgetLabels.Format(hidden == 1 ? Localization.WidgetLabels.Current.OneFrameworkFrame : Localization.WidgetLabels.Current.ManyFrameworkFrames, hidden.ToString(CultureInfo.InvariantCulture)), muted));
            hidden = 0;
        }

        private static StyledLine Prefixed(string prefix, StyledLine line) {
            List<TextSpan> spans = new List<TextSpan> { new TextSpan(prefix) };
            spans.AddRange(line);
            return new StyledLine(spans);
        }

        private static string Join(string? category, string? errorId, RenderContext context) {
            string separator = " " + Interactive.InteractiveGlyphs.For(context).Separator + " ";
            if (!string.IsNullOrEmpty(category) && !string.IsNullOrEmpty(errorId)) {
                return category + separator + errorId;
            }
            return category ?? errorId ?? string.Empty;
        }

        /// <summary>"Namespace.Type.Method(args)" as "Type.Method(args)"; script frames are left as they are.</summary>
        /// <param name="method">The method.</param>
        /// <returns>The short form.</returns>
        public static string ShortMethod(string method) {
            int open = method.IndexOf('(');
            string name = open < 0 ? method : method.Substring(0, open);
            string args = open < 0 ? string.Empty : method.Substring(open);
            if (name.IndexOf(' ') >= 0 || name.IndexOf('<') == 0) {
                return method;
            }
            // Constructors are named Type..ctor: keep the type with them.
            string[] parts = name.Replace("..ctor", ".#ctor").Replace("..cctor", ".#cctor").Split('.');
            return parts.Length <= 2 ? method : (parts[parts.Length - 2] + "." + parts[parts.Length - 1]).Replace("#", ".") + args;
        }

        /// <summary>A path relative to <see cref="BasePath"/> when under it, otherwise the folder and file name.</summary>
        /// <param name="path">The path.</param>
        /// <returns>The short path.</returns>
        public string ShortPath(string path) {
            string basePath = BasePath ?? Environment.CurrentDirectory;
            if (Text.DisplayPath.IsForeign(path)) {
                // A path from another system (a Windows trace shown on Unix, or the reverse): shortened as text, since the
                // host's path APIs would misread it.
                return Text.DisplayPath.Short(path, Text.DisplayPath.IsForeign(basePath) ? basePath : null);
            }
            try {
                string full = Path.GetFullPath(path);
                string root = Path.GetFullPath(basePath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
                if (full.StartsWith(root, StringComparison.OrdinalIgnoreCase)) {
                    return full.Substring(root.Length);
                }
                string? folder = Path.GetFileName(Path.GetDirectoryName(full));
                return string.IsNullOrEmpty(folder) ? Path.GetFileName(full) : folder + Path.DirectorySeparatorChar + Path.GetFileName(full);
            } catch (Exception ex) when (ex is ArgumentException || ex is NotSupportedException || ex is PathTooLongException || ex is System.Security.SecurityException) {
                return path;
            }
        }

        private static string? Link(string path) {
            if (Text.DisplayPath.IsForeign(path)) {
                return null;
            }
            try {
                return Path.IsPathRooted(path) ? new Uri(Path.GetFullPath(path)).AbsoluteUri : null;
            } catch (Exception ex) when (ex is ArgumentException || ex is UriFormatException || ex is NotSupportedException || ex is PathTooLongException) {
                return null;
            }
        }
    }
}
