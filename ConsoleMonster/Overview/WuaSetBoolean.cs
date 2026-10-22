using System;
using System.Runtime.InteropServices;

namespace ConsoleMonster.Overview {
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    internal delegate int WuaSetBoolean(IntPtr instance, short value);
}
