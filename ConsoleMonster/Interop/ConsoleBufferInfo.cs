using System.Runtime.InteropServices;

namespace ConsoleMonster.Interop {
    [StructLayout(LayoutKind.Sequential)]
    internal struct ConsoleBufferInfo {
        internal ConsoleCoordinate Size;
        internal ConsoleCoordinate CursorPosition;
        internal ushort Attributes;
        internal ConsoleRectangle Window;
        internal ConsoleCoordinate MaximumWindowSize;
    }
}
