using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleMonster.Clipboard {
    internal sealed class ClipboardPlatform : IClipboardPlatform {
        public ClipboardOS OS => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ClipboardOS.Windows
            : RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? ClipboardOS.MacOS
            : RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? ClipboardOS.Linux : ClipboardOS.Other;

        public string? Environment(string name) => System.Environment.GetEnvironmentVariable(name);

        public string? FindExecutable(string name) {
            foreach (string directory in (Environment("PATH") ?? string.Empty).Split(Path.PathSeparator)) {
                // Never implicitly execute from the current directory through an empty or relative PATH entry.
                string root = directory.Trim().Trim('"');
                if (root.Length == 0 || !Path.IsPathRooted(root)) {
                    continue;
                }
                try {
                    string path = Path.Combine(root, name);
                    if (File.Exists(path)) {
                        return path;
                    }
                } catch (ArgumentException) {
                    // An invalid PATH entry does not prevent finding the next provider.
                }
            }
            return null;
        }

        public bool CopyWindows(string text, out string? error) => WindowsClipboard.TryCopy(text, out error);

        public bool Run(string executable, string arguments, string text, out string? error) => Run(executable, arguments, text, 5000, out error);

        // A bounded stdin write and wait prevent a wedged provider from hanging the invoking shell.
        internal bool Run(string executable, string arguments, string text, int timeoutMilliseconds, out string? error) {
            using (Process process = new Process()) {
                process.StartInfo = new ProcessStartInfo {
                    FileName = executable,
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                if (OS == ClipboardOS.MacOS && Path.GetFileName(executable) == "pbcopy") {
                    process.StartInfo.EnvironmentVariables.Remove("LC_ALL");
                    process.StartInfo.EnvironmentVariables["LC_CTYPE"] = "UTF-8";
                }
                process.OutputDataReceived += (_, __) => { };
                process.ErrorDataReceived += (_, __) => { };
                try {
                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    Stopwatch elapsed = Stopwatch.StartNew();
                    Task input = Task.Run(() => {
                        using (StreamWriter writer = new StreamWriter(process.StandardInput.BaseStream, new UTF8Encoding(false))) {
                            writer.Write(text);
                        }
                    });
                    // Observe faults even when the timeout wins; the writer may finish after the process is killed.
                    _ = input.ContinueWith(task => { _ = task.Exception; }, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);
                    if (!input.Wait(timeoutMilliseconds) || !process.WaitForExit(Math.Max(1, timeoutMilliseconds - (int)elapsed.ElapsedMilliseconds))) {
                        Stop(process);
                        error = "Clipboard provider timed out. Check that your desktop clipboard session is available.";
                        return false;
                    }
                    if (process.ExitCode != 0) {
                        error = "Clipboard provider failed with exit code " + process.ExitCode + ". Check that your desktop clipboard session is available.";
                        return false;
                    }
                    error = null;
                    return true;
                } catch (Exception ex) when (ex is Win32Exception || ex is IOException || ex is InvalidOperationException || ex is AggregateException || ex is UnauthorizedAccessException) {
                    Stop(process);
                    error = "Clipboard provider could not accept the output. Check the executable and your desktop clipboard session.";
                    return false;
                }
            }
        }

        private static void Stop(Process process) {
            try {
                if (!process.HasExited) {
                    process.Kill();
                }
            } catch (InvalidOperationException) { } catch (Win32Exception) { }
        }
    }
}
