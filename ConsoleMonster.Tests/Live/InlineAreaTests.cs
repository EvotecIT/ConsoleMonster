using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Live;
using ConsoleMonster.Output;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Live {
    /// <summary>An area drawn on demand on a console other code writes to: in place, after foreign output, cleared.</summary>
    public sealed class InlineAreaTests {
        private static InlineArea Area(FakeConsole console) =>
            new InlineArea(TerminalProfile.Fixed(40, 20, ColorDepth.None, GlyphTier.Ascii), console, console);

        [Fact]
        public void Draw_TwiceWithoutOtherOutput_RedrawsInPlace() {
            FakeConsole console = new FakeConsole(40);
            console.Write("PS> run\r\n");
            InlineArea area = Area(console);
            area.Draw(new TextBlock("copying 10%\nfile a"));
            area.Draw(new TextBlock("copying 50%\nfile b"));
            Assert.Equal(new[] { "PS> run", "copying 50%", "file b" }, console.Lines());
            Assert.Equal(0, console.X);
            Assert.Equal(3, console.Y);
        }

        [Fact]
        public void Draw_AfterOtherOutput_MovesBelowItWithoutAStaleCopy() {
            FakeConsole console = new FakeConsole(40);
            InlineArea area = Area(console);
            area.Draw(new TextBlock("copying 10%"));
            console.Write("WARNING: slow disk\r\n");
            area.Draw(new TextBlock("copying 60%"));
            Assert.Equal(new[] { "WARNING: slow disk", "copying 60%" }, console.Lines());
        }

        [Fact]
        public void Draw_AfterAPartialLine_KeepsTheLineAndDrawsBelow() {
            FakeConsole console = new FakeConsole(40);
            InlineArea area = Area(console);
            area.Draw(new TextBlock("step 1"));
            console.Write("abc");
            area.Draw(new TextBlock("step 2"));
            Assert.Equal(new[] { "abc", "step 2" }, console.Lines());
        }

        [Fact]
        public void Clear_InPlaceAndAfterOtherOutput_LeavesOnlyTheOutput() {
            FakeConsole console = new FakeConsole(40);
            InlineArea area = Area(console);
            area.Draw(new TextBlock("working 1\nworking 2"));
            area.Clear();
            Assert.Empty(console.Lines());
            console.Write("result\r\n");
            area.Draw(new TextBlock("working 3"));
            console.Write("done\r\n");
            area.Clear();
            Assert.Equal(new[] { "result", "done" }, console.Lines());
            Assert.Equal(0, area.Height);
        }

        [Fact]
        public void Draw_Shrinking_ErasesTheOldRows() {
            FakeConsole console = new FakeConsole(40);
            InlineArea area = Area(console);
            area.Draw(new TextBlock("a1\na2\na3"));
            area.Draw(new TextBlock("b1"));
            Assert.Equal(new[] { "b1" }, console.Lines());
        }

        [Fact]
        public void Draw_WhenTheAreaWasOverwritten_DrawsAgainAndLeavesTheTextAlone() {
            FakeConsole console = new FakeConsole(40);
            InlineArea area = Area(console);
            area.Draw(new TextBlock("old area"));
            console.Write("\u001b[1Asomething else\r\n");
            area.Draw(new TextBlock("new area"));
            Assert.Equal(new[] { "something else", "new area" }, console.Lines());
        }

        // A small terminal: text, CR/LF and the cursor, erase and delete-line sequences, with a readable buffer.
        private sealed class FakeConsole : ITerminalWriter, IConsoleScreen {
            private readonly int _width;
            private readonly List<char[]> _rows = new List<char[]>();

            public FakeConsole(int width) {
                _width = width;
            }

            public int X { get; private set; }

            public int Y { get; private set; }

            public bool IsConsole => true;

            public void Flush() {
            }

            public void Dispose() {
            }

            public bool TryGetCursor(out int x, out int y, out int width, out int windowTop) {
                x = X;
                y = Y;
                width = _width;
                windowTop = 0;
                return true;
            }

            public string? ReadRow(int y, int width) => new string(Row(y), 0, Math.Min(width, _width));

            public IReadOnlyList<string> Lines() {
                List<string> lines = _rows.Select(row => new string(row).TrimEnd()).ToList();
                while (lines.Count > 0 && lines[lines.Count - 1].Length == 0) {
                    lines.RemoveAt(lines.Count - 1);
                }
                return lines;
            }

            public void Write(string text) {
                for (int i = 0; i < text.Length; i++) {
                    char c = text[i];
                    if (c == '\u001b' && i + 1 < text.Length && text[i + 1] == '[') {
                        int start = i + 2;
                        int end = start;
                        while (end < text.Length && !char.IsLetter(text[end])) {
                            end++;
                        }
                        Sequence(text.Substring(start, end - start), text[end]);
                        i = end;
                    } else if (c == '\r') {
                        X = 0;
                    } else if (c == '\n') {
                        Y++;
                    } else {
                        if (X >= _width) {
                            X = 0;
                            Y++;
                        }
                        Row(Y)[X++] = c;
                    }
                }
            }

            private void Sequence(string parameters, char command) {
                int n = int.TryParse(parameters, out int value) ? value : 1;
                switch (command) {
                    case 'A':
                        Y = Math.Max(0, Y - n);
                        break;
                    case 'B':
                        Y += n;
                        break;
                    case 'G':
                        X = Math.Max(0, n - 1);
                        break;
                    case 'K':
                        for (int x = X; x < _width; x++) {
                            Row(Y)[x] = ' ';
                        }
                        break;
                    case 'J':
                        for (int x = X; x < _width; x++) {
                            Row(Y)[x] = ' ';
                        }
                        for (int y = Y + 1; y < _rows.Count; y++) {
                            _rows[y] = Blank();
                        }
                        break;
                    case 'M':
                        for (int k = 0; k < n && Y < _rows.Count; k++) {
                            _rows.RemoveAt(Y);
                            _rows.Add(Blank());
                        }
                        break;
                }
            }

            private char[] Row(int y) {
                while (_rows.Count <= y) {
                    _rows.Add(Blank());
                }
                return _rows[y];
            }

            private char[] Blank() => Enumerable.Repeat(' ', _width).ToArray();
        }
    }
}
