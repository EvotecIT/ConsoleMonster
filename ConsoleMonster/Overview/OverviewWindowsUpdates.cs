using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace ConsoleMonster.Overview {
    // A small, raw binding to the installed WUA cache API, avoiding reflection, RCWs and a COM framework.
    // Slots include IUnknown/IDispatch and follow Microsoft's wuapi.h at win32metadata commit
    // 6d6617a8eee57d7c4f6c55d8c23e35064ea69b9e. Property get/put methods each occupy a slot.
    internal static class OverviewWindowsUpdates {
        private const int SearcherSetAutoUpgrade = 8;
        private const int SearcherSearch = 19;
        private const int SearcherSetOnline = 21;
        private const int SearchResultCode = 7;
        private const int SearchResultUpdates = 9;
        private const int UpdateCollectionCount = 10; // slot 9 is get__NewEnum(IUnknown**), not LONG*.

        internal static int? PendingCount(CancellationToken token) {
            token.ThrowIfCancellationRequested();
            int initialized = CoInitializeEx(IntPtr.Zero, 0); // worker-thread MTA
            if (initialized < 0) { return null; }
            IntPtr searcher = IntPtr.Zero;
            IntPtr result = IntPtr.Zero;
            IntPtr updates = IntPtr.Zero;
            IntPtr criteria = IntPtr.Zero;
            bool cancellable = false;
            try {
                Guid classId = new Guid("B699E5E8-67FF-4177-88B0-3684A3388BFB");
                Guid interfaceId = new Guid("8F45ABF1-F9AE-4B95-A933-F0F66E5056EA");
                if (CoCreateInstance(ref classId, IntPtr.Zero, 1, ref interfaceId, out searcher) < 0) { return null; }
                // Fail closed: never upgrade WUA or perform an online search.
                if (Method<WuaSetBoolean>(searcher, SearcherSetAutoUpgrade)(searcher, 0) < 0 || Method<WuaSetBoolean>(searcher, SearcherSetOnline)(searcher, 0) < 0) { return null; }
                if (CoEnableCallCancellation(IntPtr.Zero) < 0) { return null; }
                cancellable = true;
                uint thread = GetCurrentThreadId();
                using (token.Register(() => { CoCancelCall(thread, 0); })) {
                    criteria = Marshal.StringToBSTR("IsInstalled=0 and IsHidden=0");
                    token.ThrowIfCancellationRequested();
                    if (Method<WuaSearch>(searcher, SearcherSearch)(searcher, criteria, out result) < 0) { return null; }
                    token.ThrowIfCancellationRequested();
                    if (Method<WuaReadCount>(result, SearchResultCode)(result, out int status) < 0 || status != 2) { return null; }
                    if (Method<WuaReadPointer>(result, SearchResultUpdates)(result, out updates) < 0) { return null; }
                    return ReadCollectionCount(updates);
                }
            } finally {
                if (criteria != IntPtr.Zero) { Marshal.FreeBSTR(criteria); }
                if (updates != IntPtr.Zero) { Marshal.Release(updates); }
                if (result != IntPtr.Zero) { Marshal.Release(result); }
                if (searcher != IntPtr.Zero) { Marshal.Release(searcher); }
                if (cancellable) { CoDisableCallCancellation(IntPtr.Zero); }
                CoUninitialize();
            }
        }

        internal static int? ReadCollectionCount(IntPtr updates) {
            if (updates == IntPtr.Zero) { return null; }
            return Method<WuaReadCount>(updates, UpdateCollectionCount)(updates, out int count) >= 0 && count >= 0 ? count : (int?)null;
        }

        private static T Method<T>(IntPtr instance, int slot) where T : Delegate {
            IntPtr table = Marshal.ReadIntPtr(instance);
            return Marshal.GetDelegateForFunctionPointer<T>(Marshal.ReadIntPtr(table, slot * IntPtr.Size));
        }

        [DllImport("ole32.dll")]
        private static extern int CoInitializeEx(IntPtr reserved, uint mode);
        [DllImport("ole32.dll")]
        private static extern void CoUninitialize();
        [DllImport("ole32.dll")]
        private static extern int CoCreateInstance(ref Guid classId, IntPtr outer, uint context, ref Guid interfaceId, out IntPtr instance);
        [DllImport("ole32.dll")]
        private static extern int CoEnableCallCancellation(IntPtr reserved);
        [DllImport("ole32.dll")]
        private static extern int CoDisableCallCancellation(IntPtr reserved);
        [DllImport("ole32.dll")]
        private static extern int CoCancelCall(uint threadId, uint timeoutSeconds);
        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();
    }
}
