using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management.Automation;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell.Display;

/// <summary>A broker operation containing original objects; it never substitutes pipeline data.</summary>
public sealed class DefaultDisplayOperation {
    internal DefaultDisplayOperation(DefaultDisplayRegistration? registration, IReadOnlyList<PSObject> objects, string scope, bool streamEnd = false) {
        Registration = registration; Objects = objects; Scope = scope; StreamEnd = streamEnd;
    }
    internal DefaultDisplayRegistration? Registration { get; }
    internal string Scope { get; }
    internal bool StreamEnd { get; }
    /// <summary>Whether the broker may attempt a registered builder before native fallback.</summary>
    public bool IsRich => Registration != null;
    /// <summary>Unmodified input wrappers, in their original order, for native delegation.</summary>
    public IReadOnlyList<PSObject> Objects { get; }
}

/// <summary>One invocation's bounded, consecutive batch state.</summary>
public sealed class DefaultDisplaySession : IDisposable {
    private readonly DisplayRunspaceState _state;
    private readonly PSCmdlet _sink;
    private readonly bool _topLevel;
    private readonly bool _transcript;
    private readonly List<PSObject> _pending = new List<PSObject>();
    private readonly Stopwatch _clock = new Stopwatch();
    private DefaultDisplayRegistration? _current;
    private string _scope = string.Empty;
    private long _bytes;
    private bool _nativeBatch;
    private bool _disposed;
    private CollectingStatusLine? _status;
    private DefaultDisplayStream? _stream;
    private bool _streamFailed;

    internal DefaultDisplaySession(DisplayRunspaceState state, PSCmdlet sink, bool topLevel, bool transcript) {
        _state = state; _sink = sink; _topLevel = topLevel; _transcript = transcript;
    }
    /// <summary>Accepts one sink record. Explicit format records and unregistered types are native boundaries.</summary>
    public IReadOnlyList<DefaultDisplayOperation> Receive(PSObject? value) {
        if (_disposed) { throw new ObjectDisposedException(nameof(DefaultDisplaySession)); }
        List<DefaultDisplayOperation> output = new List<DefaultDisplayOperation>();
        if (value == null) { return output; }
        TerminalProfile profile = Terminal.GetCurrentProfile();
        DefaultDisplayRegistration? view = DefaultDisplayHostGate.CanRender(_sink, _topLevel, _transcript, profile) ? _state.Find(value) : null;
        string scope = string.Empty;
        if (view != null) {
            try { scope = view.ScopeSelector?.Invoke(value.BaseObject) ?? DisplayData.Scope(value.BaseObject, view.ScopeKey); }
            catch (Exception error) when (!(error is PipelineStoppedException)) {
                if (_state.Diagnostic(view.ViewId)) { _sink.WriteVerbose("ConsoleMonster view '" + view.ViewId + "' used native formatting: " + error.Message); }
                view = null;
            }
        }
        if (!ReferenceEquals(view, _current) || scope != _scope || view == null) {
            Flush(output);
            _current = view; _scope = scope; _nativeBatch = false; _bytes = 0; _clock.Restart();
            _streamFailed = false;
        }
        // Out-Default has no supported notification for an unmerged host stream emitted by a producer.
        // Without a verified upstream routing contract, Collect must use native output from its first record.
        if (view?.Batch == DefaultDisplayBatch.Collect && !CollectionOrderingVerified) { view = null; _current = null; }
        if (view == null || _nativeBatch) { output.Add(new DefaultDisplayOperation(null, new[] { value }, scope)); return output; }
        if (view.Batch != DefaultDisplayBatch.Collect) {
            output.Add(new DefaultDisplayOperation(view, new[] { value }, scope));
            return output;
        }
        long estimate;
        try { estimate = DisplayData.Estimate(value.BaseObject, view.Budget.Bytes); }
        catch (Exception) { estimate = view.Budget.Bytes + 1; }
        if (_pending.Count >= view.Budget.Records || estimate > view.Budget.Bytes - _bytes) {
            _status?.Dispose(); _status = null;
            _nativeBatch = true;
            if (_pending.Count > 0) { output.Add(new DefaultDisplayOperation(null, _pending.ToArray(), _scope)); _pending.Clear(); }
            output.Add(new DefaultDisplayOperation(null, new[] { value }, scope));
            return output;
        }
        _pending.Add(value); _bytes += estimate;
        if (_clock.ElapsedMilliseconds >= 1000 && profile.IsLive) {
            if (_status == null) { _status = new CollectingStatusLine(_sink); }
            _status.Update(_pending.Count, profile.Width);
        }
        return output;
    }
    /// <summary>False for automatic PowerShell sinks: unmerged streams bypass Out-Default.</summary>
    public bool CollectionOrderingVerified => false;
    /// <summary>Closes a pending batch at end of output.</summary>
    public IReadOnlyList<DefaultDisplayOperation> Complete() {
        List<DefaultDisplayOperation> output = new List<DefaultDisplayOperation>(); Flush(output); return output;
    }
    private void Flush(List<DefaultDisplayOperation> output) {
        _status?.Dispose(); _status = null;
        if (_pending.Count > 0) { output.Add(new DefaultDisplayOperation(_nativeBatch ? null : _current, _pending.ToArray(), _scope)); _pending.Clear(); }
        if (_current?.Batch == DefaultDisplayBatch.Stream) { output.Add(new DefaultDisplayOperation(_current, Array.Empty<PSObject>(), _scope, true)); }
        _bytes = 0;
    }
    /// <summary>Builds and renders, returning false for native fallback. Diagnostics are verbose once per view/runspace.</summary>
    public bool Render(DefaultDisplayOperation operation) {
        DefaultDisplayRegistration? view = operation.Registration;
        if (view == null) { return false; }
        TerminalProfile profile = Terminal.GetCurrentProfile();
        if (!DefaultDisplayHostGate.CanRender(_sink, _topLevel, _transcript, profile) || _state.Mode(view.Lease) == DefaultDisplayMode.Off) {
            _stream?.Dispose(); _stream = null;
            return false;
        }
        string encoded;
        try {
            DefaultDisplayMode mode = _state.Mode(view.Lease);
            object? style = _sink.SessionState.PSVariable.GetValue("PSStyle");
            bool plainStyle = style != null && string.Equals(style.GetType().GetProperty("OutputRendering")?.GetValue(style, null)?.ToString(), "PlainText", StringComparison.Ordinal);
            if (mode == DefaultDisplayMode.Plain || Environment.GetEnvironmentVariable("NO_COLOR") != null || plainStyle) {
                profile = profile.WithColorDepth(ColorDepth.None).WithMode(OutputMode.Plain);
            }
            DefaultDisplayContext context = new DefaultDisplayContext(profile, operation.Scope, mode);
            if (_stream != null && !_stream.MatchesProfile(profile)) {
                _stream.Dispose(); _stream = null; _streamFailed = true;
                return operation.StreamEnd; // Never emit a stream's previous decoration/width after a route change.
            }
            if (operation.StreamEnd) {
                encoded = _stream?.Complete() ?? string.Empty;
                _stream = null;
            } else {
                if (_streamFailed && view.Batch == DefaultDisplayBatch.Stream) { return false; }
                object[] objects = operation.Objects.Select(value => value.BaseObject).ToArray();
                if (view.Batch == DefaultDisplayBatch.Stream) {
                    if (_stream == null) { _stream = new DefaultDisplayStream(view, context); }
                    encoded = _stream.Append(objects[0]);
                } else { encoded = DefaultDisplayRenderer.Prepare(view.Builder(objects, context), profile, view.Budget.Bytes); }
            }
        } catch (Exception ex) when (!(ex is PipelineStoppedException)) {
            if (view.Batch == DefaultDisplayBatch.Stream) { _stream?.Dispose(); _stream = null; _streamFailed = true; }
            if (_state.Diagnostic(view.ViewId)) { _sink.WriteVerbose("ConsoleMonster view '" + view.ViewId + "' used native formatting: " + ex.Message); }
            return false;
        }
        // Recheck after user code. A builder may have started transcription or changed the route.
        if (!DefaultDisplayHostGate.CanRender(_sink, _topLevel, _transcript, Terminal.GetCurrentProfile()) || _state.Mode(view.Lease) == DefaultDisplayMode.Off) { return false; }
        // One bounded host call. Host transport errors are not renderer errors: replay could duplicate a partial write.
        if (encoded.Length > 0) { _sink.Host.UI.Write(encoded); }
        return true;
    }
    /// <summary>Erases owned transient state without writing pipeline objects.</summary>
    public void Dispose() {
        if (_disposed) { return; }
        _disposed = true; _status?.Dispose(); _status = null; _stream?.Dispose(); _stream = null; _pending.Clear();
    }
}
