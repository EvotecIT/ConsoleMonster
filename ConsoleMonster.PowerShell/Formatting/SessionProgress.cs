using System;
using System.Collections.Generic;
using System.Diagnostics;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Live;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// The session's progress area for Enable-ConsoleFormatting -Feature Progress: Write-Progress calls (through the
    /// Write-ConsoleProgress alias) update an activity list drawn below the cursor with an <see cref="InlineArea"/>. Output
    /// reaching Out-Default removes the area first, other host output (warnings, Write-Host) is found and stepped over at
    /// the next draw, and the area is removed when the command typed at the prompt ends or every activity completes.
    /// While Use-ConsoleProgress runs, its progress area takes the calls; while another live view draws (Watch-Console,
    /// ForEach-ConsoleItem, Invoke-ConsolePester, a dashboard), the calls are dropped so nothing draws over it.
    /// Pipeline thread only.
    /// </summary>
    internal static class SessionProgress {
        private static readonly object Gate = new object();
        private static readonly Stopwatch Clock = new Stopwatch();
        private static readonly Dictionary<int, string> DrawnText = new Dictionary<int, string>();
        private static ActivityList? _activities;
        private static InlineArea? _area;
        private static long _lastDraw = long.MinValue;
        private static bool? _supported;

        /// <summary>The fewest milliseconds between two draws that only move a bar; new text is drawn at once.</summary>
        private const long DrawInterval = 66;

        /// <summary>Whether the Progress feature is on.</summary>
        internal static bool Enabled { get; set; }

        /// <summary>The progress area of a running Use-ConsoleProgress, which takes the calls while it runs; or null.</summary>
        internal static ProgressBridge? Override { get; set; }

        /// <summary>One Write-Progress call. False when the area cannot be drawn here (the caller writes PowerShell's progress).</summary>
        internal static bool Report(int id, int parentId, string? activity, string? status, int percent, int secondsRemaining, bool completed) {
            lock (Gate) {
                ProgressBridge? bridge = Override;
                if (bridge != null) {
                    bridge.Progress(id, parentId, activity, status, percent, secondsRemaining, completed);
                    return true;
                }
                if (!Enabled) {
                    return false;
                }
                if (LiveSession.Current != null) {
                    // Another live view owns the screen: drawing (or PowerShell's bar) would land on top of it.
                    return true;
                }
                _supported ??= InlineArea.IsSupported();
                if (!_supported.Value) {
                    return false;
                }
                if (!Clock.IsRunning) {
                    Clock.Start();
                }
                _activities ??= new ActivityList();
                int before = _activities.Count;
                _activities.Apply(ProgressEvents.From(id, parentId, activity, status, percent, secondsRemaining, completed, Math.Max(1, Clock.ElapsedMilliseconds)));
                if (_activities.Count == 0) {
                    // Every activity completed: the area goes, as PowerShell's progress does.
                    ClearArea();
                    DrawnText.Clear();
                    return true;
                }
                string text = (activity ?? string.Empty) + "\u0001" + (status ?? string.Empty);
                bool newText = completed || !DrawnText.TryGetValue(id, out string? drawn) || !string.Equals(drawn, text, StringComparison.Ordinal);
                long now = Clock.ElapsedMilliseconds;
                if (_activities.Count == before && !newText && now - _lastDraw < DrawInterval) {
                    return true;
                }
                if (completed) {
                    DrawnText.Remove(id);
                } else {
                    DrawnText[id] = text;
                }
                _lastDraw = now;
                Draw();
                return true;
            }
        }

        /// <summary>Removes the area before other output is written; the activities stay for the next draw.</summary>
        internal static void Suspend() {
            lock (Gate) {
                if (_area != null && _area.Height > 0) {
                    ClearArea();
                }
            }
        }

        /// <summary>The command typed at the prompt ended (or a new one starts): the area goes and the activities are forgotten.</summary>
        internal static void EndPipeline() {
            lock (Gate) {
                ClearArea();
                _area = null;
                _activities = null;
                _supported = null;
                _lastDraw = long.MinValue;
                DrawnText.Clear();
                Clock.Reset();
            }
        }

        private static void Draw() {
            if (_activities == null) {
                return;
            }
            try {
                TerminalProfile profile = Terminal.GetCurrentProfile();
                RenderContext context = Terminal.CreateContext(profile).WithClock(0, Clock.Elapsed);
                _area ??= new InlineArea(profile) { MaxWidth = 100 };
                _area.Draw(_activities, context);
            } catch (Exception ex) when (ex is System.IO.IOException || ex is InvalidOperationException) {
                // A console that went away (closed window, redirected mid-run): stop drawing for this command.
                _supported = false;
                _area?.Abandon();
            }
        }

        private static void ClearArea() {
            if (_area == null) {
                return;
            }
            try {
                _area.Clear();
            } catch (Exception ex) when (ex is System.IO.IOException || ex is InvalidOperationException) {
                _area.Abandon();
            }
        }
    }
}
