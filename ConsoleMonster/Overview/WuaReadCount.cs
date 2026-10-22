using System;
using System.Runtime.InteropServices;

namespace ConsoleMonster.Overview {
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    internal delegate int WuaReadCount(IntPtr instance, out int value);
}
