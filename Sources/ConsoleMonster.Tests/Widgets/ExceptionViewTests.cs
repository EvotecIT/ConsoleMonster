using System;
using System.Linq;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Testing;
using ConsoleMonster.Tests.Rendering;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Errors {
    public sealed class ExceptionViewTests {
        private const string Trace =
            "   at ConsoleMonster.Demo.Inventory.Load(String path) in C:\\repo\\src\\Inventory.cs:line 42\r\n" +
            "   at System.IO.FileStream..ctor(String path, FileMode mode)\r\n" +
            "   at System.IO.File.OpenRead(String path)\r\n" +
            "   at ConsoleMonster.Demo.Program.Main(String[] args) in C:\\repo\\src\\Program.cs:line 12\r\n" +
            "   at System.Threading.Tasks.Task.Execute()";

        private static ExceptionInfo Sample() {
            ExceptionInfo info = new ExceptionInfo("ItemNotFoundException", "Cannot find path 'C:\\missing' because it does not exist.") {
                Category = "ObjectNotFound (C:\\missing:String)",
                ErrorId = "PathNotFound,Microsoft.PowerShell.Commands.GetItemCommand",
                PositionMessage = "At C:\\repo\\deploy.ps1:12 char:5\n+     Get-Item C:\\missing -ErrorAction Stop\n+     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~"
            };
            info.ScriptFrames.AddRange(ExceptionInfo.ParseScriptStackTrace("at Publish-Inventory, C:\\repo\\deploy.ps1: line 12\nat <ScriptBlock>, <No file>: line 1"));
            info.Frames.AddRange(ExceptionInfo.ParseStackTrace(Trace));
            ExceptionInfo inner = new ExceptionInfo("IOException", "The device is not ready.");
            inner.Frames.AddRange(ExceptionInfo.ParseStackTrace("   at System.IO.FileSystem.Open()\n   at System.IO.File.OpenRead(String path)"));
            info.Inner = inner;
            return info;
        }

        [Theory]
        [InlineData(GlyphTier.Full)]
        [InlineData(GlyphTier.Basic)]
        [InlineData(GlyphTier.Ascii)]
        public void ErrorRecordShape_MatchesGolden(GlyphTier tier) {
            string text = TestTerminal.RenderText(new ExceptionView(Sample()) { BasePath = "C:\\repo" }, 90, 0, tier);
            Golden.Match("exception/error." + tier.ToString().ToLowerInvariant() + ".txt", text);
        }

        [Fact]
        public void FrameworkFrames_CollapseUnlessAsked() {
            string collapsed = TestTerminal.RenderText(new ExceptionView(Sample()) { BasePath = "C:\\repo" }, 100);
            Assert.Contains("+ 2 framework frames", collapsed);
            Assert.DoesNotContain("FileStream", collapsed);
            string all = TestTerminal.RenderText(new ExceptionView(Sample()) { BasePath = "C:\\repo", ShowFrameworkFrames = true }, 100);
            Assert.Contains("FileStream..ctor(String path, FileMode mode)", all);
            Assert.DoesNotContain("framework frame", all);
        }

        [Fact]
        public void ParseStackTrace_ReadsMethodsFilesAndLines() {
            ExceptionFrame frame = ExceptionInfo.ParseStackTrace(Trace)[0];
            Assert.Equal("ConsoleMonster.Demo.Inventory.Load(String path)", frame.Method);
            Assert.Equal("C:\\repo\\src\\Inventory.cs", frame.File);
            Assert.Equal(42, frame.Line);
            Assert.False(frame.IsFramework);
            Assert.True(ExceptionInfo.ParseStackTrace(Trace)[1].IsFramework);
            Assert.Equal("Inventory.Load(String path)", ExceptionView.ShortMethod(frame.Method));
        }

        [Fact]
        public void FromException_FollowsInnerExceptionsAndLongMessagesWrap() {
            Exception exception;
            try {
                try {
                    throw new FormatException("The value 'abc' is not a number, and this message is long enough to wrap at a narrow width.");
                } catch (FormatException format) {
                    throw new InvalidOperationException("Loading the inventory failed.", format);
                }
            } catch (InvalidOperationException outer) {
                exception = outer;
            }
            ExceptionInfo info = ExceptionInfo.FromException(exception);
            Assert.Equal("InvalidOperationException", info.Type);
            Assert.Equal("FormatException", info.Inner!.Type);
            Assert.NotEmpty(info.Frames);
            string[] lines = TestTerminal.RenderText(new ExceptionView(exception), 50).Split('\n');
            Assert.Contains(lines, l => l.StartsWith("  Caused by FormatException", StringComparison.Ordinal));
            Assert.True(lines.Count(l => l.Contains("not a number") || l.Contains("wrap at")) >= 2);
            Assert.All(lines, l => Assert.True(l.TrimEnd().Length <= 50, l));
        }
    }
}
