using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Logging {
    internal sealed class LogEntryRenderer {
        private readonly object _gate = new object();
        private int _tagWidth;

        internal static string LevelName(int level) {
            switch (level) {
                case 0: return "verbose";
                case 1: return "debug";
                case 3: return "warn";
                case 4:
                case 5: return "err";
                default: return "info";
            }
        }

        internal void Write(LogEntryOptions options, string category, int level, int eventId, LogMessage message, Exception? exception, string? scopes = null, DateTimeOffset? timestamp = null) {
            string levelName = LevelName(level);
            string? shortCategory = CategoryText(category, options);
            if (level == 5) {
                message.Parts.Insert(0, new LogMessage.Part("critical: "));
            }
            // An entry with an exception prints above a live view (with its details kept in the scrollback) instead of
            // going into the log region, which keeps only its newest lines.
            if (options.LogRegion != null && exception == null && TryPost(options, levelName, shortCategory, scopes, eventId, message)) {
                return;
            }
            TerminalProfile profile = options.Profile ?? Terminal.GetCurrentProfile();
            RenderContext context = Terminal.CreateContext(profile);
            List<StyledLine> lines = new List<StyledLine>();
            Style muted = context.Style(ThemeToken.Muted);
            List<TextSpan> spans = new List<TextSpan>();
            if (options.TimestampFormat != null) {
                DateTimeOffset now = timestamp ?? Presenters.PresentationOptions.ReferenceNow();
                if (options.UseUtcTimestamp) { now = now.ToUniversalTime(); }
                spans.Add(new TextSpan(now.ToString(options.TimestampFormat, CultureInfo.InvariantCulture) + " ", muted));
            }
            // Status markers only on an interactive terminal; logs that are redirected or collected keep the word alone.
            RenderContext? tagContext = profile.Mode == OutputMode.Interactive ? context : null;
            string tag = LogTags.Tag(levelName, tagContext);
            lock (_gate) {
                // Messages start in one column: the tag column only grows.
                _tagWidth = Math.Max(_tagWidth, Math.Max(DisplayWidth.Measure(tag), DisplayWidth.Measure(LogTags.Tag("warn", tagContext))));
            }
            spans.Add(new TextSpan(tag + new string(' ', Math.Max(0, _tagWidth - DisplayWidth.Measure(tag))) + " ", LogTags.Style(levelName, context)));
            StringBuilder origin = new StringBuilder();
            if (shortCategory != null) {
                origin.Append(shortCategory);
            }
            if (eventId != 0) {
                origin.Append('[').Append(eventId.ToString(CultureInfo.InvariantCulture)).Append(']');
            }
            if (!string.IsNullOrEmpty(scopes)) {
                origin.Append(origin.Length > 0 ? " " : string.Empty).Append('(').Append(scopes).Append(')');
            }
            if (origin.Length > 0) {
                spans.Add(new TextSpan(origin.ToString() + "  ", muted));
            }
            Style text = LogTags.IsQuiet(levelName) ? muted : context.Style(ThemeToken.Text);
            foreach (LogMessage.Part part in message.Parts) {
                Style style = part.Role.HasValue ? context.Style(part.Role.Value) : text;
                string[] fragments = part.Text.Replace("\r\n", "\n").Split('\n');
                spans.Add(new TextSpan(fragments[0], style));
                for (int i = 1; i < fragments.Length; i++) {
                    lines.Add(new StyledLine(spans));
                    spans = new List<TextSpan> { new TextSpan("    " + fragments[i], style) };
                }
            }
            lines.Add(new StyledLine(spans));
            if (exception != null) {
                AddException(options, lines, exception, context);
            }
            lock (_gate) {
                Terminal.WriteLines(lines, true, profile, options.Writer);
            }
        }

        // Into a running session's log region: the region adds its own time and level column.
        private static bool TryPost(LogEntryOptions options, string level, string? category, string? scopes, int eventId, LogMessage message) {
            LiveSession? session = LiveSession.Current;
            if (session == null || !session.TryGetTarget(options.LogRegion!, out ILiveTarget? target) || target == null) {
                return false;
            }
            StringBuilder markup = new StringBuilder();
            if (category != null || !string.IsNullOrEmpty(scopes) || (eventId != 0)) {
                markup.Append("[muted]");
                markup.Append(Markup.Escape(category ?? string.Empty));
                if (eventId != 0) {
                    markup.Append(Markup.Escape("[" + eventId.ToString(CultureInfo.InvariantCulture) + "]"));
                }
                if (!string.IsNullOrEmpty(scopes)) {
                    markup.Append(category != null ? " " : string.Empty).Append('(').Append(Markup.Escape(scopes!)).Append(')');
                }
                markup.Append("[/] ");
            }
            foreach (LogMessage.Part part in message.Parts) {
                if (part.Role.HasValue) { markup.Append('[').Append(part.Role.Value.ToString()).Append(']'); }
                markup.Append(Markup.Escape(part.Text.Replace("\r\n", " ").Replace('\n', ' ')));
                if (part.Role.HasValue) { markup.Append("[/]"); }
            }
            session.Post(new LiveEvent(options.LogRegion!, "write", text: markup.ToString(), status: level, isMarkup: true));
            return true;
        }

        // The exception through ConsoleMonster's error view: type and message, the frames with user code emphasised and
        // framework frames collapsed, wrapped at word and path boundaries, indented under the entry.
        private static void AddException(LogEntryOptions options, List<StyledLine> lines, Exception exception, RenderContext context) {
            ExceptionView view = new ExceptionView(exception) {
                ShowStack = options.MaxExceptionLines > 0,
                MaxFrames = Math.Max(1, options.MaxExceptionLines),
                ShowSource = false
            };
            const int indent = 4;
            // On a terminal the view fits the window; in redirected logs and files nothing is wrapped or shortened.
            int width = context.Profile.Mode == OutputMode.Interactive ? Math.Max(20, context.Profile.Width - indent - 1) : 100000;
            string pad = new string(' ', indent);
            foreach (StyledLine line in view.Lines(width, context)) {
                List<TextSpan> spans = new List<TextSpan> { new TextSpan(pad) };
                spans.AddRange(line);
                lines.Add(new StyledLine(spans));
            }
        }
        private static string? CategoryText(string category, LogEntryOptions options) {
            if (string.IsNullOrEmpty(category) || !options.IncludeCategory) {
                return null;
            }
            if (options.FullCategory) {
                return category;
            }
            // Generic arguments (Logger<Shop.Orders<Item>>) keep their dots inside the brackets.
            int angle = category.IndexOf('<');
            string head = angle >= 0 ? category.Substring(0, angle) : category;
            int dot = head.LastIndexOf('.');
            return dot >= 0 ? category.Substring(dot + 1) : category;
        }

    }
}
