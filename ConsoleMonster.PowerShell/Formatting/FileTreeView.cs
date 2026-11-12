using System;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation;
using System.Runtime.InteropServices;
using ConsoleMonster.Layout;
using ConsoleMonster.Presenters;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    // Get-ChildItem -Recurse (or any listing spanning several folders under one root) as one tree instead of a table per
    // folder: folders first, each with the size of the files listed under it, files with their size and last write time.
    internal static class FileTreeView {
        // Beyond this many entries the per-folder tables stay: a tree that long is no easier to read and costs more to lay out.
        internal const int MaximumEntries = 5000;

        private sealed class Entry {
            internal Entry(string name, FileSystemInfo? item) { Name = name; Item = item; }
            internal string Name { get; }
            internal FileSystemInfo? Item { get; set; }
            internal bool IsFolder => Item == null || Item is DirectoryInfo;
            internal List<Entry> Children { get; } = new List<Entry>();
            internal long ListedBytes { get; set; }
            internal bool HasListedFiles { get; set; }
        }

        /// <summary>The tree for a recursive listing, or null when the items are one folder's listing (the table stays).</summary>
        internal static IWidget? Build(IReadOnlyList<PSObject> items) {
            if (items.Count < 2 || items.Count > MaximumEntries) { return null; }
            List<FileSystemInfo> entries = new List<FileSystemInfo>(items.Count);
            HashSet<string> parents = new HashSet<string>(PathComparer);
            foreach (PSObject item in items) {
                if (!(item.BaseObject is FileSystemInfo info)) { return null; }
                string? parent = Parent(info);
                if (parent == null) { return null; }
                parents.Add(parent);
                entries.Add(info);
            }
            if (parents.Count < 2) { return null; }
            string? root = CommonRoot(parents);
            if (root == null) { return null; }

            Dictionary<string, Entry> folders = new Dictionary<string, Entry>(PathComparer);
            Entry top = new Entry(root, Unlisted(root));
            folders[Trim(root)] = top;
            foreach (FileSystemInfo info in entries) {
                Entry parent = Folder(folders, Parent(info)!, root);
                if (info is DirectoryInfo && folders.TryGetValue(Trim(info.FullName), out Entry? known)) {
                    known.Item = info;
                    continue;
                }
                Entry entry = new Entry(info.Name, info);
                parent.Children.Add(entry);
                if (info is DirectoryInfo) { folders[Trim(info.FullName)] = entry; }
            }
            Total(top);

            // The path heads the view as "Directory: ..." does over a folder's table; the root row is the folder's name.
            string name = Path.GetFileName(Trim(root));
            Tree tree = new Tree("[accent]" + Markup.Escape(name.Length == 0 ? root : name + "/") + "[/]") { TruncateLabelsInMiddle = true, ValuePlacement = TreeValuePlacement.AfterLabel };
            if (top.HasListedFiles) { tree.Root.Values["Size"] = TableCell.FromValue(top.ListedBytes); }
            if (top.Item != null) { tree.Root.Values["LastWriteTime"] = TableCell.FromValue(top.Item.LastWriteTime); }
            tree.ValueColumns.Add(new TableColumn("Size") { Presenter = CellPresenters.Bytes, Alignment = HorizontalAlignment.Right, Priority = 9 });
            // Priority 1: a narrow terminal drops the date rather than wrapping it under each name.
            tree.ValueColumns.Add(new TableColumn("LastWriteTime") { Presenter = CellPresenters.Date, Priority = 1 });
            foreach (Entry child in Ordered(top.Children)) { tree.Root.Add(Node(child)); }
            return new Rows(new TextBlock(" Directory: " + Markup.Escape(root)) { IsMarkup = true, Role = ThemeToken.Title, Overflow = TextOverflow.Ellipsis }, tree);
        }

        private static TreeNode Node(Entry entry) {
            TreeNode node = new TreeNode(Label(entry));
            if (entry.IsFolder) {
                if (entry.HasListedFiles) { node.Values["Size"] = TableCell.FromValue(entry.ListedBytes); }
            } else if (entry.Item is FileInfo file && !IsLink(file)) {
                node.Values["Size"] = TableCell.FromValue(file.Length);
            }
            if (entry.Item != null) { node.Values["LastWriteTime"] = TableCell.FromValue(entry.Item.LastWriteTime); }
            foreach (Entry child in Ordered(entry.Children)) { node.Add(Node(child)); }
            return node;
        }

        // The same cues as the Unix listing: folders in the accent colour, links in the second accent, executables as pass,
        // and dot files muted. Names stay literal.
        private static string Label(Entry entry) {
            string name = Markup.Escape(entry.Name);
            FileSystemInfo? item = entry.Item;
            if (item != null && IsLink(item)) { return "[accent2]" + name + "[/]"; }
            if (entry.IsFolder) { return "[accent]" + name + "/[/]"; }
            if (entry.Name.StartsWith(".", StringComparison.Ordinal)) { return "[muted]" + name + "[/]"; }
            if (item != null && IsExecutable(item)) { return "[pass]" + name + "[/]"; }
            return name;
        }

        // Folders first, then files, each in the order they arrived (Get-ChildItem's own order).
        private static IEnumerable<Entry> Ordered(List<Entry> children) {
            foreach (Entry child in children) { if (child.IsFolder) { yield return child; } }
            foreach (Entry child in children) { if (!child.IsFolder) { yield return child; } }
        }

        private static void Total(Entry folder) {
            foreach (Entry child in folder.Children) {
                if (child.IsFolder) {
                    Total(child);
                    if (child.HasListedFiles) { folder.ListedBytes += child.ListedBytes; folder.HasListedFiles = true; }
                } else if (child.Item is FileInfo file && !IsLink(file)) {
                    folder.ListedBytes += file.Length;
                    folder.HasListedFiles = true;
                }
            }
        }

        // The folder entry for a path under the root, adding the folders between them that were not listed themselves
        // (Get-ChildItem -Recurse -File lists no folders).
        private static Entry Folder(Dictionary<string, Entry> folders, string path, string root) {
            string key = Trim(path);
            if (folders.TryGetValue(key, out Entry? entry)) { return entry; }
            string? parentPath = Path.GetDirectoryName(key);
            Entry parent = parentPath == null || Trim(parentPath).Length < Trim(root).Length ? folders[Trim(root)] : Folder(folders, parentPath, root);
            entry = new Entry(Path.GetFileName(key), Unlisted(key));
            parent.Children.Add(entry);
            folders[key] = entry;
            return entry;
        }

        // A folder between the root and a listed item that was not listed itself; its time is read only when it exists.
        private static DirectoryInfo? Unlisted(string path) {
            try { DirectoryInfo folder = new DirectoryInfo(path); return folder.Exists ? folder : null; }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException || ex is System.Security.SecurityException) { return null; }
        }

        private static string? Parent(FileSystemInfo info) {
            try { return Path.GetDirectoryName(Trim(info.FullName)); } catch (ArgumentException) { return null; } catch (PathTooLongException) { return null; }
        }

        private static string? CommonRoot(IEnumerable<string> paths) {
            string? root = null;
            foreach (string path in paths) {
                string candidate = Trim(path);
                if (root == null) { root = candidate; continue; }
                while (root != null && !IsWithin(candidate, root)) { root = Path.GetDirectoryName(root); }
                if (root == null) { return null; }
            }
            return root;
        }

        private static bool IsWithin(string path, string root) =>
            path.Equals(root, PathComparison) || path.StartsWith(root.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal) ? root : root + Path.DirectorySeparatorChar, PathComparison);

        private static string Trim(string path) {
            string trimmed = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            // A drive or file system root keeps its separator (C:\, /).
            return trimmed.Length == 0 || trimmed.EndsWith(":", StringComparison.Ordinal) ? path.Substring(0, Math.Min(path.Length, trimmed.Length + 1)) : trimmed;
        }

        private static bool IsLink(FileSystemInfo info) => (info.Attributes & FileAttributes.ReparsePoint) != 0;

        private static bool IsExecutable(FileSystemInfo info) {
            if (!(info is FileInfo) || RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) { return false; }
#if NET7_0_OR_GREATER
            try { return (info.UnixFileMode & (UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute)) != 0; }
            catch (IOException) { return false; }
#else
            return false;
#endif
        }

        private static readonly bool Windows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        private static StringComparison PathComparison => Windows ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        private static StringComparer PathComparer => Windows ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
    }
}
