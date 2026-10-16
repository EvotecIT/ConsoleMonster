using System;
using System.Collections.Generic;
using System.Globalization;
using ConsoleMonster.Canvas;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    /// <summary>
    /// Aligned key/value pairs: muted keys in a column sized to the longest key, values (markup) wrapping in the rest.
    /// Live operation <c>set</c> (Key, Text, optional State) adds or replaces a pair.
    /// </summary>
    public sealed class KeyValueList : LiveWidget {
        private readonly List<KeyValueItem> _items = new List<KeyValueItem>();
        private readonly Dictionary<string, int> _index = new Dictionary<string, int>(StringComparer.Ordinal);

        /// <summary>Creates a key/value list.</summary>
        /// <param name="name">The live name (default <c>keyvalue</c>).</param>
        public KeyValueList(string name = "keyvalue") : base(name) {
        }

        /// <summary>The pairs in order.</summary>
        public IReadOnlyList<KeyValueItem> Items => _items;

        /// <summary>Cells between the key column and the values (default 2).</summary>
        public int Gap { get; set; } = 2;

        /// <summary>The widest the key column may be, as a fraction of the width (default 0.4).</summary>
        public double MaxKeyFraction { get; set; } = 0.4;

        /// <summary>
        /// Layout of the leading number of each value (<c>234 / 332</c>, <c>8.4 rules/s</c>, <c>~12s</c>): values that start with
        /// a number share a number column, right-aligned by default, so the numbers line up under each other. Set the
        /// alignment to <see cref="HorizontalAlignment.Left"/> to print values as written.
        /// </summary>
        public ColumnFormat NumberColumn {
            get => _numberColumn.Format;
            set => _numberColumn.Format = value ?? throw new ArgumentNullException(nameof(value));
        }

        private readonly StatColumn _numberColumn = new StatColumn(new ColumnFormat());

        /// <summary>Adds or replaces a pair.</summary>
        /// <param name="key">The key (plain text).</param>
        /// <param name="value">The value (markup).</param>
        /// <param name="status">An optional status shown as a marker before the value.</param>
        /// <returns>This instance.</returns>
        public KeyValueList Add(string key, string value, StatusKind? status = null) {
            if (key == null) {
                throw new ArgumentNullException(nameof(key));
            }
            KeyValueItem item = new KeyValueItem(key, value ?? string.Empty, status);
            if (_index.TryGetValue(key, out int existing)) {
                _items[existing] = item;
            } else {
                _index[key] = _items.Count;
                _items.Add(item);
            }
            return this;
        }

        /// <summary>Adds or replaces a pair whose value is formatted by a presenter (sizes, durations, dates, statuses...).</summary>
        /// <param name="key">The key (plain text).</param>
        /// <param name="value">The raw value.</param>
        /// <param name="presenter">The presenter, or null for automatic.</param>
        /// <param name="rules">Tone rules, or null.</param>
        /// <returns>This instance.</returns>
        public KeyValueList AddValue(string key, object? value, Presenters.ICellPresenter? presenter = null, IReadOnlyList<Presenters.ToneRule>? rules = null) {
            Add(key, string.Empty);
            KeyValueItem item = _items[_index[key]];
            item.Cell = TableCell.FromValue(value);
            item.Presenter = presenter;
            item.Rules = rules;
            return this;
        }

        /// <summary>Culture, clock and time zone for values added with <see cref="AddValue"/>.</summary>
        public Presenters.PresentationOptions Presentation { get; set; } = new Presenters.PresentationOptions();

        /// <summary>Adds a pair with a number formatted with thousands separators.</summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The number.</param>
        /// <returns>This instance.</returns>
        public KeyValueList Add(string key, double value) => Add(key, NumberFormat.Grouped(value));

        /// <summary>Returns a handle that updates this list through a dispatcher (a live session).</summary>
        /// <param name="dispatcher">The dispatcher.</param>
        /// <returns>The handle.</returns>
        public KeyValueHandle Handle(ILiveDispatcher dispatcher) => new KeyValueHandle(dispatcher, Name);

        /// <inheritdoc />
        public override void Apply(LiveEvent liveEvent) {
            if (liveEvent.Operation == "set" && liveEvent.Key != null) {
                StatusKind? status = null;
                if (liveEvent.State != null && StatusKinds.TryParse(liveEvent.State, out StatusKind parsed)) {
                    status = parsed;
                }
                Add(liveEvent.Key, liveEvent.Text ?? liveEvent.Value.ToString(CultureInfo.InvariantCulture), status);
            }
        }

        /// <inheritdoc />
        public override Size Measure(Constraints constraints, RenderContext context) {
            int keyWidth = KeyWidth(constraints.MaxWidth);
            int valueWidth = Math.Max(1, constraints.MaxWidth - keyWidth - Gap);
            int height = 0;
            int width = 0;
            int numberWidth = NumberWidth(context);
            foreach (KeyValueItem item in _items) {
                IReadOnlyList<StyledLine> lines = ValueLines(item, valueWidth, numberWidth, context);
                height += Math.Max(1, lines.Count);
                foreach (StyledLine line in lines) {
                    width = Math.Max(width, keyWidth + Gap + line.Width);
                }
            }
            return new Size(Math.Min(width, constraints.MaxWidth), constraints.IsHeightUnbounded ? height : Math.Min(height, constraints.MaxHeight));
        }

        /// <inheritdoc />
        public override void Render(Region region, RenderContext context) {
            int keyWidth = KeyWidth(region.Width);
            int valueWidth = Math.Max(1, region.Width - keyWidth - Gap);
            Style keyStyle = context.Style(ThemeToken.Muted);
            int numberWidth = NumberWidth(context);
            int y = 0;
            foreach (KeyValueItem item in _items) {
                if (y >= region.Height) {
                    break;
                }
                region.Write(0, y, TextWrapper.Truncate(item.Key, keyWidth, context.Ellipsis), keyStyle);
                IReadOnlyList<StyledLine> lines = ValueLines(item, valueWidth, numberWidth, context);
                for (int i = 0; i < lines.Count && y < region.Height; i++) {
                    region.Write(keyWidth + Gap, y, lines[i]);
                    y++;
                }
                if (lines.Count == 0) {
                    y++;
                }
            }
        }

        private int KeyWidth(int width) {
            int longest = 0;
            foreach (KeyValueItem item in _items) {
                longest = Math.Max(longest, DisplayWidth.Measure(item.Key));
            }
            int cap = Math.Max(4, (int)(width * MaxKeyFraction));
            return Math.Min(longest, cap);
        }

        private int NumberWidth(RenderContext context) {
            if (NumberColumn.Alignment == HorizontalAlignment.Left) {
                return 0;
            }
            int widest = 0;
            foreach (KeyValueItem item in _items) {
                widest = Math.Max(widest, DisplayWidth.Measure(LeadingNumber(TextOf(item, context))));
            }
            return widest == 0 ? 0 : _numberColumn.Resolve(widest);
        }

        // The leading number of a value with an optional sign or "~" and an optional unit ("8.4", "~12s", "77%", "6 064 519"):
        // culture group separators (non-breaking or thin spaces, apostrophes) count as part of the number between digits.
        private static string LeadingNumber(string value) {
            if (string.IsNullOrEmpty(value) || value[0] == '[') {
                return string.Empty;
            }
            int end = value.IndexOf(' ');
            string token = end < 0 ? value : value.Substring(0, end);
            int i = 0;
            if (i < token.Length && (token[i] == '~' || token[i] == '+' || token[i] == '-' || token[i] == '\u2248')) {
                i++;
            }
            if (i >= token.Length || !char.IsDigit(token[i])) {
                return string.Empty;
            }
            while (i < token.Length && (char.IsDigit(token[i]) || IsNumberSeparator(token, i))) {
                i++;
            }
            for (; i < token.Length; i++) {
                if (!char.IsLetter(token[i]) && token[i] != '%' && token[i] != '/') {
                    return string.Empty;
                }
            }
            return token;
        }

        private CellContent Present(KeyValueItem item, RenderContext context) {
            return CellRendering.Content(item.Cell!, item.Presenter, item.Rules, Presentation.CreateContext(context));
        }

        private string TextOf(KeyValueItem item, RenderContext context) {
            return item.Cell != null ? Markup.Escape(Present(item, context).Lines[0].PlainText) : item.Value;
        }

        private static bool IsNumberSeparator(string token, int index) {
            char c = token[index];
            bool between = index > 0 && index + 1 < token.Length && char.IsDigit(token[index - 1]) && char.IsDigit(token[index + 1]);
            if (c == '.' || c == ',' || c == ':') {
                return true;
            }
            return between && (c == '\u00A0' || c == '\u202F' || c == '\u2009' || c == '\'');
        }

        private IReadOnlyList<StyledLine> ValueLines(KeyValueItem item, int width, int numberWidth, RenderContext context) {
            List<StyledLine> result = new List<StyledLine>();
            string prefix = item.Status.HasValue ? context.Glyphs.StatusMarker(item.Status.Value) + " " : string.Empty;
            Style markerStyle = item.Status.HasValue ? context.Style(item.Status.Value) : Style.Plain;
            string number = numberWidth > 0 ? LeadingNumber(TextOf(item, context)) : string.Empty;
            int pad = number.Length > 0 ? numberWidth - DisplayWidth.Measure(number) : 0;
            if (pad > 0 && NumberColumn.Alignment == HorizontalAlignment.Center) {
                pad /= 2;
            }
            StyledText text = item.Cell != null ? new StyledText(new[] { Present(item, context).Lines[0] }) : context.Markup(item.Value);
            bool first = true;
            foreach (StyledLine line in text.Lines) {
                StyledLine full = line;
                if (first && (prefix.Length > 0 || pad > 0)) {
                    List<TextSpan> spans = new List<TextSpan>();
                    if (prefix.Length > 0) {
                        spans.Add(new TextSpan(prefix, markerStyle));
                    }
                    if (pad > 0) {
                        spans.Add(new TextSpan(new string(' ', pad)));
                    }
                    spans.AddRange(line);
                    full = new StyledLine(spans);
                }
                first = false;
                result.AddRange(TextWrapper.Wrap(full, width));
            }
            return result;
        }
    }
}
