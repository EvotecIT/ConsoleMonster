using System;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace ConsoleMonster.FileSystem {
    /// <summary>The Windows shell's recycle-only operation. Unrecyclable items fail and stay in place.</summary>
    internal sealed class WindowsRecycleBin : ITrash {
        // RECYCLEONDELETE is an IFileOperation flag, unavailable to the older SHFileOperation API.
        // NO_CONNECTED_ELEMENTS keeps an HTML document separate from its unselected resource folder.
        private const uint Flags = 0x00080000 | 0x00100000 | 0x0004 | 0x0010 | 0x0400 | 0x2000;

        public string Name => "Recycle Bin";
        public bool IsAvailable => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && Environment.OSVersion.Version >= new Version(6, 2);

        public string? MoveToTrash(string path) {
            if (!IsAvailable) { throw new IOException("Recycle-only deletion requires Windows 8 or later; the item was left in place."); }
            string full = FileTrash.FullExistingPath(path);
            if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA) { Recycle(full); }
            else {
                Exception? failure = null;
                Thread worker = new Thread(() => { try { Recycle(full); } catch (Exception error) { failure = error; } });
                worker.SetApartmentState(ApartmentState.STA);
                worker.Start();
                worker.Join();
                if (failure != null) { ExceptionDispatchInfo.Capture(failure).Throw(); }
            }
            if (File.Exists(full) || Directory.Exists(full) || FileOperation.IsLink(full)) { throw new IOException("'" + full + "' is still in place after recycling."); }
            return null;
        }

        private static void Recycle(string full) {
            // Raw COM pointers keep this shell adapter usable by NativeAOT without runtime COM wrappers.
            // The vtable slots and signatures follow IFileOperation in the Windows SDK's ShObjIdl_core.h.
            IntPtr operation = IntPtr.Zero, item = IntPtr.Zero;
            int initialized = CoInitializeEx(IntPtr.Zero, 2);
            if (initialized < 0) { throw new IOException("Cannot initialize the recycle operation.", Marshal.GetExceptionForHR(initialized)); }
            try {
                Guid clsid = new Guid("3ad05575-8857-4850-9277-11b85bdb8e09");
                Guid iid = new Guid("947aab5f-0a5c-4c13-b4d6-4bf7836fc9f8");
                Check(CoCreateInstance(ref clsid, IntPtr.Zero, 1, ref iid, out operation), full);
                Check(Method<SetFlags>(operation, 5)(operation, Flags), full);
                Guid shellItem = new Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe");
                Check(SHCreateItemFromParsingName(full, IntPtr.Zero, ref shellItem, out item), full);
                Check(Method<DeleteItem>(operation, 18)(operation, item, IntPtr.Zero), full);
                Check(Method<Perform>(operation, 21)(operation), full);
                Check(Method<GetAborted>(operation, 22)(operation, out int aborted), full);
                if (aborted != 0) { throw new IOException("The shell did not recycle '" + full + "'; the operation was aborted."); }
            } finally {
                if (item != IntPtr.Zero) { Marshal.Release(item); }
                if (operation != IntPtr.Zero) { Marshal.Release(operation); }
                CoUninitialize();
            }
        }

        private static T Method<T>(IntPtr instance, int slot) where T : Delegate => Marshal.GetDelegateForFunctionPointer<T>(Marshal.ReadIntPtr(Marshal.ReadIntPtr(instance), slot * IntPtr.Size));
        private static void Check(int result, string path) {
            if (result < 0) { throw new IOException("The shell could not recycle '" + path + "' (0x" + result.ToString("X8", System.Globalization.CultureInfo.InvariantCulture) + ").", Marshal.GetExceptionForHR(result)); }
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)] private delegate int SetFlags(IntPtr instance, uint flags);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] private delegate int DeleteItem(IntPtr instance, IntPtr item, IntPtr sink);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] private delegate int Perform(IntPtr instance);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] private delegate int GetAborted(IntPtr instance, out int aborted);
        [DllImport("ole32.dll")] private static extern int CoInitializeEx(IntPtr reserved, uint flags);
        [DllImport("ole32.dll")] private static extern void CoUninitialize();
        [DllImport("ole32.dll")] private static extern int CoCreateInstance(ref Guid clsid, IntPtr outer, uint context, ref Guid iid, out IntPtr instance);
        [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = true)] private static extern int SHCreateItemFromParsingName(string path, IntPtr binding, ref Guid iid, out IntPtr item);
    }
}
