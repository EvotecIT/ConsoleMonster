using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using ConsoleMonster.Sessions;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace ConsoleMonster.Tests {
    public sealed class WindowsPipeReaderTests {
        [Fact]
        public void CancellationDrainsQueuedOutputBeforeReturningEofWithTheWriterOpen() {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) { return; }
            if (!CreatePipe(out SafeFileHandle read, out SafeFileHandle write, IntPtr.Zero, 0)) {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
            using (var writer = new FileStream(write, FileAccess.Write, 1))
            using (var pipe = new WindowsPipeReader(read)) {
                byte[] expected = System.Text.Encoding.UTF8.GetBytes("Queued final output\n\u001b]1337;ConsoleMonster;marker;ZmluYWw=\a");
                writer.Write(expected, 0, expected.Length);
                writer.Flush();
                pipe.Cancel();
                using (var actual = new MemoryStream()) {
                    byte[] bytes = new byte[7];
                    int count;
                    while ((count = pipe.Read(bytes)) > 0) { actual.Write(bytes, 0, count); }
                    Assert.Equal(expected, actual.ToArray());
                }
            }
        }

        [Fact]
        public void CancellationReleasesAPendingReadBeforeTheWriterCloses() {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) { return; }
            if (!CreatePipe(out SafeFileHandle read, out SafeFileHandle write, IntPtr.Zero, 0)) {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
            using (write)
            using (var pipe = new WindowsPipeReader(read))
            using (var started = new ManualResetEventSlim()) {
                uint threadId = 0;
                int count = -1;
                Exception? failure = null;
                var reader = new Thread(() => {
                    threadId = GetCurrentThreadId();
                    started.Set();
                    try { count = pipe.Read(new byte[16]); }
                    catch (Exception error) { failure = error; }
                }) { IsBackground = true };
                reader.Start();
                try {
                    Assert.True(started.Wait(TimeSpan.FromSeconds(5)));
                    using (SafeWaitHandle thread = OpenThread(0x40, false, threadId)) { // THREAD_QUERY_INFORMATION
                        Assert.False(thread.IsInvalid);
                        Assert.True(SpinWait.SpinUntil(() => {
                            if (!GetThreadIOPendingFlag(thread, out bool pending)) { throw new Win32Exception(Marshal.GetLastWin32Error()); }
                            return pending;
                        }, TimeSpan.FromSeconds(5)), "The native pipe read never became pending.");
                    }
                    pipe.Cancel();
                    Assert.True(reader.Join(TimeSpan.FromSeconds(2)), "Cancellation left the native read blocked with its writer open.");
                    Assert.Null(failure);
                    Assert.Equal(0, count);
                    Assert.Equal(0, pipe.Read(new byte[16]));
                } finally {
                    write.Dispose(); // Also releases the reader if a regression defeats cancellation.
                    Assert.True(reader.Join(TimeSpan.FromSeconds(5)));
                }
            }
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CreatePipe(out SafeFileHandle read, out SafeFileHandle write, IntPtr attributes, uint size);
        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern SafeWaitHandle OpenThread(uint access, bool inherit, uint threadId);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetThreadIOPendingFlag(SafeWaitHandle thread, [MarshalAs(UnmanagedType.Bool)] out bool pending);
    }
}
