using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management.Automation;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Frames;
using ConsoleMonster.Views;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell.Display;

/// <summary>A broker operation containing original objects; it never substitutes pipeline data.</summary>
public sealed class DefaultDisplayOperation {
    internal DefaultDisplayOperation(DefaultDisplayRegistration? registration, IReadOnlyList<PSObject> objects, string scope, bool streamEnd = false, long nativeCount = 0,
        Func<object, DefaultDisplayContext, IWidget>? singleBuilder = null, bool collected = false) {
        Registration = registration; Objects = objects; Scope = scope; StreamEnd = streamEnd; NativeCount = nativeCount; SingleBuilder = singleBuilder; Collected = collected;
    }
    internal DefaultDisplayRegistration? Registration { get; }
    internal string Scope { get; }
    internal bool StreamEnd { get; }
    internal long NativeCount { get; }
    internal Func<object, DefaultDisplayContext, IWidget>? SingleBuilder { get; }
    internal bool Collected { get; }
    /// <summary>Whether the broker may attempt a registered builder before native fallback.</summary>
    public bool IsRich => Registration != null;
    /// <summary>Unmodified input wrappers, in their original order, for native delegation.</summary>
    public IReadOnlyList<PSObject> Objects { get; }
}

/// <summary>One invocation's bounded, consecutive batch state.</summary>
public sealed partial class DefaultDisplaySession : IDisposable {
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
    private long _nativeCount;
    private bool _disposed;
    private CollectingStatusLine? _status;
    private DefaultDisplayStream? _stream;
    private bool _streamFailed;
    private DefaultDisplayRegistration? _failedView;
    private string _failedScope = string.Empty;
    private readonly DisplayPipelineLifetime? _lifetime;
    private Func<object, DefaultDisplayContext, IWidget>? _singleBuilder;
    private bool _streamStarted;
    private bool _separatorPending;

    internal DefaultDisplaySession(DisplayRunspaceState state, PSCmdlet sink, bool topLevel, bool transcript) {
        _state = state; _sink = sink; _topLevel = topLevel; _transcript = transcript;
        _lifetime = DisplayPipelineLifetime.Observe(Dispose);
    }
    /// <summary>Accepts one sink record. Explicit format records and unregistered types are native boundaries.</summary>
    public IReadOnlyList<DefaultDisplayOperation> Receive(PSObject? value) {
        if (_disposed) { throw new ObjectDisposedException(nameof(DefaultDisplaySession)); }
        List<DefaultDisplayOperation> output = new List<DefaultDisplayOperation>();
        if (value == null) { return output; }
        TerminalProfile profile = Terminal.GetCurrentProfile();
        DefaultDisplayRegistration? view = DefaultDisplayHostGate.CanRender(_sink, _topLevel, _transcript, profile) ? _state.Find(value) : null;
        if (_lifetime == null && (view?.Batch == DefaultDisplayBatch.Collect || view?.Batch == DefaultDisplayBatch.Stream)) { view = null; }
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
            _singleBuilder = view?.Batch == DefaultDisplayBatch.Stream ? view.SingleRecordBuilder : null;
            _streamStarted = false;
        }
        if (view == null || _nativeBatch) {
            if (_nativeBatch) { _nativeCount++; }
            output.Add(new DefaultDisplayOperation(null, new[] { value }, scope)); return output;
        }
        if (view.Batch == DefaultDisplayBatch.Stream && !_streamStarted) {
            Sample(value, view, output);
            return output;
        }
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
            _nativeCount = _pending.Count + 1L;
            if (_pending.Count > 0) { output.Add(new DefaultDisplayOperation(null, _pending.ToArray(), _scope)); _pending.Clear(); }
            output.Add(new DefaultDisplayOperation(null, new[] { value }, scope));
            return output;
        }
        _pending.Add(value); _bytes += estimate;
        if (_clock.ElapsedMilliseconds >= 1000) {
            if (_status == null) { _status = new CollectingStatusLine(_sink); }
            _status.Update(_pending.Count, profile.Width, _state.Mode(view.Lease) == DefaultDisplayMode.Plain
                || profile.Mode == OutputMode.Plain || profile.ColorDepth == ColorDepth.None || Environment.GetEnvironmentVariable("NO_COLOR") != null || PlainStyle());
        }
        return output;
    }
    /// <summary>Whether pipeline lifecycle is observable. Independent warning, verbose and information timing remains native.</summary>
    public bool CollectionOrderingVerified => _lifetime != null;
    /// <summary>Closes a pending batch at end of output.</summary>
    public IReadOnlyList<DefaultDisplayOperation> Complete() {
        List<DefaultDisplayOperation> output = new List<DefaultDisplayOperation>(); Flush(output); return output;
    }
    private void Flush(List<DefaultDisplayOperation> output) {
        _status?.Dispose(); _status = null;
        if (_pending.Count > 0) {
            bool stream = _current?.Batch == DefaultDisplayBatch.Stream && !_nativeBatch;
            bool collected = stream && _clock.ElapsedMilliseconds < 250;
            output.Add(new DefaultDisplayOperation(_nativeBatch ? null : _current, _pending.ToArray(), _scope,
                singleBuilder: collected && _pending.Count == 1 ? _singleBuilder : null, collected: collected));
            if (stream && !collected) { _streamStarted = true; }
            _pending.Clear();
        }
        if (_nativeCount > 0) { output.Add(new DefaultDisplayOperation(_current, Array.Empty<PSObject>(), _scope, nativeCount: _nativeCount)); _nativeCount = 0; }
        if (_current?.Batch == DefaultDisplayBatch.Stream && _streamStarted && !_nativeBatch) { output.Add(new DefaultDisplayOperation(_current, Array.Empty<PSObject>(), _scope, true)); }
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
            bool plainStyle = PlainStyle();
            if (mode == DefaultDisplayMode.Plain || Environment.GetEnvironmentVariable("NO_COLOR") != null || plainStyle) {
                profile = profile.WithColorDepth(ColorDepth.None).WithMode(OutputMode.Plain);
            }
            if (_stream != null && !_stream.MatchesProfile(profile)) {
                _stream.Dispose(); _stream = null; _streamFailed = true; _failedView = view; _failedScope = operation.Scope;
                return operation.StreamEnd; // Never emit a stream's previous decoration/width after a route change.
            }
            // A streamed scope keeps its opening pair across native messages and its final footer.
            profile = _stream?.Profile ?? CurrentHostColors(profile);
            DefaultDisplayContext context = new DefaultDisplayContext(profile, operation.Scope, mode, ViewLength());
            using IDisposable length = FrameScale.UseLength(context.Length);
            if (operation.SingleBuilder != null) {
                encoded = DefaultDisplayRenderer.Prepare(operation.SingleBuilder(operation.Objects[0].BaseObject, context), profile, view.Budget.Bytes);
            } else if (operation.Collected) {
                object[] objects = operation.Objects.Select(value => value.BaseObject).ToArray();
                IWidget widget = view.CollectedBuilder != null ? view.CollectedBuilder(objects, context)
                    : DisplayStreamSample.BuildTable(view, context, objects);
                encoded = DefaultDisplayRenderer.Prepare(widget, profile, view.Budget.Bytes);
            } else if (operation.NativeCount > 0) {
                encoded = DefaultDisplayRenderer.Prepare(TextBlock.Plain(operation.NativeCount.ToString(context.Culture) + " rows · native output · collection budget exceeded", Styling.ThemeToken.Muted), profile);
            } else if (operation.StreamEnd) {
                encoded = _stream?.Complete() ?? string.Empty;
                _stream = null;
            } else {
                if (_streamFailed && ReferenceEquals(view, _failedView) && operation.Scope == _failedScope && view.Batch == DefaultDisplayBatch.Stream) { return false; }
                object[] objects = operation.Objects.Select(value => value.BaseObject).ToArray();
                if (view.Batch == DefaultDisplayBatch.Stream) {
                    if (_stream == null) { _stream = new DefaultDisplayStream(view, context, objects); }
                    System.Text.StringBuilder block = new System.Text.StringBuilder();
                    foreach (object value in objects) {
                        block.Append(_stream.Append(value));
                        if ((long)block.Length * 2 > view.Budget.Bytes) { throw new InvalidOperationException("The rendered stream block exceeds its output budget."); }
                    }
                    encoded = block.ToString();
                } else { encoded = DefaultDisplayRenderer.Prepare(view.Builder(objects, context), profile, view.Budget.Bytes); }
            }
            if (encoded.Length > 0) {
                if (_separatorPending) { encoded = Environment.NewLine + encoded; }
                long maximum = operation.NativeCount > 0 ? 16 * 1024 * 1024 : view.Budget.Bytes;
                if ((long)encoded.Length * 2 > maximum) { throw new InvalidOperationException("The completed frame exceeds its output budget."); }
            }
        } catch (Exception ex) when (!(ex is PipelineStoppedException)) {
            if (view.Batch == DefaultDisplayBatch.Stream) { _stream?.Dispose(); _stream = null; _streamFailed = true; _failedView = view; _failedScope = operation.Scope; }
            if (_state.Diagnostic(view.ViewId)) { _sink.WriteVerbose("ConsoleMonster view '" + view.ViewId + "' used native formatting: " + ex.Message); }
            return false;
        }
        // Recheck after user code. A builder may have started transcription or changed the route.
        if (!DefaultDisplayHostGate.CanRender(_sink, _topLevel, _transcript, Terminal.GetCurrentProfile()) || _state.Mode(view.Lease) == DefaultDisplayMode.Off) { return false; }
        // One bounded host call. Host transport errors are not renderer errors: replay could duplicate a partial write.
        if (encoded.Length > 0) {
            _sink.Host.UI.Write(encoded);
            _separatorPending = operation.SingleBuilder != null || operation.Collected || operation.NativeCount > 0 || operation.StreamEnd || view.Batch != DefaultDisplayBatch.Stream;
        }
        if (operation.StreamEnd) { _separatorPending = true; }
        return true;
    }
    /// <summary>Erases owned transient state without writing pipeline objects.</summary>
    public void Dispose() {
        if (_disposed) { return; }
        _disposed = true; _lifetime?.Dispose(); _status?.Dispose(); _status = null; _stream?.Dispose(); _stream = null; _pending.Clear();
    }
    private TerminalProfile CurrentHostColors(TerminalProfile profile) {
        if (profile.Mode == OutputMode.Plain || profile.ColorDepth == ColorDepth.None) { return profile; }
        // Read both before running a builder. An unavailable RawUI pair uses Render's native-fallback path.
        System.Management.Automation.Host.PSHostRawUserInterface raw = _sink.Host.UI.RawUI;
        ConsoleColor foreground = raw.ForegroundColor;
        ConsoleColor background = raw.BackgroundColor;
        return profile.WithDefaultColors(Styling.Color.FromConsoleColor(foreground), Styling.Color.FromConsoleColor(background));
    }
    /// <summary>Reads <c>$ConsoleViewLengthPreference</c> as a string or enum only; other values never run application code.</summary>
    private ConsoleViewLength? ViewLength() {
        object? value;
        try { value = _sink.SessionState.PSVariable.GetValue("ConsoleViewLengthPreference"); }
        catch (Exception) { return null; }
        if (value is PSObject wrapped) { value = wrapped.BaseObject; }
        if (value == null) { return null; }
        if (value is ConsoleViewLength typed && Enum.IsDefined(typeof(ConsoleViewLength), typed)) { return typed; }
        if (value is string text && FrameScale.TryParseLength(text, out ConsoleViewLength parsed)) { return parsed; }
        if (_state.Diagnostic("ConsoleViewLengthPreference")) { _sink.WriteVerbose("$ConsoleViewLengthPreference must be Summary, Standard or Full; ConsoleMonster views use the default length."); }
        return null;
    }
    private bool PlainStyle() {
        try {
            object? style = _sink.SessionState.PSVariable.GetValue("PSStyle");
            return style != null && string.Equals(style.GetType().GetProperty("OutputRendering")?.GetValue(style, null)?.ToString(), "PlainText", StringComparison.Ordinal);
        } catch (Exception) { return true; }
    }
}
