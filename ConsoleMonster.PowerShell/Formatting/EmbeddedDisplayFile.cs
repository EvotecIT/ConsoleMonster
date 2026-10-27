using System;
using System.Collections.Generic;
using ConsoleMonster.PowerShell.Display;

namespace ConsoleMonster.PowerShell;

// Saved definitions survive Output disable; only their active lease participates in shared resolution.
internal sealed class EmbeddedDisplayFile : IDisposable {
    private readonly string _moduleName;
    private readonly ConsoleViewDocument _document;
    private readonly DefaultDisplaySource _source;
    private readonly HashSet<string> _types;
    private ModuleLease? _active;

    internal EmbeddedDisplayFile(string moduleName, ConsoleViewDocument document, DefaultDisplaySource source) {
        _moduleName = moduleName; _document = document; _source = source;
        _types = new HashSet<string>(document.TypeNames, StringComparer.Ordinal);
    }
    internal void SetEnabled(bool enabled) {
        if (!enabled) { Suspend(); return; }
        if (_active != null || _types.Count == 0) { return; }
        ModuleLease lease = DefaultDisplay.RegisterModule(_moduleName, new Version(1, 0, 0), _document.TypeNames, source: _source);
        try {
            _document.Register(lease);
            foreach (string type in _document.TypeNames) { if (!_types.Contains(type)) { lease.Unregister(type); } }
            _active = lease;
        } catch { lease.Dispose(); throw; }
    }
    internal bool Unregister(string typeName) {
        if (!_types.Remove(typeName)) { return false; }
        _active?.Unregister(typeName);
        if (_types.Count == 0) { Suspend(); }
        return true;
    }
    private void Suspend() { _active?.Dispose(); _active = null; }
    public void Dispose() { Suspend(); _types.Clear(); }
}
