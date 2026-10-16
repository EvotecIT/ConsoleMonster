using System.Runtime.InteropServices;

namespace ConsoleMonster.Interop {
    /// <summary>
    /// The Win32 INPUT_RECORD layout (20 bytes) with the KEY_EVENT_RECORD fields mapped; other event kinds are carried
    /// through unchanged when records are written back.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 20, CharSet = CharSet.Unicode)]
    internal struct ConsoleInputRecord {
        internal const ushort KeyEventType = 0x0001;

        [FieldOffset(0)]
        internal ushort EventType;

        [FieldOffset(4)]
        internal int KeyDown;

        [FieldOffset(8)]
        internal ushort RepeatCount;

        [FieldOffset(10)]
        internal ushort VirtualKeyCode;

        [FieldOffset(12)]
        internal ushort VirtualScanCode;

        [FieldOffset(14)]
        internal char UnicodeChar;

        [FieldOffset(16)]
        internal uint ControlKeyState;

        internal bool IsKey => EventType == KeyEventType;

        internal bool IsKeyDown => EventType == KeyEventType && KeyDown != 0;
    }
}
