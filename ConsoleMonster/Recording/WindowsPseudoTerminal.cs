using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace ConsoleMonster.Sessions {
    /// <summary>ConPTY transport. The output reader stays alive while ClosePseudoConsole drains the final frame.</summary>
    internal sealed class WindowsPseudoTerminal : IPseudoTerminal {
        private IntPtr _pty;
        private ProcessInformation _process;
        private FileStream? _input;
        private FileStream? _output;
        public WindowsPseudoTerminal(SessionRecordingOptions options) {
            SafeFileHandle? inputRead = null, inputWrite = null, outputRead = null, outputWrite = null;
            IntPtr attributes = IntPtr.Zero;
            IntPtr environment = IntPtr.Zero;
            bool initialized = false;
            try {
                if (!CreatePipe(out inputRead, out inputWrite, IntPtr.Zero, 0) || !CreatePipe(out outputRead, out outputWrite, IntPtr.Zero, 0)) { throw Error(); }
                int hr = CreatePseudoConsole(new Coord(options.Width, options.Height), inputRead, outputWrite, 0, out _pty);
                if (hr != 0) { throw new Win32Exception(hr); }
                inputRead.Dispose();
                outputWrite.Dispose();
                IntPtr size = IntPtr.Zero;
                InitializeProcThreadAttributeList(IntPtr.Zero, 1, 0, ref size);
                attributes = Marshal.AllocHGlobal(size);
                if (!InitializeProcThreadAttributeList(attributes, 1, 0, ref size)) { throw Error(); }
                initialized = true;
                if (!UpdateProcThreadAttribute(attributes, 0, (IntPtr)0x00020016, _pty, (IntPtr)IntPtr.Size, IntPtr.Zero, IntPtr.Zero)) { throw Error(); }
                StartupInfoEx startup = new StartupInfoEx();
                startup.StartupInfo.cb = Marshal.SizeOf<StartupInfoEx>();
                startup.StartupInfo.dwFlags = 0x100;
                startup.lpAttributeList = attributes;
                StringBuilder command = new StringBuilder(Quote(options.Shell ?? "powershell.exe"));
                foreach (string arg in options.Arguments) { command.Append(' ').Append(Quote(arg)); }
                if (options.Environment.Count > 0) { environment = EnvironmentBlock(options.Environment); }
                if (!CreateProcessW(null, command, IntPtr.Zero, IntPtr.Zero, false, 0x00080000 | 0x00000400, environment, options.WorkingDirectory, ref startup, out _process)) { throw Error(); }
                _input = new FileStream(inputWrite, FileAccess.Write, 4096);
                _output = new FileStream(outputRead, FileAccess.Read, 4096);
            } catch {
                inputWrite?.Dispose();
                outputRead?.Dispose();
                if (_pty != IntPtr.Zero) { ClosePseudoConsole(_pty); _pty = IntPtr.Zero; }
                CloseProcess();
                throw;
            } finally {
                inputRead?.Dispose();
                outputWrite?.Dispose();
                if (initialized) { DeleteProcThreadAttributeList(attributes); }
                if (attributes != IntPtr.Zero) { Marshal.FreeHGlobal(attributes); }
                if (environment != IntPtr.Zero) { Marshal.FreeHGlobal(environment); }
            }
        }
        private static IntPtr EnvironmentBlock(IDictionary<string, string?> overrides) {
            SortedDictionary<string, string> values = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (System.Collections.DictionaryEntry entry in Environment.GetEnvironmentVariables()) { values[(string)entry.Key] = (string?)entry.Value ?? string.Empty; }
            foreach (KeyValuePair<string, string?> entry in overrides) { if (entry.Value == null) { values.Remove(entry.Key); } else { values[entry.Key] = entry.Value; } }
            StringBuilder block = new StringBuilder();
            foreach (KeyValuePair<string, string> entry in values) { block.Append(entry.Key).Append('=').Append(entry.Value).Append('\0'); }
            block.Append('\0');
            return Marshal.StringToHGlobalUni(block.ToString());
        }
        // Reuse the shared Windows argument encoder. In particular, cmd.exe switches must remain unquoted.
        internal static string Quote(string value) => Interactive.CommandLine.Quote(value, Interactive.CommandStyle.Windows);
        public int Read(byte[] bytes) => _output!.Read(bytes, 0, bytes.Length);
        public void Write(byte[] bytes) { _input!.Write(bytes, 0, bytes.Length); _input.Flush(); }
        public void Resize(int width, int height) { int hr = ResizePseudoConsole(_pty, new Coord(width, height)); if (hr != 0) { throw new Win32Exception(hr); } }
        public bool HasExited => _process.Process != IntPtr.Zero && WaitForSingleObject(_process.Process, 0) == 0;
        public int ExitCode { get { return GetExitCodeProcess(_process.Process, out uint code) ? unchecked((int)code) : -1; } }
        public void Stop() { if (_process.Process != IntPtr.Zero && !HasExited) { TerminateProcess(_process.Process, 130); } }
        public void Complete() { if (_pty != IntPtr.Zero) { IntPtr handle = _pty; _pty = IntPtr.Zero; ClosePseudoConsole(handle); } }
        public void Dispose() {
            Stop();
            Complete();
            _input?.Dispose();
            // The recorder joins the output reader before disposing its stream.
            _output?.Dispose();
            CloseProcess();
        }
        private void CloseProcess() {
            if (_process.Process != IntPtr.Zero) { CloseHandle(_process.Process); _process.Process = IntPtr.Zero; }
            if (_process.Thread != IntPtr.Zero) { CloseHandle(_process.Thread); _process.Thread = IntPtr.Zero; }
        }
        private static Win32Exception Error() => new Win32Exception(Marshal.GetLastWin32Error());
        [StructLayout(LayoutKind.Sequential)] private struct Coord { public short X, Y; public Coord(int x, int y) { X = (short)x; Y = (short)y; } }
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)] private struct StartupInfo {
            public int cb; public string? reserved, desktop, title;
            public int x, y, xSize, ySize, xCount, yCount, fill, dwFlags;
            public short show, reservedSize; public IntPtr reservedPointer, input, output, error;
        }
        [StructLayout(LayoutKind.Sequential)] private struct StartupInfoEx { public StartupInfo StartupInfo; public IntPtr lpAttributeList; }
        [StructLayout(LayoutKind.Sequential)] private struct ProcessInformation { public IntPtr Process, Thread; public uint ProcessId, ThreadId; }
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool CreatePipe(out SafeFileHandle read, out SafeFileHandle write, IntPtr attributes, uint size);
        [DllImport("kernel32.dll")] private static extern int CreatePseudoConsole(Coord size, SafeFileHandle input, SafeFileHandle output, uint flags, out IntPtr pty);
        [DllImport("kernel32.dll")] private static extern int ResizePseudoConsole(IntPtr pty, Coord size);
        [DllImport("kernel32.dll")] private static extern void ClosePseudoConsole(IntPtr pty);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool InitializeProcThreadAttributeList(IntPtr list, int count, int flags, ref IntPtr size);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool UpdateProcThreadAttribute(IntPtr list, uint flags, IntPtr attribute, IntPtr value, IntPtr size, IntPtr previous, IntPtr returned);
        [DllImport("kernel32.dll")] private static extern void DeleteProcThreadAttributeList(IntPtr list);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern bool CreateProcessW(string? app, StringBuilder command, IntPtr processAttributes, IntPtr threadAttributes, bool inherit, uint flags, IntPtr environment, string directory, ref StartupInfoEx startup, out ProcessInformation process);
        [DllImport("kernel32.dll")] private static extern uint WaitForSingleObject(IntPtr process, uint milliseconds);
        [DllImport("kernel32.dll")] private static extern bool GetExitCodeProcess(IntPtr process, out uint code);
        [DllImport("kernel32.dll")] private static extern bool TerminateProcess(IntPtr process, uint code);
        [DllImport("kernel32.dll")] private static extern bool CloseHandle(IntPtr handle);
    }
}
