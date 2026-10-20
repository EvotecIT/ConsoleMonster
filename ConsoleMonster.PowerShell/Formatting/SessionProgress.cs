using System;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Live;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// Runspace-owned progress for Enable-ConsoleFormatting. The first eligible reporting runspace owns the drawn area
    /// until its activities and progress scopes finish; other runspaces keep PowerShell's native progress. Rendering,
    /// throttling and stepping over host output are unchanged. A Use-ConsoleProgress override belongs to its runspace.
    /// </summary>
    internal static class SessionProgress {
        private static readonly object Gate = new object();
        private static RunspaceProgressState? _foreground;
        private const long DrawInterval = 66;

        internal static bool Enabled { get => FormattingSessionState.Current.ProgressEnabled; set => FormattingSessionState.Current.ProgressEnabled = value; }

        internal static ProgressBridge? Override {
            get { lock (Gate) { return Current.Override; } }
            set {
                lock (Gate) {
                    RunspaceProgressState state = Current;
                    state.Override = value;
                    ReleaseIfIdle(state);
                }
            }
        }

        private static RunspaceProgressState Current => FormattingSessionState.Current.Progress;

        /// <summary>Reserves the single area before Use-ConsoleProgress starts a live renderer; null means native progress.</summary>
        internal static IDisposable? TryEnterForeground() {
            lock (Gate) {
                RunspaceProgressState state = Current;
                if (state.NativePipeline || !CanOwn(state)) {
                    state.NativePipeline = true;
                    return null;
                }
                _foreground = state;
                state.ForegroundScopes++;
                ClearArea(state);
                return new ForegroundScope(state);
            }
        }

        /// <summary>False delegates to native PowerShell; neither another runspace's bridge nor its activities are touched.</summary>
        internal static bool Report(int id, int parentId, string? activity, string? status, int percent, int secondsRemaining, bool completed) {
            lock (Gate) {
                // This must precede the override: enabling progress in another runspace never enables it here.
                RunspaceProgressState state = Current;
                if (!Enabled) {
                    state.NativePipeline = true;
                    return false;
                }
                if (state.NativePipeline || !CanOwn(state)) {
                    // Keep the whole pipeline native: migrating an in-flight id could swallow its native completion.
                    state.NativePipeline = true;
                    return false;
                }
                ProgressBridge? bridge = state.Override;
                if (bridge != null) {
                    _foreground = state;
                    bridge.Progress(id, parentId, activity, status, percent, secondsRemaining, completed);
                    return true;
                }
                if (LiveSession.Current != null) {
                    return true;
                }
                state.Supported ??= InlineArea.IsSupported();
                if (!state.Supported.Value) {
                    state.NativePipeline = true;
                    return false;
                }
                _foreground = state;
                if (!state.Clock.IsRunning) {
                    state.Clock.Start();
                }
                state.Activities ??= new ActivityList();
                int before = state.Activities.Count;
                state.Activities.Apply(ProgressEvents.From(id, parentId, activity, status, percent, secondsRemaining, completed, Math.Max(1, state.Clock.ElapsedMilliseconds)));
                if (state.Activities.Count == 0) {
                    ClearArea(state);
                    state.DrawnText.Clear();
                    ReleaseIfIdle(state);
                    return true;
                }
                string text = (activity ?? string.Empty) + "\u0001" + (status ?? string.Empty);
                bool newText = completed || !state.DrawnText.TryGetValue(id, out string? drawn) || !string.Equals(drawn, text, StringComparison.Ordinal);
                long now = state.Clock.ElapsedMilliseconds;
                if (state.Activities.Count == before && !newText && now - state.LastDraw < DrawInterval) {
                    return true;
                }
                if (completed) {
                    state.DrawnText.Remove(id);
                } else {
                    state.DrawnText[id] = text;
                }
                state.LastDraw = now;
                Draw(state);
                return true;
            }
        }

        /// <summary>Hides only the caller's area before its output; activities stay for the next draw.</summary>
        internal static void Suspend() {
            lock (Gate) {
                RunspaceProgressState state = Current;
                if (state.Area != null && state.Area.Height > 0) {
                    ClearArea(state);
                }
            }
        }

        internal static void EndPipeline() {
            lock (Gate) {
                EndPipeline(Current);
            }
        }

        internal static void PipelineEnded(RunspaceProgressState state) {
            lock (Gate) {
                EndPipeline(state);
            }
        }

        /// <summary>Resets the captured installation's state, including when removal runs outside its original callback.</summary>
        internal static void Reset(RunspaceProgressState state) {
            lock (Gate) {
                state.Override = null;
                EndPipeline(state);
            }
        }

        internal static void Close(RunspaceProgressState state) {
            lock (Gate) {
                state.Override = null;
                EndPipeline(state);
                if (ReferenceEquals(_foreground, state)) {
                    _foreground = null;
                }
            }
        }

        private static void EndPipeline(RunspaceProgressState state) {
            ClearArea(state);
            state.Area = null;
            state.Activities = null;
            state.Supported = null;
            state.LastDraw = long.MinValue;
            state.DrawnText.Clear();
            state.Clock.Reset();
            state.NativePipeline = false;
            ReleaseIfIdle(state);
        }

        private static bool CanOwn(RunspaceProgressState state) {
            if (_foreground != null && !_foreground.IsAlive) {
                Close(_foreground);
            }
            return _foreground == null || ReferenceEquals(_foreground, state);
        }

        private static void ReleaseIfIdle(RunspaceProgressState state) {
            if (ReferenceEquals(_foreground, state) && state.ForegroundScopes == 0 && state.Override == null && (state.Activities == null || state.Activities.Count == 0)) {
                _foreground = null;
            }
        }

        private static void Draw(RunspaceProgressState state) {
            if (state.Activities == null) {
                return;
            }
            try {
                TerminalProfile profile = Terminal.GetCurrentProfile();
                RenderContext context = Terminal.CreateContext(profile).WithClock(0, state.Clock.Elapsed);
                state.Area ??= new InlineArea(profile) { MaxWidth = 100 };
                state.Area.Draw(state.Activities, context);
            } catch (Exception ex) when (ex is System.IO.IOException || ex is InvalidOperationException) {
                state.Supported = false;
                state.Area?.Abandon();
                if (state.ForegroundScopes == 0 && state.Override == null && ReferenceEquals(_foreground, state)) {
                    _foreground = null;
                }
            }
        }

        private static void ClearArea(RunspaceProgressState state) {
            if (state.Area == null) {
                return;
            }
            try {
                state.Area.Clear();
            } catch (Exception ex) when (ex is System.IO.IOException || ex is InvalidOperationException) {
                state.Area.Abandon();
            }
        }

        private sealed class ForegroundScope : IDisposable {
            private RunspaceProgressState? _state;
            internal ForegroundScope(RunspaceProgressState state) { _state = state; }
            public void Dispose() {
                lock (Gate) {
                    if (_state == null) { return; }
                    _state.ForegroundScopes--;
                    ReleaseIfIdle(_state);
                    _state = null;
                }
            }
        }
    }
}
