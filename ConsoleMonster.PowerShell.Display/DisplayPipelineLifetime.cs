using System;
using System.Management.Automation.Runspaces;
using System.Reflection;

namespace ConsoleMonster.PowerShell.Display;

// The script end block is skipped when an upstream producer terminates. Observe lifecycle only;
// never intercept streams or render a late completion frame from this callback.
internal sealed class DisplayPipelineLifetime : IDisposable {
    private readonly object _pipeline;
    private readonly EventInfo _event;
    private readonly Delegate _handler;
    private readonly Action _finished;
    private DisplayPipelineLifetime(object pipeline, EventInfo stateChanged, Action finished) {
        _pipeline = pipeline; _event = stateChanged; _finished = finished;
        _handler = Delegate.CreateDelegate(stateChanged.EventHandlerType!, this, GetType().GetMethod(nameof(StateChanged), BindingFlags.Instance | BindingFlags.NonPublic)!);
        stateChanged.AddEventHandler(pipeline, _handler);
    }
    internal static DisplayPipelineLifetime? Observe(Action finished) {
        try {
            Runspace? owner = Runspace.DefaultRunspace;
            object? pipeline = owner?.GetType().GetMethod("GetCurrentlyRunningPipeline", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.Invoke(owner, null);
            EventInfo? changed = pipeline?.GetType().GetEvent("StateChanged");
            return pipeline != null && changed?.EventHandlerType != null && State(pipeline) == "Running"
                ? new DisplayPipelineLifetime(pipeline, changed, finished) : null;
        } catch (Exception) { return null; }
    }
    private static string? State(object value) {
        object? info = value.GetType().GetProperty("PipelineStateInfo")?.GetValue(value, null);
        return info?.GetType().GetProperty("State")?.GetValue(info, null)?.ToString();
    }
    private void StateChanged(object? sender, EventArgs args) {
        string? state = State(args);
        if (state == "Completed" || state == "Failed" || state == "Stopped") { _finished(); }
    }
    public void Dispose() => _event.RemoveEventHandler(_pipeline, _handler);
}
