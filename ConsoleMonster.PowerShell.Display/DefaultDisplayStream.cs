using System;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell.Display;

/// <summary>Incremental preparation of one streamed scope, without host I/O or PowerShell objects.</summary>
public sealed class DefaultDisplayStream : IDisposable {
    private readonly DefaultDisplayRegistration _registration;
    private readonly DefaultDisplayContext _context;
    private DisplayTableStream? _table;
    private bool _closed;
    /// <summary>Creates a pure preparation session. Automatic host streaming samples one row to avoid holding output.</summary>
    public DefaultDisplayStream(DefaultDisplayRegistration registration, DefaultDisplayContext context) {
        _registration = registration ?? throw new ArgumentNullException(nameof(registration));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        if (registration.Batch != DefaultDisplayBatch.Stream) { throw new ArgumentException("A Stream registration is required.", nameof(registration)); }
    }
    /// <summary>Builds one row and returns a fully prepared, bounded block; the first contains the only column header.</summary>
    public string Append(object value) {
        if (_closed) { throw new ObjectDisposedException(nameof(DefaultDisplayStream)); }
        try {
            if (!(_registration.Build(new[] { value }, _context) is Table row)) { throw new InvalidOperationException("A Stream builder must return a Table with one row and stable columns."); }
            if (_table == null) { _table = new DisplayTableStream(row, _registration, _context); }
            return _table.Append(row, value);
        } catch { Dispose(); throw; }
    }
    /// <summary>Returns the closed scope's summary once; repeated completion is empty.</summary>
    public string Complete() {
        if (_closed) { return string.Empty; }
        try { return _table?.Complete() ?? string.Empty; }
        finally { Dispose(); }
    }
    internal bool MatchesProfile(TerminalProfile profile) => profile.Width == _context.Profile.Width && profile.Mode == _context.Profile.Mode
        && profile.ColorDepth == _context.Profile.ColorDepth && profile.Glyphs == _context.Profile.Glyphs;
    internal TerminalProfile Profile => _context.Profile;
    /// <summary>Releases the iterator; early disposal does not print or build a completion summary.</summary>
    public void Dispose() { if (_closed) { return; } _closed = true; _table?.Dispose(); _table = null; }
}
