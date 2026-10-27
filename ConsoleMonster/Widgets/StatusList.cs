using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    /// <summary>
    /// A checklist: a status marker, a label and a right-aligned muted detail per row. Running rows show a spinner.
    /// Only the marker carries colour (one signal per row). Live operations: <c>set</c> (Key, State, Text = label, optional
    /// Total ignored) and <c>detail</c> (Key, Text).
    /// </summary>
    public sealed class StatusList : LiveWidget {
        private readonly List<StatusItem> _items = new List<StatusItem>();
        private readonly Dictionary<string, StatusItem> _byKey = new Dictionary<string, StatusItem>(StringComparer.Ordinal);

        /// <summary>Creates a status list.</summary>
        /// <param name="widgetId">The live name (default <c>status</c>).</param>
        public StatusList(string widgetId = "status") : base(widgetId) {
        }

        /// <summary>The rows.</summary>
        public IReadOnlyList<StatusItem> Items => _items;

        /// <summary>Adds or updates a row.</summary>
        /// <param name="key">Unique key.</param>
        /// <param name="label">Label (markup); <c>null</c> keeps the existing label or uses the key.</param>
        /// <param name="status">Status.</param>
        /// <param name="detail">Detail; <c>null</c> keeps the existing one.</param>
        /// <returns>This instance.</returns>
        public StatusList Set(string key, string? label, StatusKind status, string? detail = null) {
            if (key == null) {
                throw new ArgumentNullException(nameof(key));
            }
            if (_byKey.TryGetValue(key, out StatusItem? item)) {
                // Null or empty keeps the label (PowerShell passes $null to a .NET string as empty).
                if (!string.IsNullOrEmpty(label)) {
                    item.Label = label!;
                }
                item.Status = status;
                if (!string.IsNullOrEmpty(detail)) {
                    item.Detail = detail;
                }
            } else {
                item = new StatusItem(key, string.IsNullOrEmpty(label) ? Markup.Escape(key) : label!, status, string.IsNullOrEmpty(detail) ? null : detail);
                _byKey[key] = item;
                _items.Add(item);
            }
            return this;
        }

        /// <summary>Adds a row (alias of <see cref="Set"/>).</summary>
        /// <param name="label">Label (markup); also the key.</param>
        /// <param name="status">Status.</param>
        /// <param name="detail">Detail.</param>
        /// <returns>This instance.</returns>
        public StatusList Add(string label, StatusKind status, string? detail = null) => Set(label, label, status, detail);

        /// <summary>Adds or updates an item with a multicolour label (its plain text is the key).</summary>
        /// <param name="label">The label.</param>
        /// <param name="status">The status.</param>
        /// <param name="detail">An optional muted detail.</param>
        /// <returns>This instance.</returns>
        public StatusList Add(Text.RichText label, StatusKind status, string? detail = null) {
            Text.RichText rich = label ?? throw new ArgumentNullException(nameof(label));
            return Set(rich.PlainText, rich.ToMarkup(), status, detail);
        }

        /// <summary>Returns a handle that updates this list through a dispatcher.</summary>
        /// <param name="dispatcher">The dispatcher.</param>
        /// <returns>The handle.</returns>
        public StatusListHandle Handle(ILiveDispatcher dispatcher) => new StatusListHandle(dispatcher, WidgetId);

        /// <inheritdoc />
        public override void Apply(LiveEvent liveEvent) {
            if (liveEvent.Key == null) {
                return;
            }
            if (liveEvent.Operation == "set") {
                StatusKinds.TryParse(liveEvent.Status, out StatusKind status);
                Set(liveEvent.Key, liveEvent.Text, status);
            } else if (liveEvent.Operation == "detail" || liveEvent.Operation == "label") {
                if (!_byKey.TryGetValue(liveEvent.Key, out StatusItem? item)) {
                    Set(liveEvent.Key, null, StatusKind.Running);
                    item = _byKey[liveEvent.Key];
                }
                if (liveEvent.Operation == "label") { item.Label = liveEvent.Text ?? string.Empty; }
                else { item.Detail = liveEvent.Text; }
            }
        }

        /// <inheritdoc />
        public override string? Describe(LiveEvent liveEvent) {
            if (liveEvent.Operation == "set" && liveEvent.Key != null && _byKey.TryGetValue(liveEvent.Key, out StatusItem? item) && item.Status != StatusKind.Pending) {
                return item.Status.ToWord() + " " + Markup.Strip(item.Label) + (item.Detail == null ? string.Empty : " (" + item.Detail + ")");
            }
            return null;
        }

        /// <inheritdoc />
        public override Size Measure(Constraints constraints, RenderContext context) {
            int width = 0;
            foreach (StatusItem item in _items) {
                int w = DisplayWidth.Measure(context.Glyphs.StatusMarker(item.Status)) + 1 + context.Markup(item.Label).Width;
                if (!string.IsNullOrEmpty(item.Detail)) {
                    w += 2 + DisplayWidth.Measure(item.Detail);
                }
                width = Math.Max(width, w);
            }
            int height = constraints.IsHeightUnbounded ? _items.Count : Math.Min(_items.Count, constraints.MaxHeight);
            return new Size(Math.Min(width, constraints.MaxWidth), height);
        }

        /// <summary>
        /// Layout of the detail column on the right (default right-aligned). All rows share its width, which never shrinks
        /// during a session, so labels keep their room as details change.
        /// </summary>
        public ColumnFormat DetailColumn {
            get => _detailColumn.Format;
            set => _detailColumn.Format = value ?? throw new ArgumentNullException(nameof(value));
        }

        private readonly StatColumn _detailColumn = new StatColumn(new ColumnFormat());

        /// <inheritdoc />
        public override void Render(Region region, RenderContext context) {
            int markerWidth = 0;
            int detailColumn = 0;
            foreach (StatusItem item in _items) {
                markerWidth = Math.Max(markerWidth, DisplayWidth.Measure(context.Glyphs.StatusMarker(item.Status)));
                detailColumn = Math.Max(detailColumn, string.IsNullOrEmpty(item.Detail) ? 0 : DisplayWidth.Measure(item.Detail));
            }
            if (detailColumn > 0) {
                detailColumn = Math.Min(_detailColumn.Resolve(detailColumn), Math.Max(0, region.Width / 2));
            }
            Style muted = context.Style(ThemeToken.Muted);
            for (int i = 0; i < _items.Count && i < region.Height; i++) {
                StatusItem item = _items[i];
                string marker = item.Status == StatusKind.Running && context.Glyphs.Tier != Capabilities.GlyphTier.Ascii
                    ? context.Spinner()
                    : context.Glyphs.StatusMarker(item.Status);
                region.Write(0, i, marker, context.Style(item.Status));
                int labelX = markerWidth + 1;
                int labelWidth = Math.Max(0, region.Width - labelX - (detailColumn > 0 ? detailColumn + 2 : 0));
                StyledLine label = TextWrapper.Truncate(context.Markup(item.Label).Lines[0], labelWidth, context.Ellipsis);
                region.Write(labelX, i, label);
                if (detailColumn > 0 && !string.IsNullOrEmpty(item.Detail)) {
                    StatColumn.Write(region, region.Width - detailColumn, i, detailColumn, item.Detail!, muted, DetailColumn.Alignment, context.Ellipsis);
                }
            }
        }
    }
}
