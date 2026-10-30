using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace ConsoleMonster.FileSystem {
    /// <summary>
    /// The macOS Trash through <c>NSFileManager trashItemAtURL:</c>, the call Finder uses, so Put Back works and items
    /// on other volumes go to that volume's trash. It calls the Objective-C runtime directly; no Finder automation.
    /// </summary>
    internal sealed class MacTrash : ITrash {
        private const string ObjC = "/usr/lib/libobjc.A.dylib";
        private const string Foundation = "/System/Library/Frameworks/Foundation.framework/Foundation";
        private static readonly Lazy<bool> Loaded = new Lazy<bool>(Load);

        public string Name => "Trash";

        public bool IsAvailable => RuntimeInformation.IsOSPlatform(OSPlatform.OSX) && Loaded.Value;

        public string? MoveToTrash(string path) {
            if (!IsAvailable) { throw new IOException("The macOS Trash isn't available to this process, so '" + path + "' was left in place."); }
            string full = FileTrash.FullExistingPath(path);
            IntPtr pool = objc_autoreleasePoolPush();
            try {
                IntPtr text = Send(objc_getClass("NSString"), sel_registerName("stringWithUTF8String:"), Utf8(full));
                IntPtr url = Send(objc_getClass("NSURL"), sel_registerName("fileURLWithPath:"), text);
                IntPtr manager = Send(objc_getClass("NSFileManager"), sel_registerName("defaultManager"));
                if (text == IntPtr.Zero || url == IntPtr.Zero || manager == IntPtr.Zero) { throw new IOException("Foundation couldn't describe '" + full + "'."); }
                if (!SendTrash(manager, sel_registerName("trashItemAtURL:resultingItemURL:error:"), url, out IntPtr resulting, out IntPtr error)) {
                    string reason = error == IntPtr.Zero ? "unknown error" : String(Send(error, sel_registerName("localizedDescription"))) ?? "unknown error";
                    long code = error == IntPtr.Zero ? 0 : (long)Send(error, sel_registerName("code"));
                    // Cocoa's NSFileWriteNoPermissionError (513) and NSFileReadNoPermissionError (257).
                    if (code == 513 || code == 257) { throw new UnauthorizedAccessException(reason); }
                    throw new IOException(reason);
                }
                return resulting == IntPtr.Zero ? null : String(Send(resulting, sel_registerName("path")));
            } finally { objc_autoreleasePoolPop(pool); }
        }

        private static bool Load() {
            try { return dlopen(Foundation, 1) != IntPtr.Zero && objc_getClass("NSFileManager") != IntPtr.Zero; }
            catch (DllNotFoundException) { return false; }
            catch (EntryPointNotFoundException) { return false; }
        }

        private static byte[] Utf8(string text) {
            byte[] bytes = new byte[Encoding.UTF8.GetByteCount(text) + 1];
            Encoding.UTF8.GetBytes(text, 0, text.Length, bytes, 0);
            return bytes;
        }

        private static string? String(IntPtr value) {
            if (value == IntPtr.Zero) { return null; }
            IntPtr bytes = Send(value, sel_registerName("UTF8String"));
            if (bytes == IntPtr.Zero) { return null; }
            int length = 0;
            while (Marshal.ReadByte(bytes, length) != 0) { length++; }
            byte[] buffer = new byte[length];
            Marshal.Copy(bytes, buffer, 0, length);
            return Encoding.UTF8.GetString(buffer);
        }

        [DllImport("/usr/lib/libSystem.B.dylib")] private static extern IntPtr dlopen(string path, int mode);
        [DllImport(ObjC)] private static extern IntPtr objc_getClass(string name);
        [DllImport(ObjC)] private static extern IntPtr sel_registerName(string name);
        [DllImport(ObjC)] private static extern IntPtr objc_autoreleasePoolPush();
        [DllImport(ObjC)] private static extern void objc_autoreleasePoolPop(IntPtr pool);
        [DllImport(ObjC, EntryPoint = "objc_msgSend")] private static extern IntPtr Send(IntPtr receiver, IntPtr selector);
        [DllImport(ObjC, EntryPoint = "objc_msgSend")] private static extern IntPtr Send(IntPtr receiver, IntPtr selector, IntPtr argument);
        [DllImport(ObjC, EntryPoint = "objc_msgSend")] private static extern IntPtr Send(IntPtr receiver, IntPtr selector, byte[] argument);
        [DllImport(ObjC, EntryPoint = "objc_msgSend")]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool SendTrash(IntPtr receiver, IntPtr selector, IntPtr url, out IntPtr resulting, out IntPtr error);
    }
}
