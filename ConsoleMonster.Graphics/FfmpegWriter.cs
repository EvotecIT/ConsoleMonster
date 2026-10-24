using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using ChartForgeX.Raster;

namespace ConsoleMonster.Graphics {
    internal static class FfmpegWriter {
        internal static void Write(string path, RasterAnimationSource source, double fps, CancellationToken token) {
            if (double.IsNaN(fps) || fps <= 0 || fps > 60) { throw new ArgumentOutOfRangeException(nameof(fps)); }
            string target = Path.GetFullPath(path);
            string temporary = Path.Combine(Path.GetDirectoryName(target)!, ".consolemonster-" + Guid.NewGuid().ToString("N") + ".mp4");
            using (Process process = new Process()) {
                process.StartInfo = new ProcessStartInfo {
                    FileName = "ffmpeg", UseShellExecute = false, RedirectStandardInput = true, RedirectStandardError = true, CreateNoWindow = true,
                    Arguments = "-hide_banner -loglevel error -nostdin -n -f rawvideo -pixel_format rgba -video_size " + source.Width + "x" + source.Height
                        + " -framerate " + fps.ToString(CultureInfo.InvariantCulture) + " -i pipe:0 -an -vf \"pad=ceil(iw/2)*2:ceil(ih/2)*2\" -c:v libx264 -pix_fmt yuv420p -movflags +faststart " + Quote(temporary)
                };
                try {
                    try { process.Start(); }
                    catch (Win32Exception error) { throw new InvalidOperationException("MP4 needs ffmpeg on PATH. GIF works everywhere and needs no external executable.", error); }
                    Task<string> errors = process.StandardError.ReadToEndAsync();
                    using (token.Register(() => Stop(process))) {
                        try {
                            double elapsed = 0;
                            long written = 0;
                            for (int index = 0; index < source.FrameCount; index++) {
                                RasterAnimationFrame frame = source.GetFrame(index, token);
                                elapsed += frame.Duration.TotalSeconds;
                                long end = Math.Max(written, (long)Math.Round(elapsed * fps));
                                // At least one frame overall, while cumulative rounding prevents timing drift.
                                if (index == source.FrameCount - 1) { end = Math.Max(1, end); }
                                while (written < end) {
                                    token.ThrowIfCancellationRequested();
                                    process.StandardInput.BaseStream.Write(frame.Image.Pixels, 0, frame.Image.Width * frame.Image.Height * 4);
                                    written++;
                                }
                            }
                            process.StandardInput.Close();
                            process.WaitForExit(); token.ThrowIfCancellationRequested();
                            if (process.ExitCode != 0) { throw new IOException("ffmpeg failed: " + errors.GetAwaiter().GetResult()); }
                        } catch (IOException error) {
                            Stop(process); process.WaitForExit(); token.ThrowIfCancellationRequested();
                            throw new IOException("MP4 export failed. " + errors.GetAwaiter().GetResult(), error);
                        } finally { Stop(process); }
                    }
                    // Leave an existing destination intact until encoding succeeds.
                    GraphicsOutput.Publish(temporary, target);
                } finally { if (File.Exists(temporary)) { File.Delete(temporary); } }
            }
        }
        private static void Stop(Process process) {
            try { if (!process.HasExited) { process.Kill(); process.WaitForExit(); } } catch (InvalidOperationException) { }
        }
        // Windows command-line quoting is also decoded by ProcessStartInfo on Unix.
        private static string Quote(string value) {
            StringBuilder quoted = new StringBuilder("\"");
            int slashes = 0;
            foreach (char character in value) {
                if (character == '\\') { slashes++; continue; }
                quoted.Append('\\', character == '"' ? slashes * 2 + 1 : slashes);
                quoted.Append(character); slashes = 0;
            }
            quoted.Append('\\', slashes * 2).Append('"');
            return quoted.ToString();
        }
    }
}
