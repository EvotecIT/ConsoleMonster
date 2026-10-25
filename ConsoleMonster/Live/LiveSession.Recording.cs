using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using ConsoleMonster.Canvas;
using ConsoleMonster.Layout;
using ConsoleMonster.Rendering;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Live {
    public sealed partial class LiveSession {
        /// <summary>The reserved target of marker events (<see cref="Mark"/>): recorded for replay, shown nowhere.</summary>
        public const string MarkerTarget = "$marker";

        private const long SnapshotInterval = 200; // Milliseconds between snapshots of a region whose live content changes.
        private const int MaxSnapshotHeight = 200;

        // Render-thread state: the region (RegionWidget) that shows each live widget adopted from swapped content, and the regions
        // whose content changed since their last recorded snapshot.
        private readonly Dictionary<string, RegionWidget> _slotMembers = new Dictionary<string, RegionWidget>(StringComparer.Ordinal);
        private readonly Dictionary<RegionWidget, long> _pendingSnapshots = new Dictionary<RegionWidget, long>();
        private readonly Dictionary<RegionWidget, long> _lastSnapshot = new Dictionary<RegionWidget, long>();

        /// <summary>
        /// Adds a marker to the recording (a named point such as "Deploy started"), which <c>Show-ConsoleReplay</c> can jump
        /// to. Markers change nothing on screen; without a recording they are ignored.
        /// </summary>
        /// <param name="label">The marker text.</param>
        public void Mark(string label) {
            if (label == null) { throw new ArgumentNullException(nameof(label)); }
            Post(new LiveEvent(MarkerTarget, "mark", text: label));
        }

        /// <summary>
        /// Until the returned scope is disposed, what the calling thread writes to <see cref="Console.Out"/> and
        /// <see cref="Console.Error"/> goes to the original streams instead of a live session's log. A host that hands a
        /// session's own output lines on through <c>Console.Out</c> (as PowerShell's console host does with redirected
        /// output) writes them inside this scope, so they are not captured, drawn and written again.
        /// </summary>
        /// <returns>The scope; dispose it on the same thread.</returns>
        public static IDisposable SuspendCapture() {
            CaptureWriter.EnterPassThrough();
            return new CaptureSuspension();
        }

        private sealed class CaptureSuspension : IDisposable {
            private bool _disposed;

            public void Dispose() {
                if (_disposed) { return; }
                _disposed = true;
                CaptureWriter.ExitPassThrough();
            }
        }

        /// <summary>The automatic recording file of this session, or null.</summary>
        public string? RecordingPath { get; private set; }

        private bool RecordingRequired => _options.RecordWriter != null || _options.RecordPath != null;

        private void Record(LiveEvent liveEvent) {
            LiveRecorder? recorder = _recorder;
            if (recorder == null) { return; }
            Attempt(() => recorder.Write(liveEvent));
            if (RecordingRequired && recorder.WriteFailure is Exception failure) { Fail(failure); }
        }

        // Records an applied event. Region swaps carry an in-process widget, which a recording cannot hold: the new content
        // is recorded as a picture (markup) instead, and so are later changes of live widgets inside it.
        private void RecordApplied(ILiveTarget target, LiveEvent liveEvent) {
            if (target is RegionWidget slot && (liveEvent.Operation == "set" || liveEvent.Operation == "clear")) {
                TrackMembers(slot);
                if (liveEvent.Operation == "set" && liveEvent.Payload is IWidget) {
                    RecordSnapshot(slot, liveEvent.Time);
                    MarkOwnerChanged(slot.WidgetId, liveEvent.Time);
                    return;
                }
            }
            Record(liveEvent);
            MarkOwnerChanged(target.WidgetId, liveEvent.Time);
        }

        private void MarkOwnerChanged(string name, long time) {
            if (_slotMembers.TryGetValue(name, out RegionWidget? owner) && !_pendingSnapshots.ContainsKey(owner)) {
                _pendingSnapshots[owner] = time;
            }
        }

        private void TrackMembers(RegionWidget slot) {
            List<string> stale = new List<string>();
            foreach (KeyValuePair<string, RegionWidget> member in _slotMembers) {
                if (ReferenceEquals(member.Value, slot)) {
                    stale.Add(member.Key);
                }
            }
            foreach (string name in stale) {
                _slotMembers.Remove(name);
            }
            AddMembers(slot.Content, slot);
        }

        private void AddMembers(IWidget widget, RegionWidget owner) {
            if (widget is ILiveTarget target && !string.IsNullOrEmpty(target.WidgetId)) {
                _slotMembers[target.WidgetId] = owner;
            }
            if (widget is IContainer container) {
                foreach (IWidget child in container.Children) {
                    AddMembers(child, owner);
                }
            }
        }

        // Records due snapshots of regions whose live content changed: at most every SnapshotInterval per region, and all of
        // them when the session stops.
        private void RecordSlotSnapshots(bool force) {
            if (_recorder == null || _pendingSnapshots.Count == 0) {
                return;
            }
            long now = (long)Elapsed.TotalMilliseconds;
            List<RegionWidget> due = new List<RegionWidget>();
            foreach (KeyValuePair<RegionWidget, long> pending in _pendingSnapshots) {
                if (force || !_lastSnapshot.TryGetValue(pending.Key, out long last) || now - last >= SnapshotInterval) {
                    due.Add(pending.Key);
                }
            }
            foreach (RegionWidget slot in due) {
                _pendingSnapshots.Remove(slot);
                RecordSnapshot(slot, Math.Max(now, 1));
                MarkOwnerChanged(slot.WidgetId, now);
            }
        }

        private void RecordSnapshot(RegionWidget slot, long time) {
            _lastSnapshot[slot] = time;
            _pendingSnapshots.Remove(slot);
            if (_context == null) {
                return;
            }
            Attempt(() => {
                string markup = Snapshot(slot.Content, slot.RenderedWidth > 0 ? slot.RenderedWidth : _context.Profile.Width, slot.RenderedHeight, _context.WithClock(FramesRendered, Elapsed));
                Record(new LiveEvent(slot.WidgetId, "set", text: markup, status: "snapshot", time: time, isMarkup: true));
            });
        }

        internal static string Snapshot(IWidget content, int width, int height, RenderContext context) {
            width = Math.Max(1, width);
            if (height <= 0) {
                height = content.Measure(new Constraints(width, MaxSnapshotHeight), context).Height;
            }
            height = Math.Max(1, Math.Min(MaxSnapshotHeight, height));
            CellBuffer buffer = new CellBuffer(width, height);
            content.Measure(new Constraints(width, height), context);
            content.Render(new Region(buffer), context);
            return CellMarkup.From(buffer);
        }

        // Opens a new automatic recording and removes the oldest ones beyond the count and size limits. A failure only
        // turns recording off: automatic recording must never stop a run.
        private LiveRecorder? OpenAutoRecording() {
            try {
                string folder = _options.AutoRecordFolder ?? LiveSessionOptions.DefaultRecordingFolder;
                Directory.CreateDirectory(folder);
                string name = SafeName(_options.RecordingName);
                string stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
                string path = Path.Combine(folder, name + "-" + stamp + ".jsonl");
                for (int i = 2; File.Exists(path); i++) {
                    path = Path.Combine(folder, name + "-" + stamp + "-" + i.ToString(CultureInfo.InvariantCulture) + ".jsonl");
                }
                PruneRecordings(folder, name, _options.AutoRecord - 1, _options.AutoRecordMaxBytes);
                StreamWriter writer = new StreamWriter(path, false, new UTF8Encoding(false));
                try {
                    LiveRecorder recorder = new LiveRecorder(writer, false) { Limit = _options.AutoRecordMaxBytes };
                    RecordingPath = path;
                    return recorder;
                } catch {
                    writer.Dispose();
                    throw;
                }
            } catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException || ex is NotSupportedException || ex is System.Security.SecurityException) {
                Error(ex);
                return null;
            }
        }

        /// <summary>
        /// Keeps the newest <paramref name="keep"/> automatic recordings named <paramref name="name"/> in a folder, within
        /// <paramref name="maxBytes"/> together. Other files, including recordings with other names, are never touched.
        /// </summary>
        internal static void PruneRecordings(string folder, string name, int keep, long maxBytes) {
            List<FileInfo> files = new List<FileInfo>();
            foreach (FileInfo file in new DirectoryInfo(folder).GetFiles(name + "-*.jsonl")) {
                if (IsAutoRecording(file.Name, name)) {
                    files.Add(file);
                }
            }
            files.Sort((a, b) => b.LastWriteTimeUtc.CompareTo(a.LastWriteTimeUtc));
            long total = 0;
            for (int i = 0; i < files.Count; i++) {
                total += files[i].Length;
                if (i >= Math.Max(0, keep) || total > maxBytes) {
                    try {
                        files[i].Delete();
                    } catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException) {
                        Debug.WriteLine(ex.Message); // A recording still open elsewhere stays.
                    }
                }
            }
        }

        // name-yyyyMMdd-HHmmss.jsonl, optionally with -N before the extension.
        private static bool IsAutoRecording(string file, string name) {
            string rest = Path.GetFileNameWithoutExtension(file).Substring(name.Length + 1);
            return System.Text.RegularExpressions.Regex.IsMatch(rest, "^[0-9]{8}-[0-9]{6}(-[0-9]+)?$");
        }

        private static string SafeName(string? name) {
            if (string.IsNullOrWhiteSpace(name)) {
                return "session";
            }
            StringBuilder safe = new StringBuilder();
            foreach (char c in name!.Trim()) {
                safe.Append(Array.IndexOf(Path.GetInvalidFileNameChars(), c) >= 0 || char.IsWhiteSpace(c) ? '-' : c);
            }
            string text = safe.ToString();
            return text.Length > 60 ? text.Substring(0, 60) : text;
        }
    }
}
