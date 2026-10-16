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
            foreach (KeyValueItem item in _items) {
                IReadOnlyList<StyledLine> lines = ValueLines(item, valueWidth, context);
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
            int y = 0;
            foreach (KeyValueItem item in _items) {
                if (y >= region.Height) {
                    break;
                }
                region.Write(0, y, TextWrapper.Truncate(item.Key, keyWidth, context.Ellipsis), keyStyle);
                IReadOnlyList<StyledLine> lines = ValueLines(item, valueWidth, context);
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

        private static IReadOnlyList<StyledLine> ValueLines(KeyValueItem item, int width, RenderContext context) {
            List<StyledLine> result = new List<StyledLine>();
            string prefix = item.Status.HasValue ? context.Glyphs.StatusMarker(item.Status.Value) + " " : string.Empty;
            Style markerStyle = item.Status.HasValue ? context.Style(item.Status.Value) : Style.Plain;
            StyledText text = context.Markup(item.Value);
            bool first = true;
            foreach (StyledLine line in text.Lines) {
                StyledLine full = line;
                if (first && prefix.Length > 0) {
                    List<TextSpan> spans = new List<TextSpan> { new TextSpan(prefix, markerStyle) };
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
