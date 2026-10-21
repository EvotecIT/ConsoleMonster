using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using ConsoleMonster.Interactive;
using Xunit;

namespace ConsoleMonster.Tests.Interactive {
    /// <summary>Windows argument encoding is checked against the operating system's independent argv parser.</summary>
    public sealed class CommandLineQuotingTests {
        [Theory]
        [InlineData("", "\"\"")]
        [InlineData("plain", "plain")]
        [InlineData("two words", "\"two words\"")]
        [InlineData("two\twords", "\"two\twords\"")]
        [InlineData("C:\\Agent\\", "C:\\Agent\\")]
        [InlineData("C:\\Program Files\\Agent\\", "\"C:\\Program Files\\Agent\\\\\"")]
        [InlineData("say \"hello\"", "\"say \\\"hello\\\"\"")]
        [InlineData("a\\\"b", "\"a\\\\\\\"b\"")]
        [InlineData("\\\\server\\share\\", "\\\\server\\share\\")]
        [InlineData("\\\\server\\shared files\\", "\"\\\\server\\shared files\\\\\"")]
        public void Quote_Windows_EncodesArgumentBoundaries(string value, string expected) {
            Assert.Equal(expected, CommandLine.Quote(value, CommandStyle.Windows));
        }

        public static IEnumerable<object[]> WindowsArguments() {
            foreach (string value in new[] {
                string.Empty, "plain", "two words", "two\twords", " ", "\t", "\"", "\"\"",
                "say \"hello world\"", "C:\\Agent\\", "C:\\Program Files\\Agent\\",
                "\\\\server\\share\\", "\\\\server\\shared files\\", "日本 zażółć"
            }) {
                yield return new object[] { value };
            }
            // Odd/even slash runs before literal quotes and closing quotes exercise both native parsing rules.
            for (int count = 1; count <= 4; count++) {
                string slashes = new string('\\', count);
                yield return new object[] { "two words" + slashes };
                yield return new object[] { "before" + slashes + "\"after" };
                yield return new object[] { slashes + "\"" + slashes };
            }
        }

        [WindowsTheory]
        [MemberData(nameof(WindowsArguments))]
        public void Quote_Windows_RoundTripsThroughCommandLineToArgvW(string value) {
            string quoted = CommandLine.Quote(value, CommandStyle.Windows);
            string command = CommandLine.Join("fixture.exe", new[] { "before", quoted, "after" });
            Assert.Equal(new[] { "fixture.exe", "before", value, "after" }, ParseWindows(command));

            // Named form arguments use the same encoder after their /Name: prefix.
            if (value.Length > 0) {
                string argument = CommandLine.Argument("Value", value, CommandStyle.Windows);
                Assert.Equal(new[] { "fixture.exe", "/Value:" + value, "after" },
                    ParseWindows(CommandLine.Join("fixture.exe", new[] { argument, "after" })));
            }
        }

        [Fact]
        public void Quote_OtherStyles_PreserveTheirEscaping() {
            const string value = "two words\\\"and'quote\\";
            Assert.Equal("'two words\\\"and''quote\\'", CommandLine.Quote(value, CommandStyle.PowerShell));
            Assert.Equal("'two words\\\"and'\\''quote\\'", CommandLine.Quote(value, CommandStyle.Posix));
        }

        private static string[] ParseWindows(string command) {
            IntPtr arguments = CommandLineToArgvW(command, out int count);
            if (arguments == IntPtr.Zero) {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
            try {
                return Enumerable.Range(0, count)
                    .Select(i => Marshal.PtrToStringUni(Marshal.ReadIntPtr(arguments, i * IntPtr.Size))!)
                    .ToArray();
            } finally {
                LocalFree(arguments);
            }
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        private static extern IntPtr CommandLineToArgvW(string command, out int count);

        [DllImport("kernel32.dll", ExactSpelling = true)]
        private static extern IntPtr LocalFree(IntPtr memory);

        private sealed class WindowsTheoryAttribute : TheoryAttribute {
            public WindowsTheoryAttribute() {
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                    Skip = "The independent CommandLineToArgvW oracle requires Windows.";
                }
            }
        }
    }
}
