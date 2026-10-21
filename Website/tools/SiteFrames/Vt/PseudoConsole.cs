using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace SiteFrames.Vt {
    /// <summary>One chunk of output and when it arrived, in milliseconds from the start of the process.</summary>
    internal readonly struct OutputChunk {
        public OutputChunk(long time, byte[] data) {
            Time = time;
            Data = data;
        }

        public long Time { get; }

        public byte[] Data { get; }
    }

    /// <summary>
    /// Runs a program inside a Windows pseudo console (ConPTY) of a fixed size, so it sees a real console window and
    /// draws exactly as it would in Windows Terminal, and records the VT byte stream it produces with timestamps.
    /// </summary>
    internal static class PseudoConsole {
        /// <summary>
        /// Runs a command line and returns its output. <paramref name="interrupt"/> presses Ctrl+C in the console after that
        /// long, the way a reader stops a program that runs until it is told to stop (a host).
        /// </summary>
        public static List<OutputChunk> Run(string commandLine, int width, int height, IDictionary<string, string?> environment, string workingDirectory, TimeSpan timeout, TimeSpan? interrupt = null) {
            if (!OperatingSystem.IsWindows()) {
                throw new PlatformNotSupportedException("Captures run in a Windows pseudo console.");
            }
            if (!CreatePipe(out SafeFileHandle inputRead, out SafeFileHandle inputWrite, IntPtr.Zero, 0)
                || !CreatePipe(out SafeFileHandle outputRead, out SafeFileHandle outputWrite, IntPtr.Zero, 0)) {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
            int hr = CreatePseudoConsole(new Coord((short)width, (short)height), inputRead, outputWrite, 0, out IntPtr pty);
            if (hr != 0) {
                throw new Win32Exception(hr);
            }
            inputRead.Dispose();
            outputWrite.Dispose();

            List<OutputChunk> chunks = new List<OutputChunk>();
            Stopwatch clock = Stopwatch.StartNew();
            Thread reader = new Thread(() => {
                using (FileStream stream = new FileStream(outputRead, FileAccess.Read, 1)) {
                    byte[] buffer = new byte[65536];
                    while (true) {
                        int read;
                        try {
                            read = stream.Read(buffer, 0, buffer.Length);
                        } catch (IOException) {
                            break;
                        }
                        if (read <= 0) {
                            break;
                        }
                        byte[] copy = new byte[read];
                        Buffer.BlockCopy(buffer, 0, copy, 0, read);
                        lock (chunks) {
                            chunks.Add(new OutputChunk(clock.ElapsedMilliseconds, copy));
                        }
                    }
                }
            }) { IsBackground = true };
            reader.Start();

            IntPtr attributes = IntPtr.Zero;
            IntPtr size = IntPtr.Zero;
            InitializeProcThreadAttributeList(IntPtr.Zero, 1, 0, ref size);
            attributes = Marshal.AllocHGlobal(size);
            ProcessInformation info = default;
            IntPtr block = IntPtr.Zero;
            FileStream? input = null;
            try {
                if (!InitializeProcThreadAttributeList(attributes, 1, 0, ref size)) {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }
                if (!UpdateProcThreadAttribute(attributes, 0, (IntPtr)0x00020016, pty, (IntPtr)IntPtr.Size, IntPtr.Zero, IntPtr.Zero)) {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }
                StartupInfoEx startup = new StartupInfoEx();
                startup.StartupInfo.cb = Marshal.SizeOf<StartupInfoEx>();
                startup.lpAttributeList = attributes;
                // Without this the child inherits this tool's (redirected) standard handles instead of the pseudo console.
                startup.StartupInfo.dwFlags = 0x00000100;
                block = EnvironmentBlock(environment);
                const uint ExtendedStartupInfoPresent = 0x00080000;
                const uint UnicodeEnvironment = 0x00000400;
                StringBuilder command = new StringBuilder(commandLine);
                if (!CreateProcessW(null, command, IntPtr.Zero, IntPtr.Zero, false, ExtendedStartupInfoPresent | UnicodeEnvironment, block, workingDirectory, ref startup, out info)) {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }
                uint wait;
                if (interrupt != null) {
                    wait = WaitForSingleObject(info.hProcess, (uint)interrupt.Value.TotalMilliseconds);
                    if (wait != 0) {
                        // Ctrl+C typed into the pseudo console (ETX); the console turns it into the interrupt signal.
                        // The stream owns the pipe handle, so it stays open until the console closes (below).
                        input = new FileStream(inputWrite, FileAccess.Write, 1, false);
                        input.Write(new byte[] { 3 }, 0, 1);
                        input.Flush();
                    }
                }
                wait = WaitForSingleObject(info.hProcess, (uint)timeout.TotalMilliseconds);
                if (wait != 0) {
                    TerminateProcess(info.hProcess, 1);
                    throw new TimeoutException("Capture timed out: " + commandLine);
                }
                if (!GetExitCodeProcess(info.hProcess, out uint exitCode)) {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }
                if (exitCode != 0) {
                    throw new InvalidOperationException("Capture process exited with code " + exitCode + ": " + commandLine);
                }
                // Give ConPTY a moment to paint the last frame before the console closes.
                Thread.Sleep(400);
            } finally {
                ClosePseudoConsole(pty);
                reader.Join(TimeSpan.FromSeconds(10));
                input?.Dispose();
                inputWrite.Dispose();
                if (info.hProcess != IntPtr.Zero) {
                    CloseHandle(info.hProcess);
                }
                if (info.hThread != IntPtr.Zero) {
                    CloseHandle(info.hThread);
                }
                if (attributes != IntPtr.Zero) {
                    DeleteProcThreadAttributeList(attributes);
                    Marshal.FreeHGlobal(attributes);
                }
                if (block != IntPtr.Zero) {
                    Marshal.FreeHGlobal(block);
                }
            }
            lock (chunks) {
                return new List<OutputChunk>(chunks);
            }
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetExitCodeProcess(IntPtr process, out uint exitCode);

        private static IntPtr EnvironmentBlock(IDictionary<string, string?> overrides) {
            SortedDictionary<string, string> env = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (System.Collections.DictionaryEntry entry in Environment.GetEnvironmentVariables()) {
                env[(string)entry.Key] = (string?)entry.Value ?? string.Empty;
            }
            foreach (KeyValuePair<string, string?> pair in overrides) {
                if (pair.Value == null) {
                    env.Remove(pair.Key);
                } else {
                    env[pair.Key] = pair.Value;
                }
            }
            StringBuilder sb = new StringBuilder();
            foreach (KeyValuePair<string, string> pair in env) {
                sb.Append(pair.Key).Append('=').Append(pair.Value).Append('\0');
            }
            sb.Append('\0');
            return Marshal.StringToHGlobalUni(sb.ToString());
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Coord {
            public Coord(short x, short y) {
                X = x;
                Y = y;
            }

            public short X;
            public short Y;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct StartupInfo {
            public int cb;
            public string? lpReserved;
            public string? lpDesktop;
            public string? lpTitle;
            public int dwX;
            public int dwY;
            public int dwXSize;
            public int dwYSize;
            public int dwXCountChars;
            public int dwYCountChars;
            public int dwFillAttribute;
            public int dwFlags;
            public short wShowWindow;
            public short cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct StartupInfoEx {
            public StartupInfo StartupInfo;
            public IntPtr lpAttributeList;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ProcessInformation {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CreatePipe(out SafeFileHandle read, out SafeFileHandle write, IntPtr attributes, int size);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern int CreatePseudoConsole(Coord size, SafeFileHandle input, SafeFileHandle output, uint flags, out IntPtr pty);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern void ClosePseudoConsole(IntPtr pty);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool InitializeProcThreadAttributeList(IntPtr list, int count, int flags, ref IntPtr size);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool UpdateProcThreadAttribute(IntPtr list, uint flags, IntPtr attribute, IntPtr value, IntPtr size, IntPtr previous, IntPtr returnSize);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern void DeleteProcThreadAttributeList(IntPtr list);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool CreateProcessW(string? application, StringBuilder commandLine, IntPtr processAttributes, IntPtr threadAttributes, bool inherit, uint flags, IntPtr environment, string? directory, ref StartupInfoEx startup, out ProcessInformation info);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint WaitForSingleObject(IntPtr handle, uint milliseconds);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool TerminateProcess(IntPtr handle, uint code);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr handle);
    }
}
