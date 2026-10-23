using System;
using System.Runtime.InteropServices;
using ConsoleMonster.Overview;
using Xunit;

namespace ConsoleMonster.Tests.Overview {
    public sealed class OverviewWindowsUpdatesTests {
        [Theory]
        [InlineData(0, 42, 42)]
        [InlineData(0, 0, 0)]
        [InlineData(unchecked((int)0x80004005), 42, null)]
        [InlineData(0, -1, null)]
        public void CollectionCountUsesLongGetterAtSlotTen(int result, int value, int? expected) {
            AssertCollectionCount(result, value, expected);
        }

        private static void AssertCollectionCount(int result, int value, int? expected) {
            int countCalls = 0;
            int wrongSlotCalls = 0;
            IntPtr observedSelf = IntPtr.Zero;
            IntPtr table = Marshal.AllocHGlobal(11 * IntPtr.Size);
            IntPtr instance = Marshal.AllocHGlobal(IntPtr.Size);
            WuaNoWriteTrap trap = (self, destination) => {
                wrongSlotCalls++;
                return unchecked((int)0x80004005); // Do not dereference destination, even on a regression.
            };
            WuaReadCount count = (IntPtr self, out int observed) => {
                observedSelf = self;
                countCalls++;
                observed = value;
                return result;
            };
            try {
                IntPtr trapAddress = Marshal.GetFunctionPointerForDelegate(trap);
                for (int slot = 0; slot < 11; slot++) { Marshal.WriteIntPtr(table, slot * IntPtr.Size, trapAddress); }
                Marshal.WriteIntPtr(table, 10 * IntPtr.Size, Marshal.GetFunctionPointerForDelegate(count));
                Marshal.WriteIntPtr(instance, table);

                Assert.Equal(expected, OverviewWindowsUpdates.ReadCollectionCount(instance));
                Assert.Equal(instance, observedSelf);
                Assert.Equal(1, countCalls);
                Assert.Equal(0, wrongSlotCalls);
            } finally {
                GC.KeepAlive(count);
                GC.KeepAlive(trap);
                Marshal.FreeHGlobal(instance);
                Marshal.FreeHGlobal(table);
            }
        }

        [Fact]
        public void InstalledWindowsCollectionFactoryReturnsEmptyCountWithoutSearching() {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) { return; }
            // Fail safely before native activation if a future accessor regression selects the trap slot.
            AssertCollectionCount(0, 42, 42);
            // An in-memory UpdateColl does not activate a searcher, scan the cache or contact a service.
            int initialized = CoInitializeEx(IntPtr.Zero, 0);
            Assert.True(initialized >= 0, "The test worker must support a COM MTA.");
            IntPtr collection = IntPtr.Zero;
            try {
                Guid classId = new Guid("13639463-00DB-4646-803D-528026140D88");
                Guid interfaceId = new Guid("07F7438C-7709-4CA5-B518-91279288134E");
                int result = CoCreateInstance(ref classId, IntPtr.Zero, 1, ref interfaceId, out collection);
                Assert.True(result >= 0, "The installed Windows Update collection factory must be available.");
                Assert.NotEqual(IntPtr.Zero, collection);
                Assert.Equal(0, OverviewWindowsUpdates.ReadCollectionCount(collection));
            } finally {
                if (collection != IntPtr.Zero) { Marshal.Release(collection); }
                CoUninitialize();
            }
        }

        [DllImport("ole32.dll")]
        private static extern int CoInitializeEx(IntPtr reserved, uint mode);
        [DllImport("ole32.dll")]
        private static extern void CoUninitialize();
        [DllImport("ole32.dll")]
        private static extern int CoCreateInstance(ref Guid classId, IntPtr outer, uint context, ref Guid interfaceId, out IntPtr instance);
    }
}
