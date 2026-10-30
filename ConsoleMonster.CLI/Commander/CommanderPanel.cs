using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ConsoleMonster.Text;

namespace ConsoleMonster.Apps.Commander {
    /// <summary>
    /// One Commander panel: a folder listing with a cursor, sort order, marks, quick search and background folder
    /// sizes. It holds no rendering; a data grid binds to <see cref="Entries"/>, <see cref="CursorIndex"/> and the marks.
    /// </summary>
    /// <remarks>
    /// The panel belongs to the app thread. The only background work is the folder-size scan, which updates
    /// <see cref="PanelEntry.Size"/> and increments <see cref="Revision"/>; a view repaints when the revision changes.
    /// A listing is sorted once, so rows never jump while sizes arrive.
    /// </remarks>
    public sealed class CommanderPanel : IDisposable {
        private static readonly StringComparer IdComparer = StringComparer.Ordinal;
        private PanelEntry[] _entries = Array.Empty<PanelEntry>();
        private Dictionary<string, int> _index = new Dictionary<string, int>(IdComparer);
        private readonly HashSet<string> _marks = new HashSet<string>(IdComparer);
        private CancellationTokenSource? _sizes;
        private long _revision;
        private int _cursor;
        private int _pageSize = 20;
        private bool _computeSizes;

        /// <summary>Opens a panel on a folder with the default sort (name, ascending).</summary>
        /// <param name="folder">The folder to list.</param>
        public CommanderPanel(string folder) : this(new PanelState { Folder = folder }) { }

        /// <summary>Opens a panel from remembered state. A remembered folder that no longer exists falls back to its nearest existing parent, then the home folder.</summary>
        /// <param name="state">The state.</param>
        public CommanderPanel(PanelState state) {
            if (state == null) { throw new ArgumentNullException(nameof(state)); }
            SortColumn = state.SortColumn;
            SortDescending = state.SortDescending;
            ShowHidden = state.ShowHidden;
            _computeSizes = state.ComputeFolderSizes;
            string folder = Existing(state.Folder) ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            Folder = Normalize(folder);
            Load(Folder, null);
        }

        /// <summary>The listed folder.</summary>
        public string Folder { get; private set; }

        /// <summary>The sort column.</summary>
        public PanelSortColumn SortColumn { get; private set; }

        /// <summary>Whether the sort is descending.</summary>
        public bool SortDescending { get; private set; }

        /// <summary>Whether hidden files are listed. Change it with <see cref="SetShowHidden"/>.</summary>
        public bool ShowHidden { get; private set; }

        /// <summary>
        /// Whether folder sizes are computed in the background after each listing; on unless the state turned it off.
        /// Turning it off stops a running scan, and turning it on starts one.
        /// </summary>
        public bool ComputeFolderSizes {
            get => _computeSizes;
            set {
                if (value == _computeSizes) { return; }
                _computeSizes = value;
                if (value) { StartFolderSizes(); } else { CancelSizes(); }
            }
        }

        /// <summary>The rows in display order: <c>..</c>, then folders, then files.</summary>
        public IReadOnlyList<PanelEntry> Entries => _entries;

        /// <summary>The cursor row's index.</summary>
        public int CursorIndex {
            get => _cursor;
            set { int clamped = Clamp(value); if (clamped != _cursor) { _cursor = clamped; Touch(); } }
        }

        /// <summary>The cursor row, or null for an empty listing.</summary>
        public PanelEntry? Current => _entries.Length == 0 ? null : _entries[_cursor];

        /// <summary>Rows per page for <see cref="PageDown"/> and <see cref="PageUp"/>; the view sets it from its height.</summary>
        public int PageSize {
            get => _pageSize;
            set => _pageSize = Math.Max(1, value);
        }

        /// <summary>Increments on every change, including folder sizes arriving from the background, so a view knows when to repaint.</summary>
        public long Revision => Interlocked.Read(ref _revision);

        /// <summary>Why the last navigation or refresh failed, or null when it succeeded.</summary>
        public string? Error { get; private set; }

        /// <summary>The quick-search text; empty when no search is active.</summary>
        public string QuickSearch { get; private set; } = string.Empty;

        /// <summary>The current folder-size scan, or a completed task when none is running.</summary>
        public Task FolderSizes { get; private set; } = Task.CompletedTask;

        /// <summary>The state worth remembering: folder, sort and view settings.</summary>
        public PanelState State => new PanelState { Folder = Folder, SortColumn = SortColumn, SortDescending = SortDescending, ShowHidden = ShowHidden, ComputeFolderSizes = ComputeFolderSizes };

        /// <summary>Lists another folder. On failure the panel keeps its listing and sets <see cref="Error"/>.</summary>
        /// <param name="folder">The folder; relative paths are relative to the current folder.</param>
        /// <returns>Whether the folder was opened.</returns>
        public bool Navigate(string folder) => Navigate(folder, null);

        /// <summary>Opens the parent folder with the cursor on the folder just left.</summary>
        /// <returns>False at the root.</returns>
        public bool GoToParent() {
            string? parent = Path.GetDirectoryName(Folder);
            return parent != null && Navigate(parent, Folder);
        }

        /// <summary>Opens the cursor row when it's a folder or <c>..</c>.</summary>
        /// <returns>True when a folder was opened; false for a file, which the caller opens or runs.</returns>
        public bool OpenCurrent() {
            PanelEntry? current = Current;
            if (current == null || !current.IsFolder) { return false; }
            return current.IsParent ? GoToParent() : Navigate(current.FullPath, null);
        }

        /// <summary>Lists the folder again, keeping the cursor on the same row and the marks on rows that still exist.</summary>
        /// <returns>Whether the folder could be read. A folder that disappeared opens its nearest existing parent.</returns>
        public bool Refresh() {
            string? keep = Current?.Id;
            if (!Directory.Exists(Folder)) {
                string fallback = Existing(Folder) ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                return Navigate(fallback, null);
            }
            return Load(Folder, keep, true);
        }

        /// <summary>Sorts by a column. The cursor stays on the same row.</summary>
        /// <param name="column">The column.</param>
        /// <param name="descending">Whether to sort descending.</param>
        public void Sort(PanelSortColumn column, bool descending) {
            SortColumn = column;
            SortDescending = descending;
            string? keep = Current?.Id;
            _entries = Sorted(_entries);
            Reindex();
            _cursor = keep != null && _index.TryGetValue(keep, out int index) ? index : Clamp(_cursor);
            Touch();
        }

        /// <summary>Sorts by a column, or flips the direction when it's already the sort column.</summary>
        /// <param name="column">The column.</param>
        public void ToggleSort(PanelSortColumn column) => Sort(column, column == SortColumn ? !SortDescending : false);

        /// <summary>Shows or hides hidden files and lists the folder again.</summary>
        /// <param name="showHidden">Whether hidden files are listed.</param>
        public void SetShowHidden(bool showHidden) {
            if (showHidden == ShowHidden) { return; }
            ShowHidden = showHidden;
            Refresh();
        }

        /// <summary>Moves the cursor by a number of rows, stopping at the ends.</summary>
        /// <param name="rows">Rows to move; negative moves up.</param>
        public void MoveCursor(int rows) => CursorIndex = _cursor + rows;

        /// <summary>Moves the cursor down one page.</summary>
        public void PageDown() => MoveCursor(_pageSize);

        /// <summary>Moves the cursor up one page.</summary>
        public void PageUp() => MoveCursor(-_pageSize);

        /// <summary>Moves the cursor to the first row.</summary>
        public void MoveToStart() => CursorIndex = 0;

        /// <summary>Moves the cursor to the last row.</summary>
        public void MoveToEnd() => CursorIndex = _entries.Length - 1;

        /// <summary>Moves the cursor to a row by identity.</summary>
        /// <param name="id">The row's <see cref="PanelEntry.Id"/>, such as a full path.</param>
        /// <returns>Whether the row is listed.</returns>
        public bool MoveTo(string id) {
            if (id == null || !_index.TryGetValue(id, out int index)) { return false; }
            CursorIndex = index;
            return true;
        }

        /// <summary>The row with an identity, or null.</summary>
        /// <param name="id">The identity.</param>
        /// <returns>The row.</returns>
        public PanelEntry? Find(string id) => id != null && _index.TryGetValue(id, out int index) ? _entries[index] : null;

        #region Marks

        /// <summary>How many rows are marked.</summary>
        public int MarkedCount => _marks.Count;

        /// <summary>The marked rows' total size: files plus folders whose size is known.</summary>
        public long MarkedSize {
            get {
                long total = 0;
                foreach (string id in _marks) {
                    if (_index.TryGetValue(id, out int index)) { total += _entries[index].Size ?? 0; }
                }
                return total;
            }
        }

        /// <summary>Whether <see cref="MarkedSize"/> includes every marked folder, or some are still being sized.</summary>
        public bool MarkedSizeIsComplete => _marks.All(id => !_index.TryGetValue(id, out int index) || _entries[index].Size.HasValue);

        /// <summary>The marked rows in display order.</summary>
        public IReadOnlyList<PanelEntry> MarkedEntries => _marks.Count == 0 ? Array.Empty<PanelEntry>() : _entries.Where(entry => _marks.Contains(entry.Id)).ToArray();

        /// <summary>What an operation acts on: the marked rows, or the cursor row when nothing is marked. Never the <c>..</c> row.</summary>
        public IReadOnlyList<PanelEntry> Targets {
            get {
                if (_marks.Count != 0) { return MarkedEntries; }
                PanelEntry? current = Current;
                return current == null || current.IsParent ? Array.Empty<PanelEntry>() : new[] { current };
            }
        }

        /// <summary>Whether a row is marked.</summary>
        /// <param name="entry">The row.</param>
        /// <returns>True when marked.</returns>
        public bool IsMarked(PanelEntry entry) => entry != null && _marks.Contains(entry.Id);

        /// <summary>Marks or unmarks a row. The <c>..</c> row can't be marked.</summary>
        /// <param name="entry">The row.</param>
        /// <param name="marked">Whether it's marked.</param>
        public void SetMark(PanelEntry entry, bool marked) {
            if (entry == null || entry.IsParent || !_index.ContainsKey(entry.Id)) { return; }
            if (marked ? _marks.Add(entry.Id) : _marks.Remove(entry.Id)) { Touch(); }
        }

        /// <summary>Flips the cursor row's mark and moves down, as Insert does.</summary>
        public void ToggleMark() {
            PanelEntry? current = Current;
            if (current == null) { return; }
            SetMark(current, !IsMarked(current));
            MoveCursor(1);
        }

        /// <summary>Marks the rows whose names match a pattern such as <c>*.cs;*.md</c> (see <see cref="GlobPattern"/>).</summary>
        /// <param name="pattern">The pattern.</param>
        /// <param name="includeFolders">Whether folders are marked too; by default only files, as in other commanders.</param>
        /// <returns>How many rows were newly marked.</returns>
        public int MarkPattern(string pattern, bool includeFolders = false) => ApplyPattern(pattern, includeFolders, true);

        /// <summary>Unmarks the rows whose names match a pattern.</summary>
        /// <param name="pattern">The pattern.</param>
        /// <param name="includeFolders">Whether folders are unmarked too.</param>
        /// <returns>How many rows were unmarked.</returns>
        public int UnmarkPattern(string pattern, bool includeFolders = true) => ApplyPattern(pattern, includeFolders, false);

        /// <summary>Marks every unmarked row and unmarks every marked one.</summary>
        /// <param name="includeFolders">Whether folders are inverted too; by default only files.</param>
        public void InvertMarks(bool includeFolders = false) {
            foreach (PanelEntry entry in _entries) {
                if (entry.IsParent || entry.IsFolder && !includeFolders) { continue; }
                if (!_marks.Remove(entry.Id)) { _marks.Add(entry.Id); }
            }
            Touch();
        }

        /// <summary>Unmarks everything.</summary>
        public void ClearMarks() {
            if (_marks.Count == 0) { return; }
            _marks.Clear();
            Touch();
        }

        private int ApplyPattern(string pattern, bool includeFolders, bool mark) {
            GlobPattern glob = new GlobPattern(pattern ?? throw new ArgumentNullException(nameof(pattern)), !CaseInsensitiveNames);
            int changed = 0;
            foreach (PanelEntry entry in _entries) {
                if (entry.IsParent || entry.IsFolder && !includeFolders || !glob.IsMatch(entry.Name)) { continue; }
                if (mark ? _marks.Add(entry.Id) : _marks.Remove(entry.Id)) { changed++; }
            }
            if (changed != 0) { Touch(); }
            return changed;
        }

        #endregion

        #region Quick search

        /// <summary>Searches for the first row whose name starts with the text and moves the cursor there.</summary>
        /// <param name="text">The prefix; empty clears the search.</param>
        /// <returns>Whether a row matched. Without a match the search text and cursor stay as they were.</returns>
        public bool Search(string text) {
            if (string.IsNullOrEmpty(text)) { ClearSearch(); return true; }
            int found = FindMatch(text, 0);
            if (found < 0) { return false; }
            QuickSearch = text;
            _cursor = found;
            Touch();
            return true;
        }

        /// <summary>Adds a typed character to the search.</summary>
        /// <param name="character">The character.</param>
        /// <returns>Whether the longer prefix still matches; if not, the character is not added.</returns>
        public bool AppendSearch(char character) => Search(QuickSearch + character);

        /// <summary>Removes the last search character, keeping the cursor where it is.</summary>
        public void BackspaceSearch() {
            if (QuickSearch.Length == 0) { return; }
            QuickSearch = QuickSearch.Substring(0, QuickSearch.Length - 1);
            Touch();
        }

        /// <summary>Moves to the next row matching the search, wrapping at the end.</summary>
        /// <returns>Whether there is a match.</returns>
        public bool SearchNext() {
            if (QuickSearch.Length == 0 || _entries.Length == 0) { return false; }
            int found = FindMatch(QuickSearch, (_cursor + 1) % _entries.Length);
            if (found < 0) { return false; }
            CursorIndex = found;
            return true;
        }

        /// <summary>Ends the search; the cursor stays on the found row.</summary>
        public void ClearSearch() {
            if (QuickSearch.Length == 0) { return; }
            QuickSearch = string.Empty;
            Touch();
        }

        /// <summary>Whether a row matches the current search.</summary>
        /// <param name="entry">The row.</param>
        /// <returns>True when a search is active and the name starts with it.</returns>
        public bool MatchesSearch(PanelEntry entry) => QuickSearch.Length != 0 && entry != null && !entry.IsParent && entry.Name.StartsWith(QuickSearch, StringComparison.OrdinalIgnoreCase);

        private int FindMatch(string text, int start) {
            for (int step = 0; step < _entries.Length; step++) {
                int index = (start + step) % _entries.Length;
                PanelEntry entry = _entries[index];
                if (!entry.IsParent && entry.Name.StartsWith(text, StringComparison.OrdinalIgnoreCase)) { return index; }
            }
            return -1;
        }

        #endregion

        #region Folder sizes

        /// <summary>
        /// Computes folder sizes in the background, cursor-near rows first. A new listing cancels the scan. It's started
        /// automatically after each listing when <see cref="ComputeFolderSizes"/> is on.
        /// </summary>
        /// <returns>The scan, which completes when every folder is sized or the scan is cancelled.</returns>
        public Task StartFolderSizes() {
            Task previous = FolderSizes;
            CancelSizes();
            PanelEntry[] entries = _entries;
            int cursor = _cursor;
            CancellationTokenSource cancel = new CancellationTokenSource();
            _sizes = cancel;
            CancellationToken token = cancel.Token;
            FolderSizes = Task.Run(async () => {
                // The old scan must finish resetting its Computing row before collecting pending work.
                // Await on this worker so restarting a scan never blocks the UI thread.
                await previous.ConfigureAwait(false);
                if (token.IsCancellationRequested) { return; }
                List<PanelEntry> folders = new List<PanelEntry>();
                for (int distance = 0; distance < entries.Length; distance++) {
                    AddPending(folders, entries, cursor + distance);
                    if (distance != 0) { AddPending(folders, entries, cursor - distance); }
                }
                foreach (PanelEntry folder in folders) {
                    if (token.IsCancellationRequested) { break; }
                    folder.SetSizeState(EntrySizeState.Computing);
                    Interlocked.Increment(ref _revision);
                    bool partial = false;
                    long size = MeasureFolder(folder.FullPath, token, ref partial);
                    if (token.IsCancellationRequested) { folder.SetSizeState(EntrySizeState.Pending); break; }
                    folder.SetSize(size, partial);
                    Interlocked.Increment(ref _revision);
                }
            });
            return FolderSizes;
        }

        private static void AddPending(List<PanelEntry> folders, PanelEntry[] entries, int index) {
            if (index < 0 || index >= entries.Length) { return; }
            PanelEntry entry = entries[index];
            if (entry.IsFolder && !entry.IsParent && !entry.IsLink && entry.SizeState == EntrySizeState.Pending) { folders.Add(entry); }
        }

        /// <summary>Stops the folder-size scan. Sizes already computed stay.</summary>
        public void CancelSizes() {
            CancellationTokenSource? sizes = _sizes;
            _sizes = null;
            if (sizes == null) { return; }
            sizes.Cancel();
            Task running = FolderSizes;
            _ = running.ContinueWith(_ => sizes.Dispose(), TaskScheduler.Default);
        }

        private static long MeasureFolder(string path, CancellationToken token, ref bool partial) {
            long total = 0;
            Stack<DirectoryInfo> pending = new Stack<DirectoryInfo>();
            pending.Push(new DirectoryInfo(path));
            while (pending.Count > 0) {
                if (token.IsCancellationRequested) { return total; }
                DirectoryInfo folder = pending.Pop();
                try {
                    foreach (FileSystemInfo info in folder.EnumerateFileSystemInfos()) {
                        if (token.IsCancellationRequested) { return total; }
                        // Links are counted as themselves, never followed, so a link loop can't run forever.
                        if ((info.Attributes & FileAttributes.ReparsePoint) != 0) { continue; }
                        if (info is DirectoryInfo child) { pending.Push(child); } else if (info is FileInfo file) { total += file.Length; }
                    }
                } catch (IOException) { partial = true; } catch (UnauthorizedAccessException) { partial = true; } catch (System.Security.SecurityException) { partial = true; }
            }
            return total;
        }

        #endregion

        /// <summary>Stops background work.</summary>
        public void Dispose() => CancelSizes();

        private bool Navigate(string folder, string? cursorOn) {
            if (string.IsNullOrWhiteSpace(folder)) { return false; }
            string full;
            try { full = Normalize(Path.IsPathRooted(folder) ? folder : Path.Combine(Folder, folder)); } catch (Exception error) when (error is ArgumentException || error is NotSupportedException || error is PathTooLongException) {
                Error = error.Message;
                Touch();
                return false;
            }
            return Load(full, cursorOn);
        }

        private bool Load(string folder, string? cursorOn, bool keepMarks = false) {
            PanelEntry[] listing;
            try { listing = List(folder); } catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is System.Security.SecurityException || error is ArgumentException) {
                Error = error is DirectoryNotFoundException ? "'" + folder + "' doesn't exist." : error.Message;
                Touch();
                return false;
            }
            CancelSizes();
            bool sameFolder = string.Equals(folder, Folder, StringComparison.Ordinal);
            Folder = folder;
            Error = null;
            _entries = Sorted(listing);
            Reindex();
            if (!keepMarks || !sameFolder) { _marks.Clear(); } else { _marks.RemoveWhere(id => !_index.ContainsKey(id)); }
            if (!sameFolder) { QuickSearch = string.Empty; }
            int previous = _cursor;
            _cursor = cursorOn != null && _index.TryGetValue(cursorOn, out int index) ? index : sameFolder ? Clamp(previous) : 0;
            Touch();
            if (ComputeFolderSizes) { StartFolderSizes(); }
            return true;
        }

        private PanelEntry[] List(string folder) {
            DirectoryInfo directory = new DirectoryInfo(folder);
            if (!directory.Exists) { throw new DirectoryNotFoundException("'" + folder + "' doesn't exist."); }
            List<PanelEntry> entries = new List<PanelEntry>();
            DirectoryInfo? parent = directory.Parent;
            if (parent != null) {
                entries.Add(new PanelEntry(PanelEntry.ParentId, parent.FullName, true, true, 0, SafeTime(directory), FileAttributes.Directory));
            }
            foreach (FileSystemInfo info in directory.EnumerateFileSystemInfos()) {
                FileAttributes attributes;
                try { attributes = info.Attributes; } catch (IOException) { continue; } catch (UnauthorizedAccessException) { continue; }
                if (!ShowHidden && PanelEntry.IsHiddenItem(info.Name, attributes)) { continue; }
                bool isFolder = (attributes & FileAttributes.Directory) != 0;
                long size = 0;
                if (!isFolder && info is FileInfo file) {
                    try { size = file.Length; } catch (IOException) { } catch (UnauthorizedAccessException) { }
                }
                entries.Add(new PanelEntry(info.Name, info.FullName, isFolder, false, size, SafeTime(info), attributes));
            }
            return entries.ToArray();
        }

        private PanelEntry[] Sorted(PanelEntry[] entries) {
            // Sizes can change on the scan thread mid-sort, so the comparer works on a snapshot.
            SortKey[] keys = new SortKey[entries.Length];
            for (int i = 0; i < entries.Length; i++) { keys[i] = new SortKey(entries[i], SortColumn); }
            Array.Sort(keys, new KeyComparer(SortColumn, SortDescending));
            PanelEntry[] sorted = new PanelEntry[entries.Length];
            for (int i = 0; i < keys.Length; i++) { sorted[i] = keys[i].Entry; }
            return sorted;
        }

        private void Reindex() {
            Dictionary<string, int> index = new Dictionary<string, int>(_entries.Length, IdComparer);
            for (int i = 0; i < _entries.Length; i++) { index[_entries[i].Id] = i; }
            _index = index;
        }

        private int Clamp(int index) => _entries.Length == 0 ? 0 : Math.Max(0, Math.Min(_entries.Length - 1, index));

        private void Touch() => Interlocked.Increment(ref _revision);

        private static bool CaseInsensitiveNames => System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows)
            || System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX);

        private static DateTime SafeTime(FileSystemInfo info) {
            try { return info.LastWriteTime; } catch (IOException) { return DateTime.MinValue; } catch (UnauthorizedAccessException) { return DateTime.MinValue; }
        }

        private static string Normalize(string folder) {
            string full = Path.GetFullPath(folder);
            string root = Path.GetPathRoot(full) ?? string.Empty;
            return full.Length > root.Length ? full.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) : full;
        }

        private static string? Existing(string? folder) {
            if (string.IsNullOrWhiteSpace(folder)) { return null; }
            try {
                string? current = Path.GetFullPath(folder);
                while (current != null && !Directory.Exists(current)) { current = Path.GetDirectoryName(current); }
                return current;
            } catch (Exception error) when (error is ArgumentException || error is NotSupportedException || error is PathTooLongException || error is System.Security.SecurityException) { return null; }
        }

        // Everything the comparer reads, computed once per row rather than once per comparison.
        private readonly struct SortKey {
            internal SortKey(PanelEntry entry, PanelSortColumn column) {
                Entry = entry;
                Rank = entry.IsParent ? 0 : entry.IsFolder ? 1 : 2;
                Size = entry.Size ?? -1;
                Ticks = entry.Modified.Ticks;
                Extension = column == PanelSortColumn.Extension ? entry.Extension : string.Empty;
            }
            internal PanelEntry Entry { get; }
            internal int Rank { get; }
            internal long Size { get; }
            internal long Ticks { get; }
            internal string Extension { get; }
        }

        private sealed class KeyComparer : IComparer<SortKey> {
            private readonly PanelSortColumn _column;
            private readonly bool _descending;
            internal KeyComparer(PanelSortColumn column, bool descending) { _column = column; _descending = descending; }

            public int Compare(SortKey left, SortKey right) {
                if (left.Rank != right.Rank) { return left.Rank < right.Rank ? -1 : 1; }
                int result = 0;
                switch (_column) {
                    case PanelSortColumn.Extension: result = string.Compare(left.Extension, right.Extension, StringComparison.OrdinalIgnoreCase); break;
                    case PanelSortColumn.Size: result = left.Size.CompareTo(right.Size); break;
                    case PanelSortColumn.Modified: result = left.Ticks.CompareTo(right.Ticks); break;
                }
                if (result == 0) { result = string.Compare(left.Entry.Name, right.Entry.Name, StringComparison.OrdinalIgnoreCase); }
                if (result == 0) { result = string.CompareOrdinal(left.Entry.Name, right.Entry.Name); }
                return _descending ? -result : result;
            }
        }
    }
}
