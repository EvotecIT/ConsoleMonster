using System;
using System.Collections.Generic;
using System.Linq;

namespace ConsoleMonster.Live {
    /// <summary>A point-in-time snapshot of a live session's counters and errors (<see cref="LiveSession.GetCounters"/>).</summary>
    public sealed class LiveSessionCounters {
        /// <summary>Creates a snapshot.</summary>
        /// <param name="eventsPosted">Events accepted.</param>
        /// <param name="eventsApplied">Events applied.</param>
        /// <param name="eventsDropped">Events ignored (unknown target, or posted after admission closed).</param>
        /// <param name="applyErrors">Target Apply calls that threw.</param>
        /// <param name="renderErrors">Frames replaced by the one-line error note.</param>
        /// <param name="framesRendered">Frames rendered.</param>
        /// <param name="resizes">Size changes redrawn.</param>
        /// <param name="unknownTargets">Target names that received events but do not exist, with event counts.</param>
        /// <param name="lastError">The most recent error, or null.</param>
        /// <param name="failure">The error that ended the session early, or null.</param>
        public LiveSessionCounters(long eventsPosted, long eventsApplied, long eventsDropped, long applyErrors, long renderErrors,
            long framesRendered, long resizes, IReadOnlyDictionary<string, long> unknownTargets, Exception? lastError, Exception? failure) {
            EventsPosted = eventsPosted;
            EventsApplied = eventsApplied;
            EventsDropped = eventsDropped;
            ApplyErrors = applyErrors;
            RenderErrors = renderErrors;
            FramesRendered = framesRendered;
            Resizes = resizes;
            UnknownTargets = unknownTargets ?? throw new ArgumentNullException(nameof(unknownTargets));
            LastError = lastError;
            Failure = failure;
        }

        /// <summary>Events accepted before shutdown closed admission.</summary>
        public long EventsPosted { get; }

        /// <summary>Events applied successfully.</summary>
        public long EventsApplied { get; }

        /// <summary>Events ignored: sent to an unknown target, or posted after admission closed.</summary>
        public long EventsDropped { get; }

        /// <summary>Target Apply calls that threw.</summary>
        public long ApplyErrors { get; }

        /// <summary>Frames where the layout threw and a one-line note was shown instead.</summary>
        public long RenderErrors { get; }

        /// <summary>Frames rendered, including the final frame.</summary>
        public long FramesRendered { get; }

        /// <summary>Size changes the session redrew for.</summary>
        public long Resizes { get; }

        /// <summary>Target names that received events but are not registered (typos such as <c>lanez</c>), with event counts.</summary>
        public IReadOnlyDictionary<string, long> UnknownTargets { get; }

        /// <summary>Events sent to unknown targets.</summary>
        public long UnknownTargetEvents => UnknownTargets.Values.Sum();

        /// <summary>The most recent target, rendering, recording or cleanup error, or null.</summary>
        public Exception? LastError { get; }

        /// <summary>The error that ended the session early, or null.</summary>
        public Exception? Failure { get; }

        /// <summary>Whether anything went wrong: failed updates, failed frames, unknown targets or an early end.</summary>
        public bool HasProblems => ApplyErrors > 0 || RenderErrors > 0 || UnknownTargets.Count > 0 || Failure != null;
    }
}
