using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleMonster.FileSystem {
    /// <summary>
    /// A planned copy, move, delete or new folder that runs in the background with progress, conflict answers and a
    /// summary of the items that failed. Create one with <see cref="Copy"/>, <see cref="Move"/>, <see cref="Delete"/>,
    /// <see cref="DeletePermanently"/> or <see cref="CreateFolder"/>, then <see cref="RunAsync"/> it; nothing touches the
    /// disk before that.
    /// </summary>
    /// <remarks>
    /// <para>One failing item never stops the rest: access denied, a file in use or a path that's too long becomes a
    /// <see cref="FileOperationError"/> and the operation continues. <see cref="FileOperationResult.RetryFailed"/> tries
    /// those again.</para>
    /// <para>Cancellation stops after the file in progress, so no file is left half-written. Copies are written to a
    /// temporary name next to the destination and renamed into place when complete, so an existing file is only
    /// replaced by a whole one.</para>
    /// </remarks>
    public sealed partial class FileOperation {
        private static readonly bool Windows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        // .NET Framework keeps the MAX_PATH limit; .NET 8 and later lift it on Windows.
        private static readonly bool LegacyPaths = RuntimeInformation.FrameworkDescription.StartsWith(".NET Framework", StringComparison.Ordinal);
        private static readonly StringComparison PathComparison = Windows || RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        private readonly Item[] _items;

        private FileOperation(FileOperationKind kind, IEnumerable<Item> items, string? destination, FileOperationOptions? options) {
            Kind = kind;
            _items = items.ToArray();
            Destination = destination;
            Options = options ?? new FileOperationOptions();
            Sources = Array.AsReadOnly(_items.Select(item => item.Source).ToArray());
        }

        /// <summary>The operation.</summary>
        public FileOperationKind Kind { get; }

        /// <summary>The items it works on, as full paths.</summary>
        public IReadOnlyList<string> Sources { get; }

        /// <summary>The destination as given: a folder, or the new path of a single copied or moved item.</summary>
        public string? Destination { get; }

        /// <summary>The options, read when the operation runs.</summary>
        public FileOperationOptions Options { get; }

        /// <summary>The full destination path of each source, in order; null for deletes.</summary>
        public IReadOnlyList<string?> Targets => Array.AsReadOnly(_items.Select(item => item.Target).ToArray());

        /// <summary>
        /// Copies files and folders. With several sources, or an existing folder as <paramref name="destination"/>, the
        /// items keep their names inside it; a single source with any other destination is copied to that path, so
        /// <c>report.csv</c> to <c>backup.csv</c> copies under a new name. A relative destination is relative to the
        /// source's folder.
        /// </summary>
        /// <param name="sources">Files and folders to copy.</param>
        /// <param name="destination">The destination folder, or the new path for a single source.</param>
        /// <param name="options">Conflicts, timestamps, progress; defaults ask about conflicts and keep timestamps.</param>
        /// <returns>The operation, not yet started.</returns>
        public static FileOperation Copy(IEnumerable<string> sources, string destination, FileOperationOptions? options = null) => Transfer(FileOperationKind.Copy, sources, destination, options);

        /// <summary>
        /// Moves or renames files and folders, with the destination rules of <see cref="Copy"/>; <c>Move(new[] { "a.txt" }, "b.txt")</c>
        /// renames in place. Within one volume it's a rename; across volumes each file is copied and then removed.
        /// </summary>
        /// <param name="sources">Files and folders to move.</param>
        /// <param name="destination">The destination folder, or the new path for a single source.</param>
        /// <param name="options">Conflicts, timestamps, progress.</param>
        /// <returns>The operation, not yet started.</returns>
        public static FileOperation Move(IEnumerable<string> sources, string destination, FileOperationOptions? options = null) => Transfer(FileOperationKind.Move, sources, destination, options);

        /// <summary>
        /// Moves items to the recycle bin or trash (<see cref="FileOperationOptions.Trash"/>, or the platform's own). Where
        /// there's no trash for an item it fails with <see cref="FileOperationErrorKind.TrashUnavailable"/> and stays in place;
        /// it is never deleted permanently instead.
        /// </summary>
        /// <param name="paths">Files and folders to delete.</param>
        /// <param name="options">Trash and progress.</param>
        /// <returns>The operation, not yet started.</returns>
        public static FileOperation Delete(IEnumerable<string> paths, FileOperationOptions? options = null) => Removal(FileOperationKind.Delete, paths, options);

        /// <summary>Deletes files and folders permanently, including read-only files. Symbolic links are removed, never followed.</summary>
        /// <param name="paths">Files and folders to delete.</param>
        /// <param name="options">Progress.</param>
        /// <returns>The operation, not yet started.</returns>
        public static FileOperation DeletePermanently(IEnumerable<string> paths, FileOperationOptions? options = null) => Removal(FileOperationKind.DeletePermanently, paths, options);

        /// <summary>Creates a folder and any missing parents. An existing file or folder at the path is an error.</summary>
        /// <param name="path">The new folder.</param>
        /// <param name="options">Progress.</param>
        /// <returns>The operation, not yet started.</returns>
        public static FileOperation CreateFolder(string path, FileOperationOptions? options = null) {
            if (string.IsNullOrWhiteSpace(path)) { throw new ArgumentException("A folder path is required.", nameof(path)); }
            string full = Full(path);
            return new FileOperation(FileOperationKind.CreateFolder, new[] { new Item(full, full) }, path, options);
        }

        /// <summary>Runs the operation on a worker. It completes with a summary rather than throwing for item failures or cancellation.</summary>
        /// <param name="cancellationToken">Stops the operation after the item in progress.</param>
        /// <returns>The summary.</returns>
        public Task<FileOperationResult> RunAsync(CancellationToken cancellationToken = default) => Task.Run(() => new Run(this, cancellationToken).ExecuteAsync());

        internal FileOperation ForItems(IEnumerable<FileOperationError> errors) {
            List<Item> items = new List<Item>();
            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (FileOperationError error in errors) {
                if (seen.Add(error.Source + "\n" + error.Target)) { items.Add(new Item(error.Source, error.Target)); }
            }
            return new FileOperation(Kind, items, Destination, Options);
        }

        private static FileOperation Transfer(FileOperationKind kind, IEnumerable<string> sources, string destination, FileOperationOptions? options) {
            if (sources == null) { throw new ArgumentNullException(nameof(sources)); }
            if (string.IsNullOrWhiteSpace(destination)) { throw new ArgumentException("A destination is required.", nameof(destination)); }
            List<string> list = sources.Where(source => !string.IsNullOrEmpty(source)).Select(Full).ToList();
            if (list.Count == 0) { throw new ArgumentException("At least one source is required.", nameof(sources)); }
            bool relative = !Path.IsPathRooted(destination);
            bool intoFolder = list.Count > 1 || EndsWithSeparator(destination);
            string folderBase = relative ? Path.GetDirectoryName(list[0]) ?? Directory.GetCurrentDirectory() : string.Empty;
            string full = Full(relative ? Path.Combine(folderBase, destination) : destination);
            intoFolder = intoFolder || Directory.Exists(full) && !SamePath(full, list[0]);
            IEnumerable<Item> items = list.Select(source => new Item(source, intoFolder ? Path.Combine(full, Path.GetFileName(source)) : full));
            return new FileOperation(kind, items, destination, options);
        }

        private static FileOperation Removal(FileOperationKind kind, IEnumerable<string> paths, FileOperationOptions? options) {
            if (paths == null) { throw new ArgumentNullException(nameof(paths)); }
            List<Item> items = paths.Where(path => !string.IsNullOrEmpty(path)).Select(path => new Item(Full(path), null)).ToList();
            if (items.Count == 0) { throw new ArgumentException("At least one path is required.", nameof(paths)); }
            return new FileOperation(kind, items, null, options);
        }

        private static bool EndsWithSeparator(string path) => path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal) || path.EndsWith(Path.AltDirectorySeparatorChar.ToString(), StringComparison.Ordinal);

        private static string Full(string path) {
            try {
                string full = Path.GetFullPath(path);
                string root = Path.GetPathRoot(full) ?? string.Empty;
                return full.Length > root.Length ? full.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) : full;
            } catch (PathTooLongException) {
                // .NET Framework refuses paths past MAX_PATH; keep the path so the item fails with PathTooLong when it runs.
                return path.Length > 3 ? path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) : path;
            }
        }

        internal static bool SamePath(string left, string right) => string.Equals(Full(left), Full(right), PathComparison);

        internal static bool IsInside(string path, string folder) {
            string root = Full(folder);
            string full = Full(path);
            return full.Length > root.Length && full.StartsWith(root, PathComparison) && (full[root.Length] == Path.DirectorySeparatorChar || full[root.Length] == Path.AltDirectorySeparatorChar || root.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal));
        }

        /// <summary>Whether a path is a symbolic link (or, on runtimes without link APIs, any reparse point).</summary>
        internal static bool IsLink(string path) {
            try {
                FileAttributes attributes = File.GetAttributes(path);
                if ((attributes & FileAttributes.ReparsePoint) == 0) { return false; }
#if NET8_0_OR_GREATER
                FileSystemInfo info = (attributes & FileAttributes.Directory) != 0 ? new DirectoryInfo(path) : new FileInfo(path);
                return info.LinkTarget != null;
#else
                return true;
#endif
            } catch (IOException) { return false; } catch (UnauthorizedAccessException) { return false; }
        }

        internal static FileOperationErrorKind Classify(Exception error, string? path = null) {
            if (Windows && error is IOException windowsError) {
                // ERROR_FILENAME_EXCED_RANGE, or "not found" / "invalid name" for a path the volume can't hold.
                int code = windowsError.HResult & 0xFFFF;
                if (code == 206 || (code == 2 || code == 3 || code == 123) && IsTooLong(path)) { return FileOperationErrorKind.PathTooLong; }
            }
            switch (error) {
                case UnauthorizedAccessException _: return FileOperationErrorKind.AccessDenied;
                case System.Security.SecurityException _: return FileOperationErrorKind.AccessDenied;
                case PathTooLongException _: return FileOperationErrorKind.PathTooLong;
                case FileNotFoundException _: return FileOperationErrorKind.NotFound;
                case DirectoryNotFoundException _: return FileOperationErrorKind.NotFound;
                case IOException io:
                    int code = io.HResult & 0xFFFF;
                    if (Windows) {
                        if (code == 32 || code == 33) { return FileOperationErrorKind.InUse; }
                        if (code == 39 || code == 112) { return FileOperationErrorKind.DiskFull; }
                        if (code == 80 || code == 183) { return FileOperationErrorKind.AlreadyExists; }
                    } else {
                        // .NET reports the raw errno for input/output failures on Unix.
                        if (io.HResult == 16 || io.HResult == 26) { return FileOperationErrorKind.InUse; }
                        if (io.HResult == 28 || io.HResult == 122) { return FileOperationErrorKind.DiskFull; }
                        if (io.HResult == 36 || io.HResult == 63) { return FileOperationErrorKind.PathTooLong; }
                        if (io.HResult == 17) { return FileOperationErrorKind.AlreadyExists; }
                    }
                    return FileOperationErrorKind.Other;
                default: return FileOperationErrorKind.Other;
            }
        }

        /// <summary>Whether a path has a name longer than volumes allow (255), or, on .NET Framework for Windows, passes MAX_PATH.</summary>
        internal static bool IsTooLong(string? path) {
            if (string.IsNullOrEmpty(path)) { return false; }
            int run = 0;
            foreach (char character in path!) {
                run = character == Path.DirectorySeparatorChar || character == Path.AltDirectorySeparatorChar ? 0 : run + 1;
                if (run > 255) { return true; }
            }
            if (Windows && LegacyPaths && path.Length >= 260 && !path.StartsWith(@"\\?\", StringComparison.Ordinal)) { return true; }
            return false;
        }

        internal static string FreeName(string target) {
            string folder = Path.GetDirectoryName(target) ?? string.Empty;
            bool folderName = Directory.Exists(target);
            string name = Path.GetFileName(target);
            string stem = folderName ? name : Path.GetFileNameWithoutExtension(name);
            string extension = folderName ? string.Empty : Path.GetExtension(name);
            for (int number = 2; ; number++) {
                string candidate = Path.Combine(folder, stem + " (" + number.ToString(System.Globalization.CultureInfo.InvariantCulture) + ")" + extension);
                if (!File.Exists(candidate) && !Directory.Exists(candidate)) { return candidate; }
            }
        }

        private readonly struct Item {
            internal Item(string source, string? target) { Source = source; Target = target; }
            internal string Source { get; }
            internal string? Target { get; }
        }

        private enum Outcome { Done, Skipped, Failed, Stopped }

        /// <summary>The state of one execution: counters, the effective conflict policy and progress sampling.</summary>
        private sealed class Run {
            private readonly FileOperation _operation;
            private readonly FileOperationOptions _options;
            private readonly CancellationToken _token;
            private readonly Stopwatch _clock = Stopwatch.StartNew();
            private readonly List<FileOperationError> _errors = new List<FileOperationError>();
            private readonly Queue<KeyValuePair<double, long>> _samples = new Queue<KeyValuePair<double, long>>();
            private readonly byte[] _buffer;
            private FileConflictPolicy _policy;
            private string[]? _volumes;
            private bool _stopped;
            private int _itemsTotal, _itemsDone, _completed, _skipped;
            private long _bytesTotal, _bytesDone, _written, _currentBytes, _currentDone;
            private string? _current;
            private double _lastReport = double.NegativeInfinity;
            private FileOperationPhase _phase = FileOperationPhase.Scanning;

            internal Run(FileOperation operation, CancellationToken token) {
                _operation = operation;
                _options = operation.Options;
                _token = token;
                _policy = _options.ConflictPolicy;
                _buffer = operation.Kind == FileOperationKind.Copy || operation.Kind == FileOperationKind.Move ? new byte[_options.BufferSize] : Array.Empty<byte>();
            }

            private FileOperationKind Kind => _operation.Kind;

            internal async Task<FileOperationResult> ExecuteAsync() {
                Report(true);
                foreach (Item item in _operation._items) {
                    if (Stop()) { break; }
                    if (Kind == FileOperationKind.Delete || Kind == FileOperationKind.CreateFolder) { _itemsTotal++; } else { Scan(item.Source, item.Target); }
                }
                _phase = FileOperationPhase.Running;
                Report(true);
                if (!_stopped) {
                    foreach (Item item in _operation._items) {
                        if (Stop()) { break; }
                        switch (Kind) {
                            case FileOperationKind.Copy:
                            case FileOperationKind.Move:
                                await TopTransferAsync(item).ConfigureAwait(false);
                                break;
                            case FileOperationKind.Delete:
                                Trash(item.Source);
                                break;
                            case FileOperationKind.DeletePermanently:
                                Remove(item.Source);
                                break;
                            case FileOperationKind.CreateFolder:
                                MakeFolder(item.Source);
                                break;
                        }
                    }
                }
                _current = null;
                _phase = FileOperationPhase.Completed;
                Report(true);
                int notProcessed = _stopped ? Math.Max(0, _itemsTotal - _itemsDone) : 0;
                return new FileOperationResult(_operation, _completed, _skipped, notProcessed, _written, _stopped, _clock.Elapsed, _errors.AsReadOnly());
            }

            private bool Stop() {
                if (!_stopped && _token.IsCancellationRequested) { _stopped = true; }
                return _stopped;
            }

            // Counts what the running phase will report on, so progress has real totals. A move within a volume is a
            // rename: it counts one item and no bytes, and only folders merging into an existing folder are walked.
            private void Scan(string source, string? target) {
                Stack<KeyValuePair<string, string?>> pending = new Stack<KeyValuePair<string, string?>>();
                pending.Push(new KeyValuePair<string, string?>(source, target));
                while (pending.Count > 0) {
                    if (Stop()) { return; }
                    KeyValuePair<string, string?> next = pending.Pop();
                    string path = next.Key;
                    string? to = next.Value;
                    _itemsTotal++;
                    try {
                        bool rename = Kind == FileOperationKind.Move && to != null && SameVolume(path, to);
                        if (Directory.Exists(path) && !IsLink(path)) {
                            if (!rename || Directory.Exists(to!)) {
                                foreach (string child in Directory.EnumerateFileSystemEntries(path)) {
                                    pending.Push(new KeyValuePair<string, string?>(child, to == null ? null : Path.Combine(to, Path.GetFileName(child))));
                                }
                            }
                        } else if (!rename && File.Exists(path) && !IsLink(path)) {
                            _bytesTotal += new FileInfo(path).Length;
                        }
                    } catch (IOException) { } catch (UnauthorizedAccessException) { }
                    _current = path;
                    Report(false);
                }
            }

            private async Task TopTransferAsync(Item item) {
                string source = item.Source, target = item.Target!;
                if (!File.Exists(source) && !Directory.Exists(source) && !IsLink(source)) {
                    Fail(source, target, FileOperationErrorKind.NotFound, "It no longer exists.", null);
                    return;
                }
                if (Directory.Exists(source) && !IsLink(source) && (IsInside(target, source) || Kind == FileOperationKind.Copy && SamePath(source, target))) {
                    Fail(source, target, FileOperationErrorKind.IntoItself, "A folder can't be " + (Kind == FileOperationKind.Copy ? "copied" : "moved") + " into itself.", null);
                    return;
                }
                if (Kind == FileOperationKind.Move && SamePath(source, target) && string.Equals(Path.GetFileName(source), Path.GetFileName(target), StringComparison.Ordinal)) {
                    Skip();
                    return;
                }
                try {
                    string? parent = Path.GetDirectoryName(target);
                    if (!string.IsNullOrEmpty(parent)) { Directory.CreateDirectory(parent); }
                } catch (Exception error) when (IsFileError(error)) {
                    Fail(source, target, error);
                    return;
                }
                await TransferAsync(source, target).ConfigureAwait(false);
            }

            private async Task<Outcome> TransferAsync(string source, string target) {
                if (Stop()) { return Outcome.Stopped; }
                bool move = Kind == FileOperationKind.Move;
                bool folder = Directory.Exists(source);
                bool link = IsLink(source);
                _current = source;
                // A case-only rename is the same path on case-insensitive volumes; it's a rename, not a conflict.
                bool caseRename = move && SamePath(source, target);
                if (folder && !link) {
                    if (File.Exists(target) && !caseRename) {
                        Fail(source, target, FileOperationErrorKind.AlreadyExists, "A file named '" + Path.GetFileName(target) + "' is in the way.", null);
                        return Outcome.Failed;
                    }
                    if (move && SameVolume(source, target) && (!Directory.Exists(target) || caseRename)) {
                        try {
                            if (caseRename) { CaseRename(source, target, true); } else { Directory.Move(source, target); }
                            Done(0);
                            return Outcome.Done;
                        } catch (IOException) when (Directory.Exists(source) && !Directory.Exists(target)) {
                            // Some file systems refuse a rename across mount points that DriveInfo didn't reveal; copy instead.
                        } catch (Exception error) when (IsFileError(error)) {
                            Fail(source, target, error);
                            return Outcome.Failed;
                        }
                    }
                    try { Directory.CreateDirectory(target); } catch (Exception error) when (IsFileError(error)) {
                        Fail(source, target, error);
                        return Outcome.Failed;
                    }
                    List<string> children;
                    try { children = Directory.EnumerateFileSystemEntries(source).ToList(); } catch (Exception error) when (IsFileError(error)) {
                        Fail(source, target, error);
                        return Outcome.Failed;
                    }
                    bool whole = true;
                    foreach (string child in children) {
                        Outcome outcome = await TransferAsync(child, Path.Combine(target, Path.GetFileName(child))).ConfigureAwait(false);
                        whole &= outcome == Outcome.Done;
                        if (outcome == Outcome.Stopped) { return Outcome.Stopped; }
                    }
                    if (_options.PreserveTimestamps) { CopyTimes(source, target, true); }
                    if (move && whole) {
                        try { Directory.Delete(source, false); } catch (Exception error) when (IsFileError(error)) {
                            Fail(source, target, error);
                            return Outcome.Failed;
                        }
                    }
                    if (whole) { Done(0); return Outcome.Done; }
                    Pass();
                    return Outcome.Skipped;
                }

                bool overwrite = false;
                if ((File.Exists(target) || Directory.Exists(target) || IsLink(target)) && !caseRename) {
                    if (Directory.Exists(target) && !IsLink(target)) {
                        Fail(source, target, FileOperationErrorKind.AlreadyExists, "A folder named '" + Path.GetFileName(target) + "' is in the way.", null);
                        return Outcome.Failed;
                    }
                    FileInfo from = new FileInfo(source), to = new FileInfo(target);
                    long sourceSize = link ? 0 : SafeLength(from);
                    FileConflict conflict = new FileConflict(source, target, sourceSize, from.LastWriteTimeUtc, SafeLength(to), to.LastWriteTimeUtc, FreeName(target));
                    FileConflictResolution? answer = await ResolveAsync(conflict).ConfigureAwait(false);
                    if (answer == FileConflictResolution.Cancel) { _stopped = true; return Outcome.Stopped; }
                    if (answer == FileConflictResolution.Rename) {
                        string renamed = conflict.RenameTarget;
                        if (string.IsNullOrWhiteSpace(renamed)) { renamed = FreeName(target); }
                        target = Path.IsPathRooted(renamed) ? renamed : Path.Combine(Path.GetDirectoryName(target) ?? string.Empty, renamed);
                        if (File.Exists(target) || Directory.Exists(target)) {
                            Fail(source, target, FileOperationErrorKind.AlreadyExists, "'" + Path.GetFileName(target) + "' exists too.", null);
                            return Outcome.Failed;
                        }
                    } else if (answer == FileConflictResolution.Overwrite && conflict.IsSameFile) {
                        Fail(source, target, FileOperationErrorKind.AlreadyExists, "A file can't overwrite itself.", null);
                        return Outcome.Failed;
                    } else if (answer != FileConflictResolution.Overwrite) {
                        Skip(sourceSize);
                        return Outcome.Skipped;
                    } else {
                        overwrite = true;
                    }
                }

                try {
                    if (move && SameVolume(source, target)) {
                        if (caseRename) { CaseRename(source, target, false); } else { MoveFile(source, target, overwrite, folder); }
                        Done(0);
                        return Outcome.Done;
                    }
                    if (link) {
                        CopyLink(source, target, overwrite, folder);
                    } else {
                        await CopyFileAsync(source, target, overwrite).ConfigureAwait(false);
                    }
                    if (move) {
                        if (folder) { Directory.Delete(source, false); } else { DeleteFile(source); }
                    }
                    Done(0);
                    return Outcome.Done;
                } catch (Exception error) when (IsFileError(error)) {
                    Fail(source, target, error);
                    return Outcome.Failed;
                }
            }

            private async Task<FileConflictResolution?> ResolveAsync(FileConflict conflict) {
                switch (_policy) {
                    case FileConflictPolicy.Skip: return FileConflictResolution.Skip;
                    case FileConflictPolicy.Overwrite: return conflict.IsSameFile ? FileConflictResolution.Skip : FileConflictResolution.Overwrite;
                    case FileConflictPolicy.OverwriteOlder: return conflict.SourceIsNewer && !conflict.IsSameFile ? FileConflictResolution.Overwrite : FileConflictResolution.Skip;
                    case FileConflictPolicy.Rename: return FileConflictResolution.Rename;
                }
                Func<FileConflict, CancellationToken, Task<FileConflictResolution>>? ask = _options.ResolveConflict;
                if (ask == null) { return FileConflictResolution.Skip; }
                FileConflictResolution answer;
                try { answer = await ask(conflict, _token).ConfigureAwait(false); } catch (OperationCanceledException) { return FileConflictResolution.Cancel; }
                if (answer == FileConflictResolution.OverwriteAll) { _policy = FileConflictPolicy.Overwrite; return conflict.IsSameFile ? FileConflictResolution.Skip : FileConflictResolution.Overwrite; }
                if (answer == FileConflictResolution.SkipAll) { _policy = FileConflictPolicy.Skip; return FileConflictResolution.Skip; }
                return answer;
            }

            private async Task CopyFileAsync(string source, string target, bool overwrite) {
                string folder = Path.GetDirectoryName(target) ?? string.Empty;
                string partial = Path.Combine(folder, "." + Path.GetFileName(target) + "." + Guid.NewGuid().ToString("N").Substring(0, 8) + ".partial");
                try {
                    using (FileStream input = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete, _buffer.Length, true))
                    using (FileStream output = new FileStream(partial, FileMode.CreateNew, FileAccess.Write, FileShare.None, _buffer.Length, true)) {
                        _currentBytes = input.Length;
                        _currentDone = 0;
                        // The file in progress always finishes; cancellation is honoured between files.
                        int read;
                        while ((read = await input.ReadAsync(_buffer, 0, _buffer.Length, CancellationToken.None).ConfigureAwait(false)) > 0) {
                            await output.WriteAsync(_buffer, 0, read, CancellationToken.None).ConfigureAwait(false);
                            _currentDone += read;
                            _bytesDone += read;
                            _written += read;
                            Report(false);
                        }
                    }
                    if (overwrite) { Replace(partial, target); } else { File.Move(partial, target); }
                } catch {
                    try { if (File.Exists(partial)) { File.Delete(partial); } } catch (IOException) { } catch (UnauthorizedAccessException) { }
                    throw;
                } finally {
                    _currentBytes = _currentDone = 0;
                }
                CopyAttributes(source, target);
            }

            private void CopyAttributes(string source, string target) {
                if (_options.PreserveTimestamps) { CopyTimes(source, target, false); }
                try {
#if NET8_0_OR_GREATER
                    if (!Windows) { File.SetUnixFileMode(target, File.GetUnixFileMode(source)); }
#endif
                    if (Windows) {
                        FileAttributes kept = File.GetAttributes(source) & (FileAttributes.ReadOnly | FileAttributes.Hidden | FileAttributes.System | FileAttributes.Archive);
                        if (kept != 0) { File.SetAttributes(target, kept); }
                    }
                } catch (IOException) { } catch (UnauthorizedAccessException) { }
            }

            private static void CopyTimes(string source, string target, bool folder) {
                try {
                    if (folder) {
                        Directory.SetLastWriteTimeUtc(target, Directory.GetLastWriteTimeUtc(source));
                        Directory.SetLastAccessTimeUtc(target, Directory.GetLastAccessTimeUtc(source));
                        if (Windows || RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) { Directory.SetCreationTimeUtc(target, Directory.GetCreationTimeUtc(source)); }
                    } else {
                        File.SetLastWriteTimeUtc(target, File.GetLastWriteTimeUtc(source));
                        File.SetLastAccessTimeUtc(target, File.GetLastAccessTimeUtc(source));
                        if (Windows || RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) { File.SetCreationTimeUtc(target, File.GetCreationTimeUtc(source)); }
                    }
                } catch (IOException) { } catch (UnauthorizedAccessException) { } catch (ArgumentOutOfRangeException) { }
            }

            private static void CopyLink(string source, string target, bool overwrite, bool folder) {
#if NET8_0_OR_GREATER
                FileSystemInfo info = folder ? new DirectoryInfo(source) : new FileInfo(source);
                string link = info.LinkTarget ?? throw new IOException("'" + source + "' is no longer a link.");
                if (overwrite) { DeleteFile(target); }
                if (folder) { Directory.CreateSymbolicLink(target, link); } else { File.CreateSymbolicLink(target, link); }
#else
                if (folder) { throw new IOException("Linked folders aren't copied on this runtime; '" + source + "' was left out."); }
                if (overwrite) { DeleteFile(target); }
                File.Copy(source, target);
#endif
            }

            private static void MoveFile(string source, string target, bool overwrite, bool folderLink) {
                if (folderLink) {
                    if (overwrite) { DeleteFile(target); }
                    Directory.Move(source, target);
                } else if (overwrite) {
                    Replace(source, target);
                } else {
                    File.Move(source, target);
                }
            }

            // Renames over an existing file in one step, so a failure leaves the existing file in place.
            private static void Replace(string replacement, string target) {
                FileAttributes attributes = File.GetAttributes(target);
                if ((attributes & FileAttributes.ReadOnly) != 0) { File.SetAttributes(target, attributes & ~FileAttributes.ReadOnly); }
#if NET8_0_OR_GREATER
                File.Move(replacement, target, true);
#else
                File.Replace(replacement, target, null, true);
#endif
            }

            // Renames through a free intermediate name; case-insensitive volumes treat a case-only rename as a no-op otherwise.
            private static void CaseRename(string source, string target, bool folder) {
                string step = Path.Combine(Path.GetDirectoryName(source) ?? string.Empty, "." + Guid.NewGuid().ToString("N").Substring(0, 8) + ".rename");
                if (folder) { Directory.Move(source, step); Directory.Move(step, target); } else { File.Move(source, step); File.Move(step, target); }
            }

            private static void DeleteFile(string path) {
                if (Directory.Exists(path) && IsLink(path)) { Directory.Delete(path, false); return; }
                FileAttributes attributes = File.GetAttributes(path);
                if ((attributes & FileAttributes.ReadOnly) != 0) { File.SetAttributes(path, attributes & ~FileAttributes.ReadOnly); }
                File.Delete(path);
            }

            private void Trash(string path) {
                _current = path;
                Report(false);
                ITrash trash = _options.Trash ?? FileTrash.Current;
                if (!trash.IsAvailable) {
                    Fail(path, null, FileOperationErrorKind.TrashUnavailable, "There's no " + trash.Name.ToLowerInvariant() + " here, so it was left in place.", null);
                    return;
                }
                try {
                    trash.MoveToTrash(path);
                    Done(0);
                } catch (Exception error) when (IsFileError(error)) {
                    FileOperationErrorKind kind = Classify(error, path);
                    Fail(path, null, kind == FileOperationErrorKind.Other ? FileOperationErrorKind.TrashUnavailable : kind, error.Message, error);
                }
            }

            private Outcome Remove(string path) {
                if (Stop()) { return Outcome.Stopped; }
                _current = path;
                Report(false);
                try {
                    if (Directory.Exists(path) && !IsLink(path)) {
                        bool whole = true;
                        foreach (string child in Directory.EnumerateFileSystemEntries(path).ToList()) {
                            Outcome outcome = Remove(child);
                            if (outcome == Outcome.Stopped) { return outcome; }
                            whole &= outcome == Outcome.Done;
                        }
                        if (!whole) { Pass(); return Outcome.Skipped; }
                        Directory.Delete(path, false);
                    } else if (File.Exists(path) || IsLink(path)) {
                        DeleteFile(path);
                    } else {
                        Fail(path, null, FileOperationErrorKind.NotFound, "It no longer exists.", null);
                        return Outcome.Failed;
                    }
                    Done(0);
                    return Outcome.Done;
                } catch (Exception error) when (IsFileError(error)) {
                    Fail(path, null, error);
                    return Outcome.Failed;
                }
            }

            private void MakeFolder(string path) {
                _current = path;
                if (File.Exists(path) || Directory.Exists(path)) {
                    Fail(path, path, FileOperationErrorKind.AlreadyExists, "'" + Path.GetFileName(path) + "' already exists.", null);
                    return;
                }
                try {
                    Directory.CreateDirectory(path);
                    Done(0);
                } catch (Exception error) when (IsFileError(error)) {
                    Fail(path, path, error);
                }
            }

            private bool SameVolume(string source, string target) {
                if (Windows) {
                    // A path past MAX_PATH on .NET Framework has no root to compare; the copy that follows reports it.
                    try { return string.Equals(Path.GetPathRoot(Full(source)), Path.GetPathRoot(Full(target)), StringComparison.OrdinalIgnoreCase); } catch (PathTooLongException) { return false; }
                }
                if (_volumes == null) {
                    try { _volumes = DriveInfo.GetDrives().Select(drive => drive.Name).ToArray(); } catch (IOException) { _volumes = Array.Empty<string>(); } catch (UnauthorizedAccessException) { _volumes = Array.Empty<string>(); }
                }
                return string.Equals(VolumeOf(source), VolumeOf(Path.GetDirectoryName(Full(target)) ?? target), StringComparison.Ordinal);
            }

            private string VolumeOf(string path) {
                string full = Full(path), best = "/";
                foreach (string volume in _volumes!) {
                    string root = volume.TrimEnd('/');
                    if (root.Length > best.TrimEnd('/').Length && (full == root || full.StartsWith(root + "/", StringComparison.Ordinal))) { best = volume; }
                }
                return best;
            }

            private static long SafeLength(FileInfo info) {
                try { return info.Exists ? info.Length : 0; } catch (IOException) { return 0; } catch (UnauthorizedAccessException) { return 0; }
            }

            private static bool IsFileError(Exception error) => error is IOException || error is UnauthorizedAccessException || error is System.Security.SecurityException || error is NotSupportedException || error is ArgumentException;

            private void Done(long bytes) {
                _completed++;
                _itemsDone++;
                _bytesDone += bytes;
                Report(false);
            }

            // A folder whose contents were partly skipped or failed: counted, but neither done nor failed itself.
            private void Pass() {
                _itemsDone++;
                Report(false);
            }

            private void Skip(long bytes = 0) {
                _skipped++;
                _itemsDone++;
                _bytesDone += bytes;
                Report(false);
            }

            private void Fail(string source, string? target, Exception error) => Fail(source, target, Classify(error, target ?? source), error.Message, error);

            private void Fail(string source, string? target, FileOperationErrorKind kind, string message, Exception? error) {
                _errors.Add(new FileOperationError(source, target, kind, message, error));
                _itemsDone++;
                Report(false);
            }

            private void Report(bool force) {
                IProgress<FileOperationProgress>? progress = _options.Progress;
                if (progress == null) { return; }
                double now = _clock.Elapsed.TotalSeconds;
                if (!force && now - _lastReport < _options.ProgressInterval.TotalSeconds) { return; }
                _lastReport = now;
                // Rate over the last three seconds, so a stall shows quickly and a burst doesn't dominate.
                _samples.Enqueue(new KeyValuePair<double, long>(now, _bytesDone));
                while (_samples.Count > 2 && now - _samples.Peek().Key > 3) { _samples.Dequeue(); }
                KeyValuePair<double, long> oldest = _samples.Peek();
                double span = now - oldest.Key;
                double rate = span > 0.05 ? (_bytesDone - oldest.Value) / span : 0;
                TimeSpan? remaining = null;
                if (_phase == FileOperationPhase.Running) {
                    if (_bytesTotal > 0 && rate > 0) {
                        remaining = TimeSpan.FromSeconds(Math.Max(0, _bytesTotal - _bytesDone) / rate);
                    } else if (_bytesTotal == 0 && _itemsDone > 0 && now > 0.5) {
                        remaining = TimeSpan.FromSeconds(Math.Max(0, _itemsTotal - _itemsDone) * now / _itemsDone);
                    }
                } else if (_phase == FileOperationPhase.Completed) {
                    remaining = TimeSpan.Zero;
                }
                progress.Report(new FileOperationProgress(Kind, _phase, _current, _itemsDone, _itemsTotal, _bytesDone, _bytesTotal, _currentDone, _currentBytes, rate, _clock.Elapsed, remaining));
            }
        }
    }
}
