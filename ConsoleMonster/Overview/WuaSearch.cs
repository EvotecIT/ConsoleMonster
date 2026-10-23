using System;
using System.Runtime.InteropServices;

namespace ConsoleMonster.Overview {
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    internal delegate int WuaSearch(IntPtr instance, IntPtr criteria, out IntPtr result);
}
