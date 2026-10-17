using System.Collections.Generic;
using System.Linq;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Changes;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;
using ConsoleMonster.Styling;
using ConsoleMonster.Testing;
using ConsoleMonster.Tests.Rendering;
using Xunit;

namespace ConsoleMonster.Tests.Changes {
    public sealed class ObjectDiffTests {
        private sealed class Address {
            public string City { get; set; } = string.Empty;
            public string Street { get; set; } = string.Empty;
        }

        private sealed class User {
            public string SamAccountName { get; set; } = string.Empty;
            public string Title { get; set; } = string.Empty;
            public bool Enabled { get; set; }
            public Address Address { get; set; } = new Address();
            public string LastLogon { get; set; } = string.Empty;
        }

        private static List<object?> Before() => new List<object?> {
            new User { SamAccountName = "anna", Title = "Engineer", Enabled = true, Address = new Address { City = "Warsaw", Street = "Main 1" }, LastLogon = "mon" },
            new User { SamAccountName = "bob", Title = "Admin", Enabled = true, Address = new Address { City = "Berlin" }, LastLogon = "mon" },
            new User { SamAccountName = "carl", Title = "Intern", Enabled = false, LastLogon = "mon" }
        };

        private static List<object?> After() => new List<object?> {
            new User { SamAccountName = "anna", Title = "Lead engineer", Enabled = true, Address = new Address { City = "Krakow", Street = "Main 1" }, LastLogon = "tue" },
            new User { SamAccountName = "carl", Title = "Intern", Enabled = false, LastLogon = "tue" },
            new User { SamAccountName = "dora", Title = "Analyst", Enabled = true, LastLogon = "tue" }
        };

        private static ChangeSet Diff() {
            DiffOptions options = new DiffOptions();
            options.Key.Add("SamAccountName");
            options.Ignore.Add("Last*");
            return ObjectDiff.Compare(Before(), After(), options);
        }

        [Fact]
        public void Compare_MatchesByKey_AndFindsPropertyChangesIncludingNested() {
            ChangeSet changes = Diff();
            Assert.Equal(1, changes.Added);
            Assert.Equal(1, changes.Removed);
            Assert.Equal(1, changes.Changed);
            Assert.True(changes.IsDestructive);
            ObjectChange anna = changes.Changes.Single(c => c.Kind == ChangeKind.Changed);
            Assert.Equal("anna", anna.Key);
            Assert.Equal(new[] { "Address.City", "Title" }, anna.Properties.Select(p => p.Path).OrderBy(p => p).ToArray());
            Assert.Equal("bob", changes.Changes.Single(c => c.Kind == ChangeKind.Removed).Key);
            Assert.Equal("dora", changes.Changes.Single(c => c.Kind == ChangeKind.Added).Key);
        }

        [Fact]
        public void Compare_DefaultKeysAndIgnoredProperties_LeaveUnchangedObjectsOut() {
            DiffOptions options = new DiffOptions { IncludeUnchanged = true };
            options.Ignore.Add("LastLogon");
            options.Key.Add("SamAccountName");
            ChangeSet changes = ObjectDiff.Compare(Before(), After(), options);
            Assert.Equal(1, changes.Unchanged);
            Assert.Contains(changes.Changes, c => c.Kind == ChangeKind.Unchanged && c.Key == "carl");
        }

        [Fact]
        public void Compare_Scalars_AreMatchedByValue() {
            ChangeSet changes = ObjectDiff.Compare(new object?[] { "a", "b" }, new object?[] { "b", "c" });
            Assert.Equal(1, changes.Added);
            Assert.Equal(1, changes.Removed);
            Assert.False(Diff().HasChanges == false);
        }

        [Fact]
        public void TextDiff_ShowsLineLevelChanges() {
            IReadOnlyList<DiffLine> lines = ChangeView.TextDiff("one\ntwo\nthree", "one\n2\nthree\nfour");
            Assert.Equal(new[] { ChangeKind.Unchanged, ChangeKind.Removed, ChangeKind.Added, ChangeKind.Unchanged, ChangeKind.Added }, lines.Select(l => l.Kind).ToArray());
        }

        [Theory]
        [InlineData(ChangeViewMode.Full, GlyphTier.Full)]
        [InlineData(ChangeViewMode.Full, GlyphTier.Ascii)]
        [InlineData(ChangeViewMode.Summary, GlyphTier.Full)]
        [InlineData(ChangeViewMode.SideBySide, GlyphTier.Full)]
        [InlineData(ChangeViewMode.Unified, GlyphTier.Ascii)]
        public void ChangeView_MatchesGolden(ChangeViewMode mode, GlyphTier glyphs) {
            string text = TestTerminal.RenderText(new ChangeView(Diff(), mode) { Title = "Domain users" }, 90, 0, glyphs);
            Golden.Match("changes/view." + mode.ToString().ToLowerInvariant() + "." + glyphs.ToString().ToLowerInvariant() + ".txt", text);
        }

        [Fact]
        public void Ask_NoChanges_ReturnsFalseWithoutAsking() {
            ChangeSet none = ObjectDiff.Compare(new object?[] { "a" }, new object?[] { "a" });
            Assert.False(none.HasChanges);
            Assert.False(ChangePreview.Ask(none, new ChangePreviewOptions { Interactive = new InteractiveOptions { Mode = InteractionMode.Rich, Input = new ScriptedInput(string.Empty) } }));
        }

        [Fact]
        public void Ask_Destructive_NeedsTheRemovalCountTyped() {
            ScreenCapture capture = new ScreenCapture(70, 12, inline: true);
            bool approved = ChangePreview.Ask(Diff(), new ChangePreviewOptions { Interactive = TestTerminal.Interactive("\"1\" Enter", capture) });
            Assert.True(approved);
            Assert.NotNull(capture.FindFrame("Type 1 to confirm"));
            ScreenCapture wrong = new ScreenCapture(70, 12, inline: true);
            Assert.False(ChangePreview.Ask(Diff(), new ChangePreviewOptions { Interactive = TestTerminal.Interactive("\"3\" Enter Escape", wrong) }));
        }

        [Fact]
        public void Ask_NonDestructive_IsAYesNoQuestion() {
            DiffOptions options = new DiffOptions();
            options.Key.Add("SamAccountName");
            ChangeSet onlyAdded = ObjectDiff.Compare(Before(), Before().Concat(new object?[] { new User { SamAccountName = "eve" } }), options);
            Assert.False(onlyAdded.IsDestructive);
            ScreenCapture capture = new ScreenCapture(70, 12, inline: true);
            Assert.True(ChangePreview.Ask(onlyAdded, new ChangePreviewOptions { Interactive = TestTerminal.Interactive("y", capture) }));
            Assert.NotNull(capture.FindFrame("Apply these changes?"));
        }
    }
}
