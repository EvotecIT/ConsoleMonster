using System.Runtime.InteropServices;

namespace ConsoleMonster.Interop {
    [StructLayout(LayoutKind.Sequential)]
    internal struct ConsoleRectangle {
        internal short Left;
        internal short Top;
        internal short Right;
        internal short Bottom;

        internal bool TryGetSize(out int width, out int height) {
            return TryComputeSize(Left, Top, Right, Bottom, out width, out height);
        }

        internal static bool TryComputeSize(int left, int top, int right, int bottom, out int width, out int height) {
            width = right - left + 1;
            height = bottom - top + 1;
            return width > 0 && height > 0;
        }
    }
}
