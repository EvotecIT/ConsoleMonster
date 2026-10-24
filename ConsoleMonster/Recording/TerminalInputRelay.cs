using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace ConsoleMonster.Sessions {
    /// <summary>Temporarily reads raw terminal bytes without a blocking background input thread.</summary>
    internal sealed class TerminalInputRelay : IDisposable {
        private readonly bool _windows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        private readonly Decoder _decoder = Encoding.UTF8.GetDecoder();
        private IntPtr _input;
        private IntPtr _termios;
        private IntPtr _records;
        private uint _mode, _codePage;
        private bool _disposed;
        /// <summary>Enters raw input mode on the current physical terminal. Dispose restores its exact prior mode.</summary>
        public TerminalInputRelay() : this(false) { }
        /// <summary>Optionally retains the child's output newline processing while reading raw input.</summary>
        public TerminalInputRelay(bool preserveOutputProcessing) {
            if (Console.IsInputRedirected) { throw new InvalidOperationException("Raw input needs a physical terminal."); }
            if (_windows) {
                _input = GetStdHandle(-10);
                if (!GetConsoleMode(_input, out _mode)) { throw Error(); }
                _codePage = GetConsoleCP();
                _records = Marshal.AllocHGlobal(20);
                if (!SetConsoleMode(_input, (_mode & ~(1u | 2u | 4u | 64u)) | 128u | 512u)) { Marshal.FreeHGlobal(_records); throw Error(); }
                SetConsoleCP(65001);
            } else {
                _termios = Marshal.AllocHGlobal(256);
                IntPtr raw = Marshal.AllocHGlobal(256);
                try {
                    if (tcgetattr(0, _termios) != 0) { throw Error(); }
                    byte[] copy = new byte[256]; Marshal.Copy(_termios, copy, 0, copy.Length); Marshal.Copy(copy, 0, raw, copy.Length);
                    cfmakeraw(raw);
                    if (preserveOutputProcessing) {
                        // cfmakeraw also clears c_oflag. A demo child still needs ONLCR so native programs and
                        // Console.WriteLine advance to column zero. Darwin uses 64-bit tcflag_t; Linux uses 32-bit.
                        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) { Marshal.WriteInt64(raw, 8, Marshal.ReadInt64(_termios, 8)); }
                        else { Marshal.WriteInt32(raw, 4, Marshal.ReadInt32(_termios, 4)); }
                    }
                    if (tcsetattr(0, 0, raw) != 0) { throw Error(); }
                } catch { Marshal.FreeHGlobal(_termios); _termios = IntPtr.Zero; throw; }
                finally { Marshal.FreeHGlobal(raw); }
            }
        }
        /// <summary>Returns currently available UTF-8 terminal input, including escape sequences, or an empty string.</summary>
        public string ReadAvailable() {
            if (_disposed) { throw new ObjectDisposedException(nameof(TerminalInputRelay)); }
            byte[] bytes = new byte[4096]; int count;
            if (_windows) {
                // Modifier, resize and key-up events do not make ReadFile return. Consume those before entering it.
                while (true) {
                    if (!PeekConsoleInputW(_input, _records, 1, out uint events)) { throw Error(); }
                    if (events == 0) { return string.Empty; }
                    short kind = Marshal.ReadInt16(_records, 0);
                    bool down = Marshal.ReadInt32(_records, 4) != 0;
                    short key = Marshal.ReadInt16(_records, 10), character = Marshal.ReadInt16(_records, 14);
                    if (kind == 1 && down && (character != 0 || (key >= 0x21 && key <= 0x2e) || (key >= 0x70 && key <= 0x87))) { break; }
                    if (!ReadConsoleInputW(_input, _records, 1, out _)) { throw Error(); }
                }
                if (!ReadFile(_input, bytes, bytes.Length, out count, IntPtr.Zero)) { throw Error(); }
            } else {
                PollFd descriptor = new PollFd { File = 0, Events = 1 };
                int ready;
                do { ready = poll(ref descriptor, (UIntPtr)1, 0); }
                while (ready < 0 && Marshal.GetLastWin32Error() == 4);
                if (ready < 0) { throw Error(); }
                if ((descriptor.Returned & 32) != 0) { throw new IOException("The terminal input descriptor is no longer valid."); }
                if (ready == 0 || (descriptor.Returned & (1 | 8 | 16)) == 0) { return string.Empty; }
                do { count = read(0, bytes, (UIntPtr)bytes.Length).ToInt32(); }
                while (count < 0 && Marshal.GetLastWin32Error() == 4);
                if (count < 0) { throw Error(); }
            }
            char[] chars = new char[Encoding.UTF8.GetMaxCharCount(4096)];
            return new string(chars, 0, _decoder.GetChars(bytes, 0, count, chars, 0, false));
        }
        /// <summary>Restores the terminal mode and code page without changing other terminals.</summary>
        public void Dispose() {
            if (_disposed) { return; } _disposed = true;
            if (_windows) { SetConsoleMode(_input, _mode); SetConsoleCP(_codePage); Marshal.FreeHGlobal(_records); }
            else if (_termios != IntPtr.Zero) { tcsetattr(0, 0, _termios); Marshal.FreeHGlobal(_termios); }
        }
        private static Win32Exception Error() => new Win32Exception(Marshal.GetLastWin32Error());
        [StructLayout(LayoutKind.Sequential)] private struct PollFd { public int File; public short Events, Returned; }
        [DllImport("kernel32.dll")] private static extern IntPtr GetStdHandle(int handle);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool GetConsoleMode(IntPtr handle, out uint mode);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool SetConsoleMode(IntPtr handle, uint mode);
        [DllImport("kernel32.dll")] private static extern uint GetConsoleCP();
        [DllImport("kernel32.dll")] private static extern bool SetConsoleCP(uint page);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool PeekConsoleInputW(IntPtr handle, IntPtr records, uint count, out uint read);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool ReadConsoleInputW(IntPtr handle, IntPtr records, uint count, out uint read);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool ReadFile(IntPtr handle, byte[] bytes, int count, out int read, IntPtr overlapped);
        [DllImport("libc", SetLastError = true)] private static extern int tcgetattr(int fd, IntPtr termios);
        [DllImport("libc", SetLastError = true)] private static extern int tcsetattr(int fd, int actions, IntPtr termios);
        [DllImport("libc")] private static extern void cfmakeraw(IntPtr termios);
        [DllImport("libc", SetLastError = true)] private static extern int poll(ref PollFd descriptors, UIntPtr count, int timeout);
        [DllImport("libc", SetLastError = true)] private static extern IntPtr read(int fd, byte[] bytes, UIntPtr count);
    }
}
