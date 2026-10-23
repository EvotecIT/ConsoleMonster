using System;
using ConsoleMonster.Canvas;
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Testing;
using Xunit;

namespace ConsoleMonster.Tests.Interactive.App {
    /// <summary>Public focus traversal and hit testing over ordinary widget layouts.</summary>
    public sealed class FocusManagerTests {
        /// <summary>Tab overrides sort first and equal indexes retain nested declaration order in both directions.</summary>
        [Fact]
        public void Traversal_UsesTabIndexThenDeclarationOrderAndWraps() {
            TextInput last = new TextInput("last") { TabIndex = 5 };
            TextInput first = new TextInput("first");
            TextInput second = new TextInput("second");
            using (FocusManager focus = new FocusManager(new Rows(last, new Rows(first, second)))) {
                Assert.Equal(new InputControl[] { first, second, last }, focus.Controls);
                Assert.Same(first, focus.Current);
                focus.MoveNext();
                Assert.Same(second, focus.Current);
                Assert.False(first.Focused);
                Assert.True(second.Focused);
                focus.MoveNext();
                Assert.Same(last, focus.Current);
                focus.MoveNext();
                Assert.Same(first, focus.Current);
                focus.MovePrevious();
                Assert.Same(last, focus.Current);
                focus.MovePrevious();
                Assert.Same(second, focus.Current);
            }
        }

        /// <summary>Disabled controls are skipped and disabling the current control transfers or clears focus.</summary>
        [Fact]
        public void DisabledControls_AreSkippedAndCurrentFocusIsReconciled() {
            TextInput first = new TextInput("first");
            TextInput disabled = new TextInput("disabled") { Enabled = false };
            TextInput last = new TextInput("last");
            using (FocusManager focus = new FocusManager(new Rows(first, disabled, last))) {
                focus.MoveNext();
                Assert.Same(last, focus.Current);
                focus.MovePrevious();
                Assert.Same(first, focus.Current);
                first.Enabled = false;
                Assert.Same(last, focus.Current);
                Assert.False(first.Focused);
                last.Enabled = false;
                Assert.Null(focus.Current);
                Assert.False(last.Focused);
                disabled.Enabled = true;
                focus.MoveNext();
                Assert.Same(disabled, focus.Current);
            }
        }

        /// <summary>Control Focus and manager Focus target the same owner and reject foreign or disabled controls.</summary>
        [Fact]
        public void ExplicitFocus_TransfersOwnershipAndIgnoresDisabledTargets() {
            TextInput first = new TextInput("first");
            TextInput second = new TextInput("second");
            using (FocusManager focus = new FocusManager(new Rows(first, second))) {
                second.Focus();
                Assert.Same(second, focus.Current);
                Assert.False(first.Focused);
                Assert.True(second.Focused);
                focus.Focus(first);
                Assert.Same(first, focus.Current);
                second.Enabled = false;
                second.Focus();
                focus.Focus(second);
                Assert.Same(first, focus.Current);
                Assert.Throws<ArgumentException>(() => focus.Focus(new TextInput("foreign")));
            }
        }

        /// <summary>Hit testing uses zero-based rendered frame coordinates with exclusive right and bottom edges.</summary>
        [Fact]
        public void FocusAt_UsesRenderedCoordinatesAndSkipsDisabledControls() {
            TextInput first = new TextInput("first");
            TextInput second = new TextInput("second");
            using (FocusManager focus = new FocusManager(new Rows(first, second))) {
                RenderContext context = new RenderContext(TestTerminal.Profile(40, 16), Theme.Dark);
                CellBuffer frame = new CellBuffer(40, 16);
                first.Render(new Region(frame).Sub(3, 2, 12, 3), context);
                second.Render(new Region(frame).Sub(20, 8, 12, 3), context);
                Assert.True(focus.FocusAt(20, 8));
                Assert.Same(second, focus.Current);
                Assert.False(focus.FocusAt(32, 8));
                Assert.False(focus.FocusAt(20, 11));
                Assert.False(focus.FocusAt(-1, 8));
                Assert.Same(second, focus.Current);
                Assert.True(focus.FocusAt(14, 4));
                Assert.Same(first, focus.Current);
                second.Enabled = false;
                Assert.False(focus.FocusAt(21, 9));
                Assert.Same(first, focus.Current);
            }
        }

        /// <summary>Case-insensitive input identity prevents two controls from sharing a state key.</summary>
        [Fact]
        public void DuplicateNames_AreRejectedBeforeFocusOwnership() {
            TextInput first = new TextInput("Query");
            TextInput duplicate = new TextInput("QUERY");
            Assert.Throws<ArgumentException>(() => new FocusManager(new Rows(first, duplicate)));
            Assert.False(first.Focused);
            Assert.False(duplicate.Focused);
        }
    }
}
