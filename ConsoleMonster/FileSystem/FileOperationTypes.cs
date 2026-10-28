using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleMonster.FileSystem {
    /// <summary>What a <see cref="FileOperation"/> does.</summary>
    public enum FileOperationKind {
        /// <summary>Copies files and folders.</summary>
        Copy,
        /// <summary>Moves or renames files and folders; a rename when the destination is on the same volume.</summary>
        Move,
        /// <summary>Moves items to the recycle bin or trash.</summary>
        Delete,
        /// <summary>Deletes items without a way back.</summary>
        DeletePermanently,
        /// <summary>Creates a folder, including missing parents.</summary>
        CreateFolder
    }

    /// <summary>Where a running operation is.</summary>
    public enum FileOperationPhase {
        /// <summary>Counting items and bytes so progress has totals.</summary>
        Scanning,
        /// <summary>Working through the items.</summary>
        Running,
        /// <summary>Finished, cancelled or stopped; the final report.</summary>
        Completed
    }

    /// <summary>What happens when a copy or move meets an existing file.</summary>
    public enum FileConflictPolicy {
        /// <summary>Ask <see cref="FileOperationOptions.ResolveConflict"/> for each conflict; without a callback, skip.</summary>
        Ask,
        /// <summary>Keep the existing file.</summary>
        Skip,
        /// <summary>Replace the existing file only when the source was modified later.</summary>
        OverwriteOlder,
        /// <summary>Replace every existing file.</summary>
        Overwrite,
        /// <summary>Keep both: the new file gets a free name such as <c>report (2).csv</c>.</summary>
        Rename
    }

    /// <summary>The answer to one conflict.</summary>
    public enum FileConflictResolution {
        /// <summary>Replace this file.</summary>
        Overwrite,
        /// <summary>Keep the existing file.</summary>
        Skip,
        /// <summary>Replace this file and every later one without asking again.</summary>
        OverwriteAll,
        /// <summary>Keep this file and every later one without asking again.</summary>
        SkipAll,
        /// <summary>Keep both, writing the new file to <see cref="FileConflict.RenameTarget"/>.</summary>
        Rename,
        /// <summary>Stop the operation, as cancellation does.</summary>
        Cancel
    }

    /// <summary>Why one item failed.</summary>
    public enum FileOperationErrorKind {
        /// <summary>Permission was denied.</summary>
        AccessDenied,
        /// <summary>Another process holds the file open.</summary>
        InUse,
        /// <summary>The path is longer than the file system allows.</summary>
        PathTooLong,
        /// <summary>The item disappeared before it was processed.</summary>
        NotFound,
        /// <summary>The destination exists as something that can't be replaced, such as a folder in place of a file.</summary>
        AlreadyExists,
        /// <summary>A folder would be copied or moved into itself.</summary>
        IntoItself,
        /// <summary>There is no recycle bin or trash for the item; nothing was deleted.</summary>
        TrashUnavailable,
        /// <summary>The destination has no room left.</summary>
        DiskFull,
        /// <summary>Any other input/output failure.</summary>
        Other
    }

    /// <summary>Options for a <see cref="FileOperation"/>.</summary>
    public sealed class FileOperationOptions {
        private int _bufferSize = 1 << 20;
        private TimeSpan _progressInterval = TimeSpan.FromMilliseconds(100);

        /// <summary>What happens when a destination file exists. The default asks.</summary>
        public FileConflictPolicy ConflictPolicy { get; set; } = FileConflictPolicy.Ask;

        /// <summary>Whether copies keep the source's modification and access times (and creation time where the platform has one). On by default.</summary>
        public bool PreserveTimestamps { get; set; } = true;

        /// <summary>
        /// Answers a conflict under <see cref="FileConflictPolicy.Ask"/>. It runs on the operation's worker, so a UI
        /// posts the question to its own thread and completes the task with the answer.
        /// </summary>
        public Func<FileConflict, CancellationToken, Task<FileConflictResolution>>? ResolveConflict { get; set; }

        /// <summary>Receives progress snapshots from the worker: at most once per <see cref="ProgressInterval"/>, plus each phase change.</summary>
        public IProgress<FileOperationProgress>? Progress { get; set; }

        /// <summary>The trash for delete operations; <see cref="FileTrash.Current"/> when null.</summary>
        public ITrash? Trash { get; set; }

        /// <summary>The shortest time between progress reports. The default is 100 ms.</summary>
        public TimeSpan ProgressInterval {
            get => _progressInterval;
            set => _progressInterval = value < TimeSpan.Zero ? throw new ArgumentOutOfRangeException(nameof(value), "The interval can't be negative.") : value;
        }

        /// <summary>The copy buffer in bytes. The default is 1 MiB.</summary>
        public int BufferSize {
            get => _bufferSize;
            set => _bufferSize = value < 4096 ? throw new ArgumentOutOfRangeException(nameof(value), "The buffer must be at least 4096 bytes.") : value;
        }

        internal FileOperationOptions Clone() => (FileOperationOptions)MemberwiseClone();
    }

    /// <summary>An existing destination file met by a copy or move, with what's needed to decide.</summary>
    public sealed class FileConflict {
        internal FileConflict(string source, string target, long sourceSize, DateTime sourceModified, long targetSize, DateTime targetModified, string renameTarget) {
            Source = source;
            Target = target;
            SourceSize = sourceSize;
            SourceModified = sourceModified;
            TargetSize = targetSize;
            TargetModified = targetModified;
            RenameTarget = renameTarget;
        }

        /// <summary>The file being copied or moved.</summary>
        public string Source { get; }

        /// <summary>The existing file.</summary>
        public string Target { get; }

        /// <summary>The source size in bytes.</summary>
        public long SourceSize { get; }

        /// <summary>When the source was last modified, in UTC.</summary>
        public DateTime SourceModified { get; }

        /// <summary>The existing file's size in bytes.</summary>
        public long TargetSize { get; }

        /// <summary>When the existing file was last modified, in UTC.</summary>
        public DateTime TargetModified { get; }

        /// <summary>Whether the source was modified after the existing file.</summary>
        public bool SourceIsNewer => SourceModified > TargetModified;

        /// <summary>Whether source and destination are the same file, which can only be kept or copied under a new name.</summary>
        public bool IsSameFile => FileOperation.SamePath(Source, Target);

        /// <summary>
        /// The path written by <see cref="FileConflictResolution.Rename"/>: a free name next to the target, such as
        /// <c>report (2).csv</c>. A UI may replace it with the name the user typed; a name without a folder stays next to the target.
        /// </summary>
        public string RenameTarget { get; set; }
    }

    /// <summary>A snapshot of a running operation's progress.</summary>
    public sealed class FileOperationProgress {
        internal FileOperationProgress(FileOperationKind kind, FileOperationPhase phase, string? currentItem, int itemsDone, int itemsTotal, long bytesDone, long bytesTotal, long currentItemBytesDone, long currentItemBytes, double bytesPerSecond, TimeSpan elapsed, TimeSpan? remaining) {
            Kind = kind;
            Phase = phase;
            CurrentItem = currentItem;
            ItemsDone = itemsDone;
            ItemsTotal = itemsTotal;
            BytesDone = bytesDone;
            BytesTotal = bytesTotal;
            CurrentItemBytesDone = currentItemBytesDone;
            CurrentItemBytes = currentItemBytes;
            BytesPerSecond = bytesPerSecond;
            Elapsed = elapsed;
            Remaining = remaining;
        }

        /// <summary>The operation.</summary>
        public FileOperationKind Kind { get; }

        /// <summary>Scanning, running or completed.</summary>
        public FileOperationPhase Phase { get; }

        /// <summary>The path being processed, or null between items.</summary>
        public string? CurrentItem { get; }

        /// <summary>Items finished, skipped or failed so far.</summary>
        public int ItemsDone { get; }

        /// <summary>Items found by the scan; it grows while scanning.</summary>
        public int ItemsTotal { get; }

        /// <summary>Bytes copied, or accounted for by skipped and renamed items.</summary>
        public long BytesDone { get; }

        /// <summary>Bytes found by the scan; zero for operations that don't copy data.</summary>
        public long BytesTotal { get; }

        /// <summary>Bytes copied of the current item.</summary>
        public long CurrentItemBytesDone { get; }

        /// <summary>The current item's size.</summary>
        public long CurrentItemBytes { get; }

        /// <summary>Recent throughput, smoothed over the last few seconds.</summary>
        public double BytesPerSecond { get; }

        /// <summary>Time since the operation started.</summary>
        public TimeSpan Elapsed { get; }

        /// <summary>Estimated time left, or null until there's enough to estimate from.</summary>
        public TimeSpan? Remaining { get; }

        /// <summary>Overall completion from 0 to 100: by bytes when there are bytes to copy, otherwise by items.</summary>
        public double Percent => Phase == FileOperationPhase.Completed ? 100
            : BytesTotal > 0 ? Math.Min(100, BytesDone * 100.0 / BytesTotal)
            : ItemsTotal > 0 ? Math.Min(100, ItemsDone * 100.0 / ItemsTotal) : 0;
    }

    /// <summary>One item that failed; the operation continued with the next.</summary>
    public sealed class FileOperationError {
        internal FileOperationError(string source, string? target, FileOperationErrorKind kind, string message, Exception? exception) {
            Source = source;
            Target = target;
            Kind = kind;
            Message = message;
            Exception = exception;
        }

        /// <summary>The item that failed.</summary>
        public string Source { get; }

        /// <summary>Where it was going, for copies and moves.</summary>
        public string? Target { get; }

        /// <summary>The failure category.</summary>
        public FileOperationErrorKind Kind { get; }

        /// <summary>A sentence for the summary.</summary>
        public string Message { get; }

        /// <summary>The underlying exception, when there was one.</summary>
        public Exception? Exception { get; }

        /// <inheritdoc />
        public override string ToString() => Source + ": " + Message;
    }

    /// <summary>The summary of a finished operation. Failed items can be retried; leaving them is the skip.</summary>
    public sealed class FileOperationResult {
        private readonly FileOperation _operation;

        internal FileOperationResult(FileOperation operation, int completed, int skipped, int notProcessed, long bytes, bool canceled, TimeSpan elapsed, IReadOnlyList<FileOperationError> errors) {
            _operation = operation;
            Completed = completed;
            Skipped = skipped;
            NotProcessed = notProcessed;
            BytesTransferred = bytes;
            Canceled = canceled;
            Elapsed = elapsed;
            Errors = errors;
        }

        /// <summary>The operation.</summary>
        public FileOperationKind Kind => _operation.Kind;

        /// <summary>Items done.</summary>
        public int Completed { get; }

        /// <summary>Items skipped by a conflict answer or policy.</summary>
        public int Skipped { get; }

        /// <summary>Items never reached because the operation was cancelled. A folder not yet scanned counts as one item.</summary>
        public int NotProcessed { get; }

        /// <summary>Bytes written.</summary>
        public long BytesTransferred { get; }

        /// <summary>Whether cancellation (or a Cancel answer) stopped the operation after the item in progress.</summary>
        public bool Canceled { get; }

        /// <summary>How long it ran.</summary>
        public TimeSpan Elapsed { get; }

        /// <summary>The items that failed, in the order they failed.</summary>
        public IReadOnlyList<FileOperationError> Errors { get; }

        /// <summary>Whether every item completed or was deliberately skipped.</summary>
        public bool Succeeded => !Canceled && Errors.Count == 0;

        /// <summary>An operation that tries the failed items again with the same options, or null when nothing failed.</summary>
        /// <returns>The retry, which runs only when started.</returns>
        public FileOperation? RetryFailed() => Errors.Count == 0 ? null : _operation.ForItems(Errors);

        /// <inheritdoc />
        public override string ToString() {
            string text = Completed + " done";
            if (Skipped > 0) { text += ", " + Skipped + " skipped"; }
            if (Errors.Count > 0) { text += ", " + Errors.Count + " failed"; }
            if (Canceled) { text += ", cancelled" + (NotProcessed > 0 ? " with " + NotProcessed + " left" : string.Empty); }
            return text;
        }
    }
}
