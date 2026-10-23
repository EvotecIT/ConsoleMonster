using System.IO;
using System.Text;

namespace ConsoleMonster.Overview {
    internal static class OverviewFiles {
        // Local optional text logs only. Limit both bytes sought and characters read while writers append.
        internal static string ReadTail(string path) {
            using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete)) {
                const int limit = 65536;
                bool truncated = stream.Length > limit;
                if (truncated) { stream.Seek(-limit, SeekOrigin.End); }
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8)) {
                    if (truncated) { reader.ReadLine(); }
                    char[] buffer = new char[limit];
                    int count = reader.ReadBlock(buffer, 0, buffer.Length);
                    return new string(buffer, 0, count);
                }
            }
        }
    }
}
