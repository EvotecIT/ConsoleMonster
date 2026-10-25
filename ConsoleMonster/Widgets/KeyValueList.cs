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
        /// <param name="widgetId">The live name (default <c>keyvalue</c>).</param>
        public KeyValueList(string widgetId = "keyvalue") : base(widgetId) {
        }

        /// <summary>The pairs in order.</summary>
        public IReadOnlyList<KeyValueItem> Items => _items;

        /// <summary>Cells between the key column and the values (default 2).</summary>
        public int Gap { get; set; } = 2;

        /// <summary>Preferred number of pairs per row; narrow layouts reduce the count automatically.</summary>
        public int Columns { get; set; } = 1;

        /// <summary>Theme colour for keys.</summary>
        public ThemeToken KeyRole { get; set; } = ThemeToken.Muted;

        private IWidget? MultiColumn(int width) {
            int count = Math.Min(Math.Max(1, Columns), Math.Max(1, width / 32));
            if (count <= 1) return null;
            var children = new List<IWidget>();
            foreach (KeyValueItem item in _items) {
                children.Add(new KeyValueList { Gap = Gap, MaxKeyFraction = MaxKeyFraction, KeyRole = KeyRole, Presentation = Presentation, NumberColumn = NumberColumn }.Add(item));
            }
            return new Grid(count, children.ToArray());
        }

        /// <summary>Preferred key fraction when the natural label and value widths cannot both fit (default 0.4).</summary>
        public double MaxKeyFraction { get; set; } = 0.4;

        /// <summary>
        /// Layout of the leading number of each value (<c>234 / 332</c>, <c>8.4 rules/s</c>, <c>~12s</c>): when every value
        /// starts with a number, the numbers share a column, right-aligned by default, so they line up under each other. A
        /// list that mixes in text (a host name, an OS) keeps every value left-aligned as written, so "3 days" under a name
        /// is not pushed right. Set the alignment to <see cref="HorizontalAlignment.Left"/> to always print values as written.
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

        /// <summary>Adds or replaces a pair made beforehand, keeping its presenter (a key already in the list is replaced in place).</summary>
        /// <param name="item">The pair.</param>
        /// <returns>This instance.</returns>
        public KeyValueList Add(KeyValueItem item) {
            if (item == null) {
                throw new ArgumentNullException(nameof(item));
            }
            if (_index.TryGetValue(item.Key, out int existing)) {
                _items[existing] = item;
            } else {
                _index[item.Key] = _items.Count;
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
        public KeyValueList AddValue(string key, object? value, Presenters.ICellPresenter? presenter = null, IReadOnlyList<Presenters.RoleRule>? rules = null) {
            Add(key, string.Empty);
            KeyValueItem item = _items[_index[key]];
            item.Cell = TableCell.FromValue(value);
            item.Presenter = presenter ?? Presenters.CellPresenters.ForLabel(key, value);
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
        public KeyValueHandle Handle(ILiveDispatcher dispatcher) => new KeyValueHandle(dispatcher, WidgetId);

        /// <inheritdoc />
        public override void Apply(LiveEvent liveEvent) {
            if (liveEvent.Operation == "set" && liveEvent.Key != null) {
                StatusKind? status = null;
                if (liveEvent.Status != null && StatusKinds.TryParse(liveEvent.Status, out StatusKind parsed)) {
                    status = parsed;
                }
                Add(liveEvent.Key, liveEvent.Text ?? liveEvent.Value.ToString(CultureInfo.InvariantCulture), status);
            }
        }

        /// <inheritdoc />
        public override Size Measure(Constraints constraints, RenderContext context) {
            IWidget? multi = MultiColumn(constraints.MaxWidth);
            if (multi != null) return multi.Measure(constraints, context);
            int keyWidth = KeyWidth(constraints.MaxWidth, context);
            int valueWidth = Math.Max(1, constraints.MaxWidth - keyWidth - Gap);
            int height = 0;
            int width = 0;
            int numberWidth = NumberWidth(context, constraints.MaxWidth);
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
            IWidget? multi = MultiColumn(region.Width);
            if (multi != null) { multi.Render(region, context); return; }
            int keyWidth = KeyWidth(region.Width, context);
            int valueWidth = Math.Max(1, region.Width - keyWidth - Gap);
            Style keyStyle = context.Style(KeyRole);
            int numberWidth = NumberWidth(context, region.Width);
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

        private int KeyWidth(int width, RenderContext context) {
            int longest = 0;
            foreach (KeyValueItem item in _items) {
                longest = Math.Max(longest, DisplayWidth.Measure(item.Key));
            }
            int values = 0;
            int numbers = NumberWidth(context, width);
            foreach (KeyValueItem item in _items) {
                foreach (StyledLine line in ValueLines(item, int.MaxValue / 2, numbers, context)) {
                    values = Math.Max(values, line.Width);
                }
            }
            int available = Math.Max(0, width - Math.Max(0, Gap));
            if (longest + values <= available) { return longest; }
            // Keep values on one line if possible, then spend the remaining cells on labels.
            int preferred = Math.Min(longest, Math.Max(4, (int)(width * MaxKeyFraction)));
            return Math.Max(0, Math.Min(preferred, available - Math.Min(values, Math.Max(1, available - preferred))));
        }

        private int NumberWidth(RenderContext context, int width) {
            if (NumberColumn.Alignment == HorizontalAlignment.Left) {
                return 0;
            }
            int widest = 0;
            foreach (KeyValueItem item in _items) {
                string text = TextOf(item, context);
                if (text.Trim().Length == 0) {
                    continue;
                }
                string number = LeadingNumber(text);
                // A numeric column: every value starts with a number ("234 / 332", "8.4 rules/s", "~12s").
                if (number.Length == 0) {
                    return 0;
                }
                widest = Math.Max(widest, DisplayWidth.Measure(number));
            }
            if (widest == 0) { return 0; }
            int resolved = _numberColumn.Resolve(widest);
            int keyWidth = 0, valueWidth = 0;
            foreach (KeyValueItem item in _items) {
                keyWidth = Math.Max(keyWidth, DisplayWidth.Measure(item.Key));
                foreach (StyledLine line in ValueLines(item, int.MaxValue / 2, resolved, context)) { valueWidth = Math.Max(valueWidth, line.Width); }
            }
            // Numeric alignment is optional when it would force otherwise fitting labels or values to wrap.
            return keyWidth + Gap + valueWidth <= width ? resolved : 0;
        }

        // The leading number of a value with an optional sign or "~" and an optional unit ("8.4", "~12s", "77%", "6 064 519"):
        // culture group separators (non-breaking or thin spaces, apostrophes) count as part of the number between digits.
        private static string LeadingNumber(string value) {
            if (string.IsNullOrEmpty(value) || value[0] == '[') {
                return string.Empty;
            }
            int end = value.IndexOf(' ');
            string role = end < 0 ? value : value.Substring(0, end);
            int i = 0;
            if (i < role.Length && (role[i] == '~' || role[i] == '+' || role[i] == '-' || role[i] == '\u2248')) {
                i++;
            }
            if (i >= role.Length || !char.IsDigit(role[i])) {
                return string.Empty;
            }
            while (i < role.Length && (char.IsDigit(role[i]) || IsNumberSeparator(role, i))) {
                i++;
            }
            for (; i < role.Length; i++) {
                if (!char.IsLetter(role[i]) && role[i] != '%' && role[i] != '/') {
                    return string.Empty;
                }
            }
            return role;
        }

        private CellContent Present(KeyValueItem item, RenderContext context) {
            return CellRendering.Content(item.Cell!, item.Presenter, item.Rules, Presentation.CreateContext(context));
        }

        private string TextOf(KeyValueItem item, RenderContext context) {
            return item.Cell != null ? Markup.Escape(Present(item, context).Lines[0].PlainText) : item.Value;
        }

        private static bool IsNumberSeparator(string role, int index) {
            char c = role[index];
            bool between = index > 0 && index + 1 < role.Length && char.IsDigit(role[index - 1]) && char.IsDigit(role[index + 1]);
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
