using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace ConsoleMonster.Sessions {
    /// <summary>openpty plus posix_spawn avoids running managed code after fork in a multithreaded CLR.</summary>
    internal sealed class UnixPseudoTerminal : IPseudoTerminal {
        private int _master = -1;
        private int _slave = -1;
        private int _pid;
        private readonly object _processGate = new object();
        private bool _exited;
        private int _exitCode;
        private readonly bool _mac = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        private readonly bool _macArm64 = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) && RuntimeInformation.ProcessArchitecture == Architecture.Arm64;
        public UnixPseudoTerminal(SessionRecordingOptions options) {
            WinSize size = new WinSize(options.Width, options.Height);
            int slave = -1;
            IntPtr actions = Marshal.AllocHGlobal(1024), attributes = Marshal.AllocHGlobal(1024);
            bool actionsReady = false, attributesReady = false;
            List<IntPtr> allocations = new List<IntPtr>();
            MacTerminalLauncher? launcher = null;
            try {
                int opened = _mac ? OpenMac(out _master, out slave, IntPtr.Zero, IntPtr.Zero, ref size) : OpenLinux(out _master, out slave, IntPtr.Zero, IntPtr.Zero, ref size);
                if (opened != 0) { throw Error(); }
                // Both parent descriptors must close on exec; dup2 actions create the child's standard handles.
                CloseOnExec(_master); CloseOnExec(slave);
                Check(posix_spawn_file_actions_init(actions)); actionsReady = true;
                Check(posix_spawnattr_init(attributes)); attributesReady = true;
                byte[] tty = new byte[1024];
                Check(ttyname_r(slave, tty, (UIntPtr)tty.Length));
                string ttyPath = Encoding.UTF8.GetString(tty, 0, Array.IndexOf(tty, (byte)0));
                // Linux acquires its controlling tty on open after setsid. The macOS exec shim
                // explicitly calls setsid + TIOCSCTTY once it is outside the CLR.
                Check(posix_spawn_file_actions_addopen(actions, 0, ttyPath, 2, 0));
                for (int fd = 1; fd <= 2; fd++) { Check(posix_spawn_file_actions_adddup2(actions, 0, fd)); }
                Check(posix_spawn_file_actions_addclose(actions, _master));
                Check(posix_spawn_file_actions_addclose(actions, slave));
                Check(posix_spawnattr_setflags(attributes, (short)(_mac ? 0 : 0x80))); // Linux POSIX_SPAWN_SETSID
                string shell = options.Shell ?? UnixChildEnvironment.DefaultShell(Environment.GetEnvironmentVariable("SHELL"), _mac);
                List<string> args = new List<string>();
                if (_mac) { launcher = new MacTerminalLauncher(); launcher.AddFileActions(actions, CloseOnExec); args.Add(launcher.Executable); }
                args.Add(shell);
                foreach (string arg in options.Arguments) { args.Add(arg); }
                List<string> env = UnixChildEnvironment.Build(options, Environment.GetEnvironmentVariables(), _mac);
                // chdir is a child-only spawn action; never changes the process-wide working directory.
                Check(posix_spawn_file_actions_addchdir_np(actions, options.WorkingDirectory));
                int result = posix_spawnp(out _pid, launcher?.Executable ?? shell, actions, attributes, Strings(args, allocations), Strings(env, allocations));
                Check(result);
                launcher?.WaitForExec();
                launcher?.Dispose(); launcher = null;
                // Keep the replica open until the reader drains the child. Darwin can discard unread terminal
                // output on the last replica close, including when a fast child exits before the reader starts.
                _slave = slave;
                slave = -1;
            } catch {
                if (_pid > 0) { kill(-_pid, 9); kill(_pid, 9); while (waitpid(_pid, out _, 0) < 0 && Marshal.GetLastWin32Error() == 4) { } _pid = 0; }
                if (_master >= 0) { close(_master); _master = -1; }
                throw;
            } finally {
                if (slave >= 0) { close(slave); }
                if (actionsReady) { posix_spawn_file_actions_destroy(actions); }
                if (attributesReady) { posix_spawnattr_destroy(attributes); }
                Marshal.FreeHGlobal(actions); Marshal.FreeHGlobal(attributes);
                foreach (IntPtr pointer in allocations) { Marshal.FreeHGlobal(pointer); }
                launcher?.Dispose();
            }
        }
        private static IntPtr Strings(IList<string> values, List<IntPtr> allocations) {
            IntPtr array = Marshal.AllocHGlobal((values.Count + 1) * IntPtr.Size); allocations.Add(array);
            for (int i = 0; i < values.Count; i++) {
                byte[] bytes = Encoding.UTF8.GetBytes(values[i] + "\0");
                IntPtr value = Marshal.AllocHGlobal(bytes.Length); allocations.Add(value);
                Marshal.Copy(bytes, 0, value, bytes.Length); Marshal.WriteIntPtr(array, i * IntPtr.Size, value);
            }
            Marshal.WriteIntPtr(array, values.Count * IntPtr.Size, IntPtr.Zero);
            return array;
        }
        public int Read(byte[] bytes) => ReadUntilOutputOrExit(WaitReadable, () => HasExited, () => ReadNative(bytes));
        // Keep ordering separate from the native calls so the empty-poll/child-exit race is deterministic to test.
        internal static int ReadUntilOutputOrExit(Func<int, bool> waitReadable, Func<bool> hasExited, Func<int> readOutput) {
            bool exited = false;
            while (true) {
                if (waitReadable(exited ? 0 : 20)) { return readOutput(); }
                // Only an empty readiness observation after exit proves the queue has been drained.
                if (exited) { return 0; }
                exited = hasExited();
            }
        }
        private bool WaitReadable(int timeout) {
            while (true) {
                PollFd descriptor = new PollFd { File = _master, Events = 1 };
                int ready = poll(ref descriptor, (UIntPtr)1, timeout);
                if (ready < 0) {
                    if (Marshal.GetLastWin32Error() == 4) { continue; }
                    throw Error();
                }
                return ready > 0;
            }
        }
        private int ReadNative(byte[] bytes) {
            while (true) {
                long count = read(_master, bytes, (UIntPtr)bytes.Length).ToInt64();
                if (count >= 0) { return (int)count; }
                int error = Marshal.GetLastWin32Error();
                if (error == 4) { continue; }
                if (error == 5 || error == 9) { return 0; } // terminal EOF / closed
                throw Error();
            }
        }
        public void Write(byte[] bytes) {
            int offset = 0;
            while (offset < bytes.Length) {
                byte[] remaining = offset == 0 ? bytes : Slice(bytes, offset);
                long count = write(_master, remaining, (UIntPtr)remaining.Length).ToInt64();
                if (count < 0 && Marshal.GetLastWin32Error() == 4) { continue; }
                if (count <= 0) { throw Error(); }
                offset += (int)count;
            }
        }
        private static byte[] Slice(byte[] bytes, int offset) { byte[] copy = new byte[bytes.Length - offset]; Buffer.BlockCopy(bytes, offset, copy, 0, copy.Length); return copy; }
        public void Resize(int width, int height) {
            WinSize size = new WinSize(width, height);
            UIntPtr request = _mac ? (UIntPtr)0x80087467 : (UIntPtr)0x5414;
            int result = _macArm64 ? IoctlMacArm64(_master, request, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, ref size)
                : ioctl(_master, request, ref size);
            if (result != 0) { throw Error(); }
        }
        private void CloseOnExec(int descriptor) {
            int result = _macArm64 ? FcntlMacArm64(descriptor, 2, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 1)
                : fcntl(descriptor, 2, 1);
            if (result != 0) { throw Error(); }
        }
        public bool HasExited {
            get {
                lock (_processGate) {
                    if (!_exited && _pid > 0) {
                        byte[] info = new byte[128];
                        int flags = 1 | 4 | (_mac ? 0x20 : 0x01000000); // WNOHANG | WEXITED | WNOWAIT
                        int result = waitid(1, _pid, info, flags);
                        if (result != 0 && Marshal.GetLastWin32Error() != 4) { throw Error(); }
                        if (result == 0) { _exited = TryGetExitCode(info, _mac, IntPtr.Size, _pid, out _exitCode); }
                    }
                    return _exited;
                }
            }
        }
        internal static bool TryGetExitCode(byte[] info, bool mac, int pointerSize, int pid, out int exitCode) {
            exitCode = 0;
            int pidOffset = mac || pointerSize == 4 ? 12 : 16;
            if (BitConverter.ToInt32(info, pidOffset) != pid) { return false; }
            int code = BitConverter.ToInt32(info, 8), status = BitConverter.ToInt32(info, pidOffset + 8);
            exitCode = code == 1 ? status : 128 + status;
            return true;
        }
        public int ExitCode => _exitCode;
        public void Stop() { lock (_processGate) { if (_pid > 0) { kill(-_pid, 15); kill(_pid, 15); } } }
        public void Complete() {
            lock (_processGate) {
                // WNOWAIT reserves the leader PID until teardown, so this process-group identity cannot be reused.
                if (_pid > 0) { kill(-_pid, 9); kill(_pid, 9); }
            }
        }
        public void Dispose() {
            lock (_processGate) {
                Complete();
                if (_pid > 0) { while (waitpid(_pid, out _, 0) < 0 && Marshal.GetLastWin32Error() == 4) { } _pid = 0; _exited = true; }
                if (_master >= 0) { close(_master); _master = -1; }
                if (_slave >= 0) { close(_slave); _slave = -1; }
            }
        }
        private static Win32Exception Error() => new Win32Exception(Marshal.GetLastWin32Error());
        private static void Check(int error) { if (error != 0) { throw new Win32Exception(error); } }
        [StructLayout(LayoutKind.Sequential)] private struct WinSize { public ushort Rows, Columns, X, Y; public WinSize(int width, int height) { Rows = (ushort)height; Columns = (ushort)width; X = Y = 0; } }
        [DllImport("libutil.so.1", EntryPoint = "openpty", SetLastError = true)] private static extern int OpenLinux(out int master, out int slave, IntPtr name, IntPtr termios, ref WinSize size);
        [DllImport("/usr/lib/libSystem.B.dylib", EntryPoint = "openpty", SetLastError = true)] private static extern int OpenMac(out int master, out int slave, IntPtr name, IntPtr termios, ref WinSize size);
        [DllImport("libc", SetLastError = true)] private static extern int fcntl(int fd, int command, int value);
        [DllImport("libc")] private static extern int ttyname_r(int fd, byte[] name, UIntPtr size);
        [DllImport("libc", SetLastError = true)] private static extern IntPtr read(int fd, byte[] buffer, UIntPtr count);
        [DllImport("libc", SetLastError = true)] private static extern IntPtr write(int fd, byte[] buffer, UIntPtr count);
        [DllImport("libc", SetLastError = true)] private static extern int close(int fd);
        [DllImport("libc", SetLastError = true)] private static extern int ioctl(int fd, UIntPtr request, ref WinSize size);
        // Apple's arm64 variadic ABI reads the third argument from the stack, not x2. Six unused register
        // slots put the real variadic value in the first stack slot without a new native shim or runtime dependency.
        [DllImport("libc", EntryPoint = "ioctl", SetLastError = true)] private static extern int IoctlMacArm64(int fd, UIntPtr request, IntPtr x2, IntPtr x3, IntPtr x4, IntPtr x5, IntPtr x6, IntPtr x7, ref WinSize size);
        [DllImport("libc", EntryPoint = "fcntl", SetLastError = true)] private static extern int FcntlMacArm64(int fd, int command, IntPtr x2, IntPtr x3, IntPtr x4, IntPtr x5, IntPtr x6, IntPtr x7, int flags);
        [StructLayout(LayoutKind.Sequential)] private struct PollFd { public int File; public short Events, Returned; }
        [DllImport("libc", SetLastError = true)] private static extern int poll(ref PollFd descriptors, UIntPtr count, int timeout);
        [DllImport("libc", SetLastError = true)] private static extern int kill(int pid, int signal);
        [DllImport("libc", SetLastError = true)] private static extern int waitpid(int pid, out int status, int options);
        [DllImport("libc", SetLastError = true)] private static extern int waitid(int type, int pid, byte[] info, int options);
        [DllImport("libc")] private static extern int posix_spawn_file_actions_init(IntPtr actions);
        [DllImport("libc")] private static extern int posix_spawn_file_actions_destroy(IntPtr actions);
        [DllImport("libc")] private static extern int posix_spawn_file_actions_adddup2(IntPtr actions, int fd, int newFd);
        [DllImport("libc", CharSet = CharSet.Ansi)] private static extern int posix_spawn_file_actions_addopen(IntPtr actions, int fd, string path, int flags, uint mode);
        [DllImport("libc")] private static extern int posix_spawn_file_actions_addclose(IntPtr actions, int fd);
        [DllImport("libc", CharSet = CharSet.Ansi)] private static extern int posix_spawn_file_actions_addchdir_np(IntPtr actions, string directory);
        [DllImport("libc")] private static extern int posix_spawnattr_init(IntPtr attributes);
        [DllImport("libc")] private static extern int posix_spawnattr_destroy(IntPtr attributes);
        [DllImport("libc")] private static extern int posix_spawnattr_setflags(IntPtr attributes, short flags);
        [DllImport("libc", CharSet = CharSet.Ansi)] private static extern int posix_spawnp(out int pid, string file, IntPtr actions, IntPtr attributes, IntPtr args, IntPtr environment);
    }
}
