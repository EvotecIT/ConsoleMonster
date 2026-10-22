using System;
using System.Runtime.InteropServices;

namespace ConsoleMonster.Interop {
    // INPUT_RECORD: a 2-byte event type, 2 bytes of padding and a 16-byte union. Only the members the reader uses are mapped.
    [StructLayout(LayoutKind.Explicit, Size = 20)]
    internal struct ConsoleInputRecord {
        [FieldOffset(0)] public ushort EventType;
        // KEY_EVENT_RECORD
        [FieldOffset(4)] public int KeyDown;
        [FieldOffset(8)] public ushort RepeatCount;
        [FieldOffset(10)] public ushort VirtualKeyCode;
        [FieldOffset(12)] public ushort VirtualScanCode;
        [FieldOffset(14)] public ushort UnicodeChar;
        [FieldOffset(16)] public uint ControlKeyState;
        // MOUSE_EVENT_RECORD (the modifier state is at a different offset from KEY_EVENT_RECORD).
        [FieldOffset(4)] public short MouseX;
        [FieldOffset(6)] public short MouseY;
        [FieldOffset(8)] public uint MouseButtonState;
        [FieldOffset(12)] public uint MouseControlKeyState;
        [FieldOffset(16)] public uint MouseEventFlags;
        // WINDOW_BUFFER_SIZE_RECORD shares offset 4 (a COORD); FOCUS_EVENT_RECORD shares offset 4 (a BOOL).
        [FieldOffset(4)] public short SizeX;
        [FieldOffset(6)] public short SizeY;
        [FieldOffset(4)] public int SetFocus;
    }

    internal static class ConsoleInputNative {
        internal const ushort KeyEvent = 0x0001;
        internal const ushort MouseEvent = 0x0002;
        internal const ushort WindowBufferSizeEvent = 0x0004;
        internal const ushort FocusEvent = 0x0010;

        internal const uint EnableProcessedInput = 0x0001;
        internal const uint EnableLineInput = 0x0002;
        internal const uint EnableEchoInput = 0x0004;
        internal const uint EnableWindowInput = 0x0008;
        internal const uint EnableMouseInput = 0x0010;
        internal const uint EnableQuickEditMode = 0x0040;
        internal const uint EnableExtendedFlags = 0x0080;
        internal const uint EnableVirtualTerminalInput = 0x0200;

        internal const uint RightAlt = 0x0001;
        internal const uint LeftAlt = 0x0002;
        internal const uint RightCtrl = 0x0004;
        internal const uint LeftCtrl = 0x0008;
        internal const uint ShiftPressed = 0x0010;

        private const uint WaitObject0 = 0;

        internal static IntPtr StandardInput => WindowsConsole.IsWindows ? GetStdHandle(-10) : IntPtr.Zero;

        internal static bool TryGetMode(IntPtr handle, out uint mode) => WindowsConsole.TryGetMode(handle, out mode);

        internal static bool TrySetMode(IntPtr handle, uint mode) => WindowsConsole.TrySetMode(handle, mode);

        // Returns true when input is waiting; false on timeout or failure.
        internal static bool Wait(IntPtr handle, int milliseconds) {
            return WaitForSingleObject(handle, (uint)Math.Max(0, milliseconds)) == WaitObject0;
        }

        internal static int PendingCount(IntPtr handle) {
            return GetNumberOfConsoleInputEvents(handle, out uint count) ? (int)count : 0;
        }

        internal static int Read(IntPtr handle, ConsoleInputRecord[] buffer) {
            return ReadConsoleInputW(handle, buffer, (uint)buffer.Length, out uint read) ? (int)read : -1;
        }

        internal static bool Write(IntPtr handle, ConsoleInputRecord[] records, int count) {
            return WriteConsoleInputW(handle, records, (uint)count, out _);
        }

        internal static bool Flush(IntPtr handle) => FlushConsoleInputBuffer(handle);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool FlushConsoleInputBuffer(IntPtr handle);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetStdHandle(int standardHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint WaitForSingleObject(IntPtr handle, uint milliseconds);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetNumberOfConsoleInputEvents(IntPtr handle, out uint count);

        [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ReadConsoleInputW(IntPtr handle, [Out] ConsoleInputRecord[] buffer, uint length, out uint read);

        [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool WriteConsoleInputW(IntPtr handle, [In] ConsoleInputRecord[] buffer, uint length, out uint written);
    }
}
