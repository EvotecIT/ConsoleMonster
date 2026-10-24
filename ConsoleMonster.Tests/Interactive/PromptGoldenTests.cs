using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Interactive;
using ConsoleMonster.Styling;
using ConsoleMonster.Tests.Rendering;
using ConsoleMonster.Text;
using Xunit;

namespace ConsoleMonster.Tests.Interactive {
    /// <summary>
    /// Every prompt type driven by scripted keys, with golden frames per glyph tier and width, and the typed values they
    /// return. Frames are plain text (no colour), so the focused row shows its bar and, where colour may be missing,
    /// brackets.
    /// </summary>
    public sealed class PromptGoldenTests {
        private static List<Choice> Servers() {
            return new List<Choice> {
                new Choice("fs01", "FS01") { Group = "Warsaw", Badge = RichText.Plain("File"), Description = "File server · Windows Server 2022" },
                new Choice("web01", "WEB01") { Group = "Warsaw", Badge = RichText.Plain("Web") },
                new Choice("prn11", "PRN11") { Group = "Berlin", Badge = RichText.Plain("Print"), DisabledReason = "offline since 09:12" },
                new Choice("fs11", "FS11") { Group = "Berlin", Badge = RichText.Plain("File") },
                new Choice("lab01", "LAB01") { Group = "Lab", Badge = RichText.Plain("Test") }
            };
        }

        private static string Run<T>(Prompt<T> prompt, string keys, int width, GlyphTier tier, out T result, int frames = 2) {
            RecordingSurface surface = new RecordingSurface(width, 20, true);
            result = prompt.Show(PromptHarness.Options(keys, surface, tier));
            StringBuilder sb = new StringBuilder();
            List<int> picks = new List<int> { 0 };
            for (int i = Math.Max(1, surface.Frames.Count - frames + 1); i < surface.Frames.Count; i++) {
                picks.Add(i);
            }
            foreach (int i in picks.Distinct()) {
                sb.Append("--- frame ").Append(i).Append('\n').Append(surface.Frames[i].ToPlainText().TrimEnd('\n')).Append('\n');
            }
            sb.Append("--- summary\n").Append(surface.SummaryText()).Append('\n');
            return sb.ToString();
        }

        private static string Name(string scenario, GlyphTier tier, int width) => "interactive/prompt." + scenario + "." + tier.ToString().ToLowerInvariant() + "." + width + ".txt";

        [Theory]
        [InlineData(GlyphTier.Full, 60)]
        [InlineData(GlyphTier.Basic, 60)]
        [InlineData(GlyphTier.Ascii, 60)]
        [InlineData(GlyphTier.Full, 36)]
        public void Select_FilterAndChoose_MatchesGoldenAndReturnsTheValue(GlyphTier tier, int width) {
            string text = Run(new SelectPrompt<object?>("Target server", Servers()), "Down Down \"fs\" Down Enter", width, tier, out object? value, 3);
            Assert.Equal("fs11", value);
            Golden.Match(Name("select", tier, width), text);
        }

        [Theory]
        [InlineData(GlyphTier.Full, 60)]
        [InlineData(GlyphTier.Basic, 60)]
        [InlineData(GlyphTier.Ascii, 60)]
        public void MultiSelect_GroupsAndCounts_MatchGoldenAndReturnTheValues(GlyphTier tier, int width) {
            MultiSelectPrompt<object?> prompt = new MultiSelectPrompt<object?>("Servers", Servers()) { Minimum = 1 };
            string text = Run(prompt, "Enter Space Down*4 Space Enter", width, tier, out IReadOnlyList<object?> values, 3);
            Assert.Equal(new object?[] { "fs01", "fs11" }, values.ToArray());
            Golden.Match(Name("multi", tier, width), text);
        }

        [Fact]
        public void MultiSelect_WidePreview_ShowsThePreviewBesideTheList() {
            MultiSelectPrompt<object?> prompt = new MultiSelectPrompt<object?>("Servers", Servers()) { PreviewText = v => "Details of " + v };
            string text = Run(prompt, "Down Space Enter", 100, GlyphTier.Full, out IReadOnlyList<object?> values);
            Assert.Equal(new object?[] { "web01" }, values.ToArray());
            Assert.Contains("Details of web01", text);
            Golden.Match(Name("multi-preview", GlyphTier.Full, 100), text);
        }

        [Theory]
        [InlineData(GlyphTier.Full)]
        [InlineData(GlyphTier.Ascii)]
        public void Confirm_ArrowsAndEnter_MatchGolden(GlyphTier tier) {
            string text = Run(new ConfirmPrompt("Restart the service now?", true), "Right Enter", 60, tier, out bool answer);
            Assert.False(answer);
            Golden.Match(Name("confirm", tier, 60), text);
        }

        [Fact]
        public void Confirm_YesKey_AnswersAtOnce() {
            Run(new ConfirmPrompt("Continue?", false), "y", 60, GlyphTier.Full, out bool answer);
            Assert.True(answer);
        }

        [Theory]
        [InlineData(GlyphTier.Full)]
        [InlineData(GlyphTier.Ascii)]
        public void Text_ValidationError_ShowsInlineUntilFixed(GlyphTier tier) {
            TextPrompt prompt = new TextPrompt("Share name") { Pattern = "[a-z]+", PatternMessage = "Lower case letters only", Default = "reports" };
            RecordingSurface surface = new RecordingSurface(60, 20, true);
            string answer = prompt.Show(PromptHarness.Options("\"Bad Name\" Enter Ctrl+U \"ok\" Enter", surface, tier));
            Assert.Equal("ok", answer);
            int error = surface.Frames.ToList().FindIndex(f => f.ToPlainText().Contains("Lower case letters only"));
            Assert.True(error > 0);
            Golden.Match(Name("text", tier, 60), "--- error frame\n" + surface.Frames[error].ToPlainText().TrimEnd('\n') + "\n" + PromptHarness.Frames(surface, 2));
        }

        [Fact]
        public void Text_EmptyAnswer_ReturnsTheDefault() {
            Run(new TextPrompt("Name") { Default = "srv01" }, "Enter", 60, GlyphTier.Full, out string answer);
            Assert.Equal("srv01", answer);
        }

        [Fact]
        public void Secret_IsMaskedRevealsAndNeverShowsInTheSummary() {
            RecordingSurface surface = new RecordingSurface(60, 10, true);
            string answer = new SecretPrompt("Password").Show(PromptHarness.Options("\"hunter2\" Ctrl+R Ctrl+R Enter", surface));
            Assert.Equal("hunter2", answer);
            Assert.Contains("•••••••", surface.Frames[7].ToPlainText());
            Assert.Contains("hunter2", surface.Frames[8].ToPlainText());
            Assert.DoesNotContain("hunter2", surface.SummaryText());
            Golden.Match(Name("secret", GlyphTier.Full, 60), PromptHarness.Frames(surface, 3));
        }

        [Fact]
        public void Number_StepsWithArrowsAndChecksTheRange() {
            NumberPrompt prompt = new NumberPrompt("Port") { Integer = true, Minimum = 1, Maximum = 65535, Default = 443 };
            string text = Run(prompt, "Up Up Enter", 60, GlyphTier.Full, out decimal value, 3);
            Assert.Equal(445m, value);
            Golden.Match(Name("number", GlyphTier.Full, 60), text);
            NumberPrompt range = new NumberPrompt("Port") { Integer = true, Minimum = 1, Maximum = 10 };
            RecordingSurface surface = new RecordingSurface(60, 10, true);
            Assert.Equal(7m, range.Show(PromptHarness.Options("\"42\" Enter Backspace*2 \"7\" Enter", surface)));
            Assert.Contains("Enter 10 or less.", surface.Frames.Select(f => f.ToPlainText()).First(f => f.Contains("or less")));
        }

        [Fact]
        public void Path_CompletesFromTheFolderAndChecksExistence() {
            string root = Path.Combine(Path.GetTempPath(), "cm-path-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(Path.Combine(root, "reports"));
            Directory.CreateDirectory(Path.Combine(root, "releases"));
            File.WriteAllText(Path.Combine(root, "readme.txt"), "x");
            try {
                PathPrompt prompt = new PathPrompt("Folder") { Kind = PathKind.Folder, MustExist = true, BasePath = root };
                RecordingSurface surface = new RecordingSurface(60, 14, true);
                string value = prompt.Show(PromptHarness.Options("\"rep\" Tab Enter", surface));
                Assert.Equal("reports" + Path.DirectorySeparatorChar, value);
                PathPrompt missing = new PathPrompt("Folder") { Kind = PathKind.Folder, MustExist = true, BasePath = root };
                RecordingSurface second = new RecordingSurface(60, 14, true);
                Assert.Equal("releases", missing.Show(PromptHarness.Options("\"nope\" Enter Ctrl+U \"releases\" Enter", second)));
                Assert.Contains(second.Frames, f => f.ToPlainText().Contains("Not found: nope"));
                Assert.Contains(surface.Frames, f => f.ToPlainText().Contains("reports"));
            } finally {
                Directory.Delete(root, true);
            }
        }

        [Fact]
        public void TypedConfirm_AcceptsOnlyTheExactText() {
            TypedConfirmPrompt prompt = new TypedConfirmPrompt("Remove FS01 from the inventory?", "FS01") { Warning = "This cannot be undone." };
            string text = Run(prompt, "\"FS0\" Enter \"1\" Enter", 60, GlyphTier.Full, out bool confirmed, 3);
            Assert.True(confirmed);
            Golden.Match(Name("typed", GlyphTier.Full, 60), text);
            Run(new TypedConfirmPrompt("Remove?", "x"), "Escape", 60, GlyphTier.Full, out bool backedOut);
            Assert.False(backedOut);
        }

        [Fact]
        public void Escape_CancelsWithAnException() {
            RecordingSurface surface = new RecordingSurface(60, 10, true);
            PromptCanceledException error = Assert.Throws<PromptCanceledException>(() => new SelectPrompt<object?>("Target", Servers()).Show(PromptHarness.Options("Escape", surface)));
            Assert.Equal("Target", error.Prompt);
            Assert.Contains("cancelled", surface.SummaryText());
        }

        [Theory]
        [InlineData("F1")]
        [InlineData("\"?\"")]
        public void HelpKeysOpenTheKeyMapAndAnyKeyClosesIt(string help) {
            RecordingSurface surface = new RecordingSurface(60, 24, true);
            new MultiSelectPrompt<object?>("Servers", Servers()).Show(PromptHarness.Options(help + " Escape Enter", surface));
            string map = surface.Frames[1].ToPlainText();
            Assert.Contains("Keys", map);
            Assert.Contains("ctrl+a", map);
            Assert.Contains("all", map);
            Golden.Match("interactive/prompt.keymap.full.60.txt", map);
        }

        [Fact]
        public void Select_ColourFrame_UsesTheFocusBandAndAccentBar() {
            RecordingSurface surface = new RecordingSurface(48, 12, true);
            InteractiveOptions options = PromptHarness.Options("Enter", surface, GlyphTier.Full, ColorDepth.TrueColor);
            new SelectPrompt<object?>("Target", Servers()).Show(options);
            TerminalProfile profile = TerminalProfile.Fixed(48, 12, ColorDepth.TrueColor, GlyphTier.Full);
            ConsoleMonster.Canvas.CellBuffer frame = surface.Frames[0];
            ConsoleMonster.Canvas.RenderContext context = Terminal.CreateContext(profile, Theme.Dark);
            Style focus = context.Style(ThemeToken.Focus);
            int row = Enumerable.Range(0, frame.Height).First(y => frame.GetRowText(y).Contains("FS01"));
            Assert.Equal(focus.BackgroundColor, frame[10, row].Style.BackgroundColor);
            Assert.Equal("▌", frame[2, row].Glyph);
            Assert.DoesNotContain("[FS01]", frame.GetRowText(row));
        }
    }
}
