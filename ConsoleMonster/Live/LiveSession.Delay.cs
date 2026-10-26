using System;
using System.Threading;
using System.Threading.Tasks;
using ConsoleMonster.Canvas;
using ConsoleMonster.Output;
using ConsoleMonster.Rendering;

namespace ConsoleMonster.Live {
    public sealed partial class LiveSession {
        // Resolve once at Start: a producer may run in a different PowerShell runspace or target scope.
        private RecordingTarget? _recordingTarget;
        private bool _delayClock;
        private TimeSpan _checkpointHeartbeat;
        private long _playbackTicks;

        /// <summary>
        /// Pauses work cancellably. With <see cref="RecordingTarget.SkipDelays"/>, first applies earlier queued updates
        /// and captures selected compact dashboard frames on the dispatcher instead of waiting. Selection and call
        /// counts belong to the recording target; checkpoint order assumes one producer. Checkpoints use a fixed animation
        /// tick and the elapsed time before the delay. Unless an explicit session clock is supplied, skipped recording
        /// delays advance the session's logical elapsed time by the requested interval after each checkpoint.
        /// Setting CONSOLEMONSTER_SNAPSHOT to 1, true or yes (case-insensitive) also
        /// skips the wait, without capturing a frame unless the session's recording target has SkipDelays enabled.
        /// </summary>
        /// <param name="delay">A nonnegative finite interval, no greater than Int32.MaxValue milliseconds.</param>
        /// <exception cref="ArgumentOutOfRangeException">The interval is outside the WaitHandle timeout range.</exception>
        /// <exception cref="InvalidOperationException">The session is not running, or the dispatcher calls this method.</exception>
        /// <exception cref="OperationCanceledException">The session was cancelled or stopped.</exception>
        public void Delay(TimeSpan delay) => DelayCore(delay, Cancellation);

        // Replay and watch producers supply their linked cancellation while sharing the same checkpoint admission.
        internal void DelayCore(TimeSpan delay, CancellationToken cancellation) {
            if (delay < TimeSpan.Zero || delay.TotalMilliseconds > int.MaxValue) {
                throw new ArgumentOutOfRangeException(nameof(delay));
            }
            if (Thread.CurrentThread == _thread) {
                throw new InvalidOperationException("A render callback cannot wait for the render thread.");
            }
            EnsureDelayAvailable();

            bool selected = _recordingTarget?.SelectCheckpoint() == true;
            if (_recordingTarget?.SkipDelays == true) {
                if (!selected) {
                    AdvanceDelayClock(delay);
                    return;
                }
                TaskCompletionSource<bool> checkpoint = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                if (!TryPost(new LiveEvent("$checkpoint", "capture") { Payload = checkpoint })) {
                    EnsureDelayAvailable();
                    throw new InvalidOperationException("The live session has stopped accepting updates.");
                }
                checkpoint.Task.Wait(cancellation);
                cancellation.ThrowIfCancellationRequested();
                if (!checkpoint.Task.Result) {
                    throw new InvalidOperationException("The dashboard checkpoint could not be captured.", Failure);
                }
                AdvanceDelayClock(delay);
                return;
            }

            string? snapshot = Environment.GetEnvironmentVariable("CONSOLEMONSTER_SNAPSHOT");
            bool skip = snapshot == "1" || string.Equals(snapshot, "true", StringComparison.OrdinalIgnoreCase)
                || string.Equals(snapshot, "yes", StringComparison.OrdinalIgnoreCase);
            if (!skip) { cancellation.WaitHandle.WaitOne(delay); }
            cancellation.ThrowIfCancellationRequested();
        }

        // Stop closes admission before signalling its token. Synchronize with that transition, then
        // complete cancellation outside the lifecycle lock so cancellation callbacks can safely reenter.
        private void EnsureDelayAvailable() {
            bool stopping;
            lock (_lifecycle) {
                Cancellation.ThrowIfCancellationRequested();
                if (_state == 0) { throw new InvalidOperationException("The live session is not running."); }
                stopping = _state >= 2;
            }
            if (stopping) {
                CancelWork();
                Cancellation.ThrowIfCancellationRequested();
            }
        }

        private void AdvanceDelayClock(TimeSpan delay) {
            if (_delayClock) { Interlocked.Add(ref _playbackTicks, delay.Ticks); }
        }

        // Both the queued widget changes and their render belong to the dispatcher, never the producer.
        private void ApplyCheckpoint(TaskCompletionSource<bool> checkpoint) {
            try {
                if (Cancellation.IsCancellationRequested) {
                    Interlocked.Increment(ref _eventsApplied);
                    checkpoint.TrySetResult(false);
                    return;
                }
                lock (_outputGate) {
                    FlushUnrouted();
                    RecordingTarget target = _recordingTarget!;
                    if (target.CaptureViewport && !_output!.SupportsLiveFrames) {
                        if (_options.StreamHeartbeat > TimeSpan.Zero && Elapsed - _checkpointHeartbeat >= _options.StreamHeartbeat) {
                            Heartbeat();
                            _checkpointHeartbeat = Elapsed;
                        }
                    } else {
                        RenderContext context = _context!.WithClock(target.CaptureViewport ? target.CheckpointCount - 1 : 0, Elapsed);
                        _frame = target.CaptureViewport
                            ? FrameComposer.Compose(RootForFrame(context.Profile.Height), context.Profile.Width, context.Profile.Height, context)
                            : FrameComposer.RenderBlock(_root, context.Profile.Width, context);
                        target.CaptureCheckpoint(_frame);
                    }
                }
                Interlocked.Increment(ref _eventsApplied);
                checkpoint.TrySetResult(true);
            } catch (Exception ex) {
                Fail(ex);
                checkpoint.TrySetResult(false);
                throw;
            }
        }
    }
}
