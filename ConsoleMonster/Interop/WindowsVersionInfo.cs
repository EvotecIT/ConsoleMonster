using System.Runtime.InteropServices;

namespace ConsoleMonster.Interop {
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct WindowsVersionInfo {
        internal uint Size;
        internal uint Major;
        internal uint Minor;
        internal uint Build;
        internal uint Platform;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        internal string ServicePack;
    }
}
