using System;
using System.Runtime.InteropServices;
using System.Text;
using ConsoleMonster.Canvas;
using ConsoleMonster.Layout;

namespace DisplayHostContract {
    public sealed class Row {
        public string Name { get; set; }
        public string Scope { get; set; }
        public Row(string name, string scope) { Name = name; Scope = scope; }
    }
    public sealed class BrokenWidget : IWidget {
        public Size Measure(Constraints constraints, RenderContext context) { return new Size(20, 2); }
        public void Render(Region region, RenderContext context) {
            region.Write(0, 0, "PARTIAL_RICH_MUST_NOT_APPEAR", default(ConsoleMonster.Styling.Style));
            throw new InvalidOperationException("Expected widget failure after drawing its first row.");
        }
    }
    public static class ConsoleText {
        [StructLayout(LayoutKind.Sequential)]
        private struct Coordinate { public short X; public short Y; }
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetStdHandle(int kind);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool ReadConsoleOutputCharacter(IntPtr handle, StringBuilder text, uint length, Coordinate start, out uint read);
        public static string Read() {
            int width = Console.BufferWidth;
            int height = Console.BufferHeight;
            int cells = Math.Min(2000000, width * height);
            StringBuilder text = new StringBuilder(cells);
            uint read;
            if (!ReadConsoleOutputCharacter(GetStdHandle(-11), text, (uint)cells, new Coordinate(), out read)) {
                throw new InvalidOperationException("The owned test console's text buffer could not be read.");
            }
            return text.ToString();
        }
    }
}
