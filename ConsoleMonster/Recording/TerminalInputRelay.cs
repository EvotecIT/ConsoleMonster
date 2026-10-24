using System;
using System.ComponentModel;
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
        public TerminalInputRelay() {
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
                if (poll(ref descriptor, (UIntPtr)1, 0) <= 0 || (descriptor.Returned & 1) == 0) { return string.Empty; }
                count = read(0, bytes, (UIntPtr)bytes.Length).ToInt32();
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
