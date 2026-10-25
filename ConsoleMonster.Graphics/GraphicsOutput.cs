using System;
using System.IO;

namespace ConsoleMonster.Graphics {
    // Keep the previous export intact when encoding, cancellation or disk I/O fails.
    internal static class GraphicsOutput {
        internal static void ValidatePath(string path) {
            if (string.Equals(Path.GetExtension(path), ".mp4", StringComparison.OrdinalIgnoreCase)) {
                throw new ArgumentException("MP4 export isn't available yet; use GIF for slides (see https://consolemonster.dev/docs/talks-and-demos/).");
            }
        }
        internal static string Temporary(string target) => Path.Combine(Path.GetDirectoryName(Path.GetFullPath(target))!, ".consolemonster-" + Guid.NewGuid().ToString("N") + Path.GetExtension(target));
        internal static void Publish(string temporary, string target) {
            if (File.Exists(target)) { File.Replace(temporary, target, null); }
            else { File.Move(temporary, target); }
        }
        internal static void Write(string path, Action<Stream> encode) {
            ValidatePath(path);
            string target = Path.GetFullPath(path), temporary = Temporary(target);
            try {
                using (FileStream output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write)) { encode(output); }
                Publish(temporary, target);
            } finally { if (File.Exists(temporary)) { File.Delete(temporary); } }
        }
        internal static void Bytes(string path, byte[] bytes) => Write(path, stream => stream.Write(bytes, 0, bytes.Length));
    }
}
