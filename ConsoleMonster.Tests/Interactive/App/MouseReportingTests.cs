using System;
using System.Text;
using ConsoleMonster.Interactive;
using ConsoleMonster.Output;
using Xunit;

namespace ConsoleMonster.Tests.Interactive.App {
    /// <summary>Mouse modes belong to one app lifetime and are restored on normal and failed setup paths.</summary>
    public sealed class MouseReportingTests {
        /// <summary>Both reporting modes are disabled once, and a borrowed writer remains usable.</summary>
        [Fact]
        public void Dispose_RestoresModesOnceWithoutDisposingBorrowedWriter() {
            Writer writer = new Writer();
            MouseReporting reporting = new MouseReporting(writer, false);
            reporting.Dispose();
            reporting.Dispose();
            Assert.Equal("\u001b[?1002h\u001b[?1006h\u001b[?1002l\u001b[?1006l", writer.Text.ToString());
            Assert.False(writer.Disposed);
        }

        /// <summary>A failed initial flush still attempts the disable sequence and releases an owned writer.</summary>
        [Fact]
        public void SetupFailure_DisablesModesAndDisposesOwnedWriter() {
            Writer writer = new Writer { FailFirstFlush = true };
            Assert.Throws<InvalidOperationException>(() => new MouseReporting(writer, true));
            Assert.EndsWith("\u001b[?1002l\u001b[?1006l", writer.Text.ToString());
            Assert.True(writer.Disposed);
        }

        private sealed class Writer : ITerminalWriter {
            internal readonly StringBuilder Text = new StringBuilder();
            internal bool Disposed;
            internal bool FailFirstFlush;
            public bool IsConsole => true;
            public void Write(string text) => Text.Append(text);
            public void Flush() {
                if (FailFirstFlush) { FailFirstFlush = false; throw new InvalidOperationException(); }
            }
            public void Dispose() { Disposed = true; }
        }
    }
}
