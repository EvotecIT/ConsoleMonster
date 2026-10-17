using System.Runtime.InteropServices;

namespace ConsoleMonster.Interop {
    [StructLayout(LayoutKind.Sequential)]
    internal struct ConsoleCoordinate {
        internal short X;
        internal short Y;
    }
}
