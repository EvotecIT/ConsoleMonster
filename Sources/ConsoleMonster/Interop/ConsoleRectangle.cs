using System.Runtime.InteropServices;

namespace ConsoleMonster.Interop {
    [StructLayout(LayoutKind.Sequential)]
    internal struct ConsoleRectangle {
        internal short Left;
        internal short Top;
        internal short Right;
        internal short Bottom;
    }
}
