using System.Runtime.InteropServices;

namespace ConsoleMonster.Interop {
    [StructLayout(LayoutKind.Sequential)]
    internal struct ConsoleScreenBufferInfoEx {
        internal uint Size;
        internal ConsoleCoordinate BufferSize;
        internal ConsoleCoordinate CursorPosition;
        internal ushort Attributes;
        internal ConsoleRectangle Window;
        internal ConsoleCoordinate MaximumWindowSize;
        internal ushort PopupAttributes;
        [MarshalAs(UnmanagedType.Bool)]
        internal bool FullscreenSupported;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        internal uint[] ColorTable;
    }
}
