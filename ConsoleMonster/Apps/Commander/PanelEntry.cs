using System;
using System.IO;
using System.Threading;

namespace ConsoleMonster.Apps.Commander {
    /// <summary>How a panel orders its rows. Folders always come first and <c>..</c> stays on top.</summary>
    public enum PanelSortColumn {
        /// <summary>By name, ignoring case.</summary>
        Name,
        /// <summary>By extension, then name.</summary>
        Extension,
        /// <summary>By size; folders whose size is still unknown sort as empty.</summary>
        Size,
        /// <summary>By last modification time.</summary>
        Modified
    }

    /// <summary>Where a row's size stands. Files are always <see cref="Known"/>; folder sizes are computed in the background.</summary>
    public enum EntrySizeState {
        /// <summary>Not computed yet; a view shows a placeholder such as <c>…</c>.</summary>
        Pending,
        /// <summary>Being computed now.</summary>
        Computing,
        /// <summary>Known.</summary>
        Known,
        /// <summary>Computed, but some subfolders couldn't be read, so the size is a lower bound.</summary>
        Partial
    }

    /// <summary>One row of a <see cref="CommanderPanel"/>: a file, a folder or the <c>..</c> parent row.</summary>
    /// <remarks>Rows are created by a listing and keep their <see cref="Id"/> across refreshes. A background folder-size scan may update <see cref="Size"/> and <see cref="SizeState"/>; everything else is fixed.</remarks>
    public sealed class PanelEntry {
        /// <summary>The identity of the parent row.</summary>
        public const string ParentId = "..";
        private long _size;
        private int _sizeState;

        internal PanelEntry(string name, string fullPath, bool isFolder, bool isParent, long size, DateTime modified, FileAttributes attributes) {
            Name = name;
            FullPath = fullPath;
            IsFolder = isFolder;
            IsParent = isParent;
            Modified = modified;
            Attributes = attributes;
            _size = isFolder ? 0 : size;
            _sizeState = (int)(isFolder ? EntrySizeState.Pending : EntrySizeState.Known);
            Id = isParent ? ParentId : fullPath;
        }

        /// <summary>Stable row identity: the full path, or <see cref="ParentId"/> for the parent row. Marks, the cursor and a grid's selection key on it.</summary>
        public string Id { get; }

        /// <summary>The name shown in the panel; <c>..</c> for the parent row.</summary>
        public string Name { get; }

        /// <summary>The full path; for the parent row, the parent folder.</summary>
        public string FullPath { get; }

        /// <summary>The extension without its dot, or empty for folders and names without one.</summary>
        public string Extension {
            get {
                if (IsFolder) { return string.Empty; }
                int dot = Name.LastIndexOf('.');
                return dot <= 0 ? string.Empty : Name.Substring(dot + 1);
            }
        }

        /// <summary>Whether this is a folder (or a link to one).</summary>
        public bool IsFolder { get; }

        /// <summary>Whether this is the <c>..</c> row.</summary>
        public bool IsParent { get; }

        /// <summary>Whether this is a symbolic link, junction or other reparse point.</summary>
        public bool IsLink => (Attributes & FileAttributes.ReparsePoint) != 0;

        /// <summary>Whether the item is hidden: the hidden attribute, or a leading dot on Unix.</summary>
        public bool IsHidden => (Attributes & FileAttributes.Hidden) != 0;

        /// <summary>Whether the item is read-only.</summary>
        public bool IsReadOnly => (Attributes & FileAttributes.ReadOnly) != 0;

        /// <summary>The file system attributes when listed.</summary>
        public FileAttributes Attributes { get; }

        /// <summary>When the item was last modified, in local time.</summary>
        public DateTime Modified { get; }

        /// <summary>The size in bytes; null for a folder until its size is computed.</summary>
        public long? Size {
            get {
                EntrySizeState state = SizeState;
                return state == EntrySizeState.Known || state == EntrySizeState.Partial ? Interlocked.Read(ref _size) : (long?)null;
            }
        }

        /// <summary>Whether the size is known, pending or being computed.</summary>
        public EntrySizeState SizeState => (EntrySizeState)Volatile.Read(ref _sizeState);

        internal void SetSizeState(EntrySizeState state) => Volatile.Write(ref _sizeState, (int)state);

        internal void SetSize(long size, bool partial) {
            Interlocked.Exchange(ref _size, size);
            SetSizeState(partial ? EntrySizeState.Partial : EntrySizeState.Known);
        }

        /// <inheritdoc />
        public override string ToString() => Name;
    }
}
