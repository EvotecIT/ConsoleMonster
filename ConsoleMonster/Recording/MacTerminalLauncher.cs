using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace ConsoleMonster.Sessions {
    /// <summary>Owns the embedded macOS exec shim and its close-on-exec startup error channel.</summary>
    internal sealed class MacTerminalLauncher : IDisposable {
        private string? _directory;
        private int _read = -1, _write = -1;
        internal string Executable { get; }

        internal MacTerminalLauncher() {
            try {
                byte[] template = Encoding.UTF8.GetBytes(Path.Combine(Path.GetTempPath(), "consolemonster-tty-XXXXXX") + "\0");
                if (mkdtemp(template) == IntPtr.Zero) { throw Error(); }
                _directory = Encoding.UTF8.GetString(template, 0, Array.IndexOf(template, (byte)0));
                Executable = Path.Combine(_directory, "terminal-exec");
                using (Stream source = typeof(MacTerminalLauncher).Assembly.GetManifestResourceStream("ConsoleMonster.Recording.macos-terminal-exec")!)
                using (FileStream file = new FileStream(Executable, FileMode.CreateNew, FileAccess.Write, FileShare.None)) { source.CopyTo(file); }
                if (chmod(Executable, 448) != 0) { throw Error(); } // 0700, within mkdtemp's private directory
                int[] descriptors = new int[2];
                if (pipe(descriptors) != 0) { throw Error(); }
                _read = descriptors[0]; _write = descriptors[1];
            } catch { Dispose(); throw; }
        }

        // Add this after the PTY close actions: descriptor 3 may originally be the master.
        internal void AddFileActions(IntPtr actions, Action<int> closeOnExec) {
            closeOnExec(_read); closeOnExec(_write);
            Check(posix_spawn_file_actions_addclose(actions, _read));
            Check(posix_spawn_file_actions_adddup2(actions, _write, 3));
            if (_write != 3) { Check(posix_spawn_file_actions_addclose(actions, _write)); }
        }

        internal void WaitForExec() {
            close(_write); _write = -1;
            byte[] error = new byte[4]; int offset = 0;
            while (offset < error.Length) {
                PollFd descriptor = new PollFd { File = _read, Events = 1 };
                int ready = poll(ref descriptor, (UIntPtr)1, 10000);
                if (ready < 0 && Marshal.GetLastWin32Error() == 4) { continue; }
                if (ready < 0) { throw Error(); }
                if (ready == 0) { throw new TimeoutException("The macOS terminal launcher did not exec the child."); }
                byte[] next = new byte[error.Length - offset];
                int count = read(_read, next, (UIntPtr)next.Length).ToInt32();
                if (count < 0 && Marshal.GetLastWin32Error() == 4) { continue; }
                if (count < 0) { throw Error(); }
                if (count == 0) {
                    if (offset == 0) { return; }
                    throw new IOException("The macOS terminal launcher returned an incomplete startup error.");
                }
                Buffer.BlockCopy(next, 0, error, offset, count); offset += count;
            }
            throw new Win32Exception(BitConverter.ToInt32(error, 0));
        }

        public void Dispose() {
            if (_read >= 0) { close(_read); _read = -1; }
            if (_write >= 0) { close(_write); _write = -1; }
            if (_directory != null) {
                if (File.Exists(Executable)) { File.Delete(Executable); }
                Directory.Delete(_directory); _directory = null;
            }
        }

        private static Win32Exception Error() => new Win32Exception(Marshal.GetLastWin32Error());
        private static void Check(int error) { if (error != 0) { throw new Win32Exception(error); } }
        [StructLayout(LayoutKind.Sequential)] private struct PollFd { public int File; public short Events, Returned; }
        [DllImport("libc", SetLastError = true)] private static extern IntPtr mkdtemp([In, Out] byte[] template);
        [DllImport("libc", SetLastError = true)] private static extern int chmod(string path, uint mode);
        [DllImport("libc", SetLastError = true)] private static extern int pipe([Out] int[] descriptors);
        [DllImport("libc", SetLastError = true)] private static extern int close(int descriptor);
        [DllImport("libc", SetLastError = true)] private static extern IntPtr read(int fd, byte[] bytes, UIntPtr count);
        [DllImport("libc", SetLastError = true)] private static extern int poll(ref PollFd descriptors, UIntPtr count, int timeout);
        [DllImport("libc")] private static extern int posix_spawn_file_actions_adddup2(IntPtr actions, int fd, int newFd);
        [DllImport("libc")] private static extern int posix_spawn_file_actions_addclose(IntPtr actions, int fd);
    }
}
