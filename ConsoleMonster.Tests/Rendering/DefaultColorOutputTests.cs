using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Output;
using ConsoleMonster.Rendering;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using Xunit;

namespace ConsoleMonster.Tests.Rendering {
    public sealed class DefaultColorOutputTests {
        private const string Baseline = "\u001b[0;30;107m";

        [Theory]
        [InlineData("line", "line")]
        [InlineData("line", "inline")]
        [InlineData("line", "block")]
        [InlineData("vt", "line")]
        [InlineData("vt", "inline")]
        [InlineData("vt", "block")]
        [InlineData("inline", "line")]
        [InlineData("inline", "block")]
        public void Write_OptedInBaseline_ExplicitColorsWinAndDefaultsAndEndRestoreHostColors(string kind, string operation) {
            TerminalProfile profile = Profile().WithDefaultColors(Color.FromAnsi16(0), Color.FromAnsi16(15));
            string encoded = Write(kind, operation, profile, Sample());
            Assert.Contains(Baseline + "D", encoded, StringComparison.Ordinal);
            Assert.Contains("\u001b[0;31;107mF", encoded, StringComparison.Ordinal);
            Assert.Contains("\u001b[0;30;44mB", encoded, StringComparison.Ordinal);
            Assert.Contains("\u001b[0;1;32;43mE", encoded, StringComparison.Ordinal);
            Assert.EndsWith(Baseline, LastSgr(encoded), StringComparison.Ordinal);
            Assert.DoesNotContain("\u001b[0m", encoded, StringComparison.Ordinal);
        }

        [Theory]
        [InlineData("line", "line", "\u001b[0mplain\u001b[0m")]
        [InlineData("line", "inline", "\u001b[0mplain\u001b[0m")]
        [InlineData("line", "block", "\u001b[0mplain\u001b[0m")]
        [InlineData("vt", "line", "\u001b[0mplain\u001b[0m")]
        [InlineData("vt", "inline", "\u001b[0mplain\u001b[0m")]
        [InlineData("vt", "block", "\u001b[0mplain\u001b[0m")]
        [InlineData("inline", "line", "\u001b[0mplain\u001b[0m\u001b[K")]
        [InlineData("inline", "block", "\u001b[0mplain\u001b[0m")]
        public void Write_NoOptIn_FullTierRetainsOriginalBytes(string kind, string operation, string expected) {
            string suffix = operation == "inline" && kind != "inline" ? string.Empty : kind == "line" ? Environment.NewLine : "\r\n";
            string before = Write(kind, operation, Profile(), StyledLine.From("plain"));
            Assert.Equal(expected + suffix, before);
            string after = Write(kind, operation, Profile().WithDefaultColors(Color.Default, Color.Default), StyledLine.From("plain"));
            Assert.Equal(before, after);
        }

        [Theory]
        [InlineData(ColorDepth.Ansi16)]
        [InlineData(ColorDepth.Ansi256)]
        [InlineData(ColorDepth.TrueColor)]
        public void Write_OneBaselineChannel_DoesNotReplaceTheOtherDefaultOrLeakAcrossProfiles(ColorDepth depth) {
            TerminalProfile original = Profile().WithColorDepth(depth);
            string foreground = Write("line", "inline", original.WithDefaultColors(Color.FromAnsi16(0), Color.Default), StyledLine.From("F"));
            string background = Write("line", "inline", original.WithDefaultColors(Color.Default, Color.FromAnsi16(15)), StyledLine.From("B"));
            Assert.Equal("\u001b[0;30mF\u001b[0;30m", foreground);
            Assert.Equal("\u001b[0;107mB\u001b[0;107m", background);
            Assert.Equal("\u001b[0mD\u001b[0m", Write("line", "inline", original, StyledLine.From("D")));
        }

        [Theory]
        [InlineData("line", "plain")]
        [InlineData("vt", "plain")]
        [InlineData("inline", "plain")]
        [InlineData("line", "none")]
        [InlineData("vt", "none")]
        [InlineData("inline", "none")]
        [InlineData("line", "no-color")]
        [InlineData("vt", "no-color")]
        [InlineData("inline", "no-color")]
        public void Write_ColorDisabled_WithBaselineEmitsNoSgrIncludingLiveCleanup(string kind, string setting) {
            TerminalProfile profile = setting == "plain" ? Profile().WithMode(OutputMode.Plain)
                : setting == "none" ? Profile().WithColorDepth(ColorDepth.None)
                : new CapabilityDetector(new TerminalEnvironment(new Dictionary<string, string> { ["TERM"] = "xterm", ["NO_COLOR"] = "1" })).Detect();
            profile = profile.WithDefaultColors(Color.FromAnsi16(0), Color.FromAnsi16(15));
            StringTerminalWriter writer = new StringTerminalWriter();
            using (ITerminalOutput output = Output(kind, writer, profile)) {
                output.WriteLine(Sample());
                output.WriteInline(Sample());
                output.WriteBlock(Frame(Sample()));
                if (output.SupportsLiveFrames) {
                    output.BeginLive();
                    output.RenderFrame(Frame(Sample()));
                    output.EndLive();
                }
            }
            Assert.Empty(Regex.Matches(writer.ToString(), "\u001b\\[[0-9;]*m"));
            Assert.Contains("DFBED", writer.ToString());
        }

        [Fact]
        public void VtLive_FullFrameDiffAndExit_RestoreBaselineBeforeEraseAndAfterReturningToNormalScreen() {
            TerminalProfile profile = Profile().WithDefaultColors(Color.FromAnsi16(0), Color.FromAnsi16(15));
            StringTerminalWriter writer = new StringTerminalWriter();
            using (VtOutput output = new VtOutput(writer, profile, false)) {
                output.BeginLive();
                Assert.Contains("\u001b[?25l" + Baseline + "\u001b[2J", writer.ToString(), StringComparison.Ordinal);
                writer.Clear();
                CellBuffer frame = new CellBuffer(10, 2);
                frame.Write(0, 0, "A", Style.Plain);
                output.RenderFrame(frame);
                Assert.Contains(Baseline + "\u001b[2J", writer.ToString(), StringComparison.Ordinal);
                Assert.Equal(Baseline, LastSgr(writer.ToString()));
                writer.Clear();
                frame.Write(0, 0, "B", new Style(Color.FromAnsi16(1)));
                output.RenderFrame(frame);
                Assert.Contains("\u001b[0;31;107mB", writer.ToString(), StringComparison.Ordinal);
                Assert.Equal(Baseline, LastSgr(writer.ToString()));
                writer.Clear();
                output.EndLive();
                Assert.EndsWith("\u001b[?1049l" + Baseline, writer.ToString(), StringComparison.Ordinal);
            }
        }

        [Fact]
        public void InlineLive_FrameAndExit_RestoreBaselineAtRowAndSessionBoundaries() {
            StringTerminalWriter writer = new StringTerminalWriter();
            using (InlineOutput output = new InlineOutput(writer, Profile().WithDefaultColors(Color.FromAnsi16(0), Color.FromAnsi16(15)), false)) {
                output.BeginLive();
                output.RenderFrame(Frame(Sample()));
                Assert.Contains(Baseline + "\u001b[K", writer.ToString(), StringComparison.Ordinal);
                writer.Clear();
                output.EndLive();
                Assert.EndsWith(Baseline + "\u001b[?25h", writer.ToString(), StringComparison.Ordinal);
            }
        }

        [Fact]
        public void VtInPlace_StatusUpdate_RestoresBaselineBeforeClearingRemainder() {
            StringTerminalWriter writer = new StringTerminalWriter();
            using (VtOutput output = new VtOutput(writer, Profile().WithDefaultColors(Color.FromAnsi16(0), Color.FromAnsi16(15)), false)) {
                output.WriteInPlace(StyledLine.From("status", new Style(Color.FromAnsi16(1))), true);
            }
            Assert.Equal("\r\u001b[0;31;107mstatus" + Baseline + "\u001b[K\r\n", writer.ToString());
        }

        [Theory]
        [InlineData("vt")]
        [InlineData("inline")]
        public void Write_HyperlinkEnabledStyledLine_StillUsesProfileBaselineAndClosesTheLink(string kind) {
            TerminalProfile profile = Profile().WithHyperlinks(true).WithDefaultColors(Color.FromAnsi16(0), Color.FromAnsi16(15));
            string encoded = Write(kind, "line", profile, StyledLine.From("report", Style.Plain.WithLink("https://example.test")));
            Assert.Contains("\u001b]8;;https://example.test\u001b\\" + Baseline + "report\u001b]8;;\u001b\\", encoded, StringComparison.Ordinal);
            Assert.Equal(Baseline, LastSgr(encoded));
        }

        [Theory]
        [InlineData(ColorDepth.Ansi16, "\u001b[0;30;107m")]
        [InlineData(ColorDepth.Ansi256, "\u001b[0;38;5;16;48;5;231m")]
        [InlineData(ColorDepth.TrueColor, "\u001b[0;38;2;0;0;0;48;2;255;255;255m")]
        public void Write_RgbBaseline_IsDownsampledAtEncodingTime(ColorDepth depth, string expected) {
            TerminalProfile profile = Profile().WithColorDepth(depth).WithDefaultColors(Color.FromRgb(0, 0, 0), Color.FromRgb(255, 255, 255));
            Assert.Equal(expected + "text" + expected, Write("line", "inline", profile, StyledLine.From("text")));
        }

        private static TerminalProfile Profile() => TerminalProfile.Fixed(20, 5, ColorDepth.TrueColor, GlyphTier.Full);

        private static StyledLine Sample() => new StyledLine(new[] {
            new TextSpan("D", Style.Plain), new TextSpan("F", new Style(Color.FromAnsi16(1))),
            new TextSpan("B", new Style(Color.Default, Color.FromAnsi16(4))),
            new TextSpan("E", new Style(Color.FromAnsi16(2), Color.FromAnsi16(3), TextDecoration.Bold)), new TextSpan("D", Style.Plain)
        });

        private static CellBuffer Frame(StyledLine line) {
            CellBuffer buffer = new CellBuffer(line.Width, 1);
            new Region(buffer).Write(0, 0, line);
            return buffer;
        }

        private static ITerminalOutput Output(string kind, StringTerminalWriter writer, TerminalProfile profile) => kind == "line"
            ? (ITerminalOutput)new LineOutput(writer, profile, false) : kind == "vt" ? new VtOutput(writer, profile, false) : new InlineOutput(writer, profile, false);

        private static string Write(string kind, string operation, TerminalProfile profile, StyledLine line) {
            StringTerminalWriter writer = new StringTerminalWriter();
            using (ITerminalOutput output = Output(kind, writer, profile)) {
                if (operation == "block") output.WriteBlock(Frame(line));
                else if (operation == "inline") output.WriteInline(line);
                else output.WriteLine(line);
            }
            return writer.ToString();
        }

        private static string LastSgr(string text) {
            MatchCollection matches = Regex.Matches(text, "\u001b\\[[0-9;]*m");
            return matches.Count == 0 ? string.Empty : matches[matches.Count - 1].Value;
        }
    }
}
