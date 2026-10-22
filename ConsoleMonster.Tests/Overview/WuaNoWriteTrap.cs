using System;
using System.Runtime.InteropServices;

namespace ConsoleMonster.Tests.Overview {
    // A wrong slot must never write an interface-sized pointer into the caller's LONG buffer.
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    internal delegate int WuaNoWriteTrap(IntPtr instance, IntPtr destination);
}
