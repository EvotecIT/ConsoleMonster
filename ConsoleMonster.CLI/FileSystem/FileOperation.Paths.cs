using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace ConsoleMonster.FileSystem {
    public sealed partial class FileOperation {
        // Compare directory entries, rather than assuming that an entire operating system ignores case.
        // The final component stays unresolved: two different links to one file are still different entries.
        internal static bool SamePath(string left, string right) {
            left = Full(left);
            right = Full(right);
            if (string.Equals(left, right, StringComparison.Ordinal)) { return true; }
            string? leftParent = Path.GetDirectoryName(left), rightParent = Path.GetDirectoryName(right);
            if (leftParent == null || rightParent == null) { return false; }
            string parent = PhysicalPath(leftParent);
            if (!string.Equals(parent, PhysicalPath(rightParent), StringComparison.Ordinal)) { return false; }
            string leftName = Path.GetFileName(left), rightName = Path.GetFileName(right);
            if (leftName == rightName) { return true; }
            if (!string.Equals(leftName, rightName, StringComparison.OrdinalIgnoreCase) ||
                !Exists(left) || !Exists(right)) { return false; }
            // On a case-sensitive directory both spellings can exist. On an insensitive directory the
            // lookup succeeds twice, but enumeration contains only the one actual spelling.
            bool foundLeft = false, foundRight = false;
            foreach (string entry in Directory.EnumerateFileSystemEntries(parent)) {
                string name = Path.GetFileName(entry);
                foundLeft |= name == leftName;
                foundRight |= name == rightName;
            }
            return !(foundLeft && foundRight);
        }

        internal static bool IsInside(string path, string folder) {
            string root = PhysicalPath(folder);
            string full = PhysicalPath(path);
            return full.Length > root.Length && full.StartsWith(root, StringComparison.Ordinal) &&
                (EndsWithSeparator(root) || full[root.Length] == Path.DirectorySeparatorChar || full[root.Length] == Path.AltDirectorySeparatorChar);
        }

        private static bool Exists(string path) => File.Exists(path) || Directory.Exists(path) || IsLink(path);

        /// <summary>Resolves existing ancestors before appending a missing destination tail. Fails before writing if resolution fails.</summary>
        private static string PhysicalPath(string path) {
            string existing = Full(path);
            Stack<string> tail = new Stack<string>();
            while (!Exists(existing)) {
                string? parent = Path.GetDirectoryName(existing);
                if (parent == null || parent == existing) { throw new DirectoryNotFoundException("Cannot resolve '" + path + "'."); }
                tail.Push(Path.GetFileName(existing));
                existing = parent;
            }
            string resolved;
            if (Windows) {
                using SafeFileHandle handle = OpenPath(existing, 0, FileShare.Read | FileShare.Write | FileShare.Delete,
                    IntPtr.Zero, FileMode.Open, 0x02000000, IntPtr.Zero);
                if (handle.IsInvalid) { throw PathError(existing); }
                StringBuilder result = new StringBuilder(512);
                uint length = GetFinalPathNameByHandle(handle, result, (uint)result.Capacity, 0);
                if (length >= result.Capacity) {
                    result.Capacity = checked((int)length + 1);
                    length = GetFinalPathNameByHandle(handle, result, (uint)result.Capacity, 0);
                }
                if (length == 0 || length >= result.Capacity) { throw PathError(existing); }
                resolved = result.ToString();
                if (resolved.StartsWith(@"\\?\UNC\", StringComparison.Ordinal)) { resolved = @"\\" + resolved.Substring(8); }
                else if (resolved.StartsWith(@"\\?\", StringComparison.Ordinal)) { resolved = resolved.Substring(4); }
            } else {
                IntPtr result = RealPath(existing, IntPtr.Zero);
                if (result == IntPtr.Zero) { throw PathError(existing); }
                try { resolved = Marshal.PtrToStringAnsi(result)!; }
                finally { FreePath(result); }
            }
            while (tail.Count != 0) { resolved = Path.Combine(resolved, tail.Pop()); }
            return Full(resolved);
        }

        private static IOException PathError(string path) => new IOException("Cannot resolve '" + path + "'.", new Win32Exception(Marshal.GetLastWin32Error()));

        [DllImport("kernel32.dll", EntryPoint = "CreateFileW", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern SafeFileHandle OpenPath(string path, uint access, FileShare share, IntPtr security, FileMode mode, uint flags, IntPtr template);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern uint GetFinalPathNameByHandle(SafeFileHandle handle, StringBuilder path, uint capacity, uint flags);

        [DllImport("libc", EntryPoint = "realpath", SetLastError = true)]
        private static extern IntPtr RealPath(string path, IntPtr buffer);

        [DllImport("libc", EntryPoint = "free")]
        private static extern void FreePath(IntPtr buffer);
    }
}
