using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ConsoleMonster.Canvas;
using ConsoleMonster.Layout;
using ConsoleMonster.Output;
using ConsoleMonster.Rendering;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell.Display;

// Adapts the core pull iterator to the runspace's ordered push calls. MoveNext is never called without a queued row.
internal sealed class DisplayTableStream : IDisposable {
    private readonly Queue<Row> _queue = new Queue<Row>();
    private readonly IEnumerator<CellBuffer> _chunks;
    private readonly DefaultDisplayRegistration _registration;
    private readonly DefaultDisplayContext _context;
    private readonly int _columns;
    private readonly DisplayStreamColumns _layout;
    private long _rendered;
    private long _received;
    private bool _complete;
    private bool _disposed;
    private readonly int _width;
    private readonly bool _gutter;
    internal DisplayTableStream(Table prototype, DefaultDisplayRegistration registration, DefaultDisplayContext context, bool sampled = false) {
        _registration = registration; _context = context; _columns = prototype.Columns.Count;
        DefaultDisplayStreaming? streaming = registration.Streaming;
        if (_columns == 0) { throw new InvalidOperationException("A streaming builder must define table columns."); }
        if (prototype.Rows.Count < 1 || !sampled && prototype.Rows.Count != 1) { throw new InvalidOperationException("A streaming builder must return exactly one row."); }
        _gutter = sampled;
        _width = Math.Max(1, context.Width - (sampled ? 2 : 0));
        _layout = new DisplayStreamColumns(prototype, context, sampled, _width);
        GroupedStreamingTable<Row, object?> table = new GroupedStreamingTable<Row, object?>(_layout.Columns,
            row => row.Group, row => row.Cells,
            key => streaming?.Seed(),
            (aggregate, row) => {
                object? updated = streaming == null ? aggregate : streaming.Aggregate(aggregate, row.Value);
                _rendered++;
                return updated;
            },
            (key, aggregate, count) => streaming == null
                ? TextBlock.Plain(count.ToString(context.Culture) + " rows", ThemeToken.Muted)
                : streaming.Summary(aggregate, count, context)) {
            // The host session has already sampled on ordered runspace callbacks; the pull bridge never waits.
            SampleRows = 1, Presentation = prototype.Presentation, Border = TableBorder.Rule, RepeatHeaderPerGroup = false
        };
        _chunks = table.RenderChunks(Read(), _width, ConsoleViewStyle.Apply(Terminal.CreateContext(context.Profile))).GetEnumerator();
    }
    internal string Append(Table row, object value) {
        if (_disposed || _complete) { throw new ObjectDisposedException(nameof(DisplayTableStream)); }
        if (row.Columns.Count != _columns || row.Rows.Count != 1) { throw new InvalidOperationException("A streaming builder must return one table row with stable column definitions."); }
        _queue.Enqueue(new Row(value, _layout.Indices.Select(index => row.Rows[0][index]).ToArray(), ConsoleMonster.Text.Markup.Strip(row.Title ?? string.Empty)));
        _received++;
        BoundedTextWriter writer = new BoundedTextWriter(_registration.Budget.Bytes);
        using (ITerminalOutput output = Terminal.CreateOutput(_context.Profile, writer)) {
            if (_received == 1 && _layout.Hidden != null) {
                Write(output, FrameComposer.RenderBlock(TextBlock.Plain(_layout.Hidden, ThemeToken.Muted), _width, ConsoleViewStyle.Apply(Terminal.CreateContext(_context.Profile))));
            }
            // Hidden-column notices and the group heading may precede the row. Stop exactly after that row's chunk.
            while (_rendered < _received) { Advance(output); }
        }
        return writer.ToString();
    }
    internal string Complete() {
        if (_disposed) { return string.Empty; }
        _complete = true;
        BoundedTextWriter writer = new BoundedTextWriter(_registration.Budget.Bytes);
        using (ITerminalOutput output = Terminal.CreateOutput(_context.Profile, writer)) {
            while (_chunks.MoveNext()) { Write(output, _chunks.Current); }
        }
        Dispose();
        return writer.ToString();
    }
    private void Advance(ITerminalOutput output) {
        if (!_chunks.MoveNext()) { throw new InvalidOperationException("The streaming table ended before its scope closed."); }
        Write(output, _chunks.Current);
    }
    private void Write(ITerminalOutput output, CellBuffer block) {
        if (!_gutter) { output.WriteBlock(block); return; }
        CellBuffer padded = new CellBuffer(block.Width + 2, block.Height);
        padded.Blit(block, 0, 0, block.Width, block.Height, 1, 0);
        output.WriteBlock(padded);
    }
    private IEnumerable<Row> Read() {
        while (!_complete || _queue.Count > 0) {
            if (_queue.Count == 0) { throw new InvalidOperationException("The streaming iterator advanced without a producer record."); }
            yield return _queue.Dequeue();
        }
    }
    public void Dispose() {
        if (_disposed) { return; }
        _disposed = true; _chunks.Dispose(); _queue.Clear();
    }
    private sealed class Row {
        internal Row(object value, TableCell[] cells, string group) { Value = value; Cells = cells; Group = group; }
        internal object Value { get; }
        internal TableCell[] Cells { get; }
        internal string Group { get; }
    }
    private sealed class BoundedTextWriter : ITerminalWriter {
        private readonly StringBuilder _text = new StringBuilder();
        private readonly long _maximum;
        internal BoundedTextWriter(long maximum) { _maximum = maximum; }
        public bool IsConsole => false;
        public void Write(string text) {
            if (((long)_text.Length + text.Length) * 2 > _maximum) { throw new InvalidOperationException("The rendered stream block exceeds its output budget."); }
            _text.Append(text);
        }
        public void Flush() { }
        public void Dispose() { }
        public override string ToString() => _text.ToString();
    }
}
