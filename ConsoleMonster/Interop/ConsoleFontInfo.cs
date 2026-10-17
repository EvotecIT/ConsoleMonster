using System.Runtime.InteropServices;

namespace ConsoleMonster.Interop {
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct ConsoleFontInfo {
        internal uint Size;
        internal uint Font;
        internal ConsoleCoordinate FontSize;
        internal uint FontFamily;
        internal uint FontWeight;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        internal string FaceName;
    }
}
