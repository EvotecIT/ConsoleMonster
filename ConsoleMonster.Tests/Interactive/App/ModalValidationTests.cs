using System;
using System.Collections.Generic;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;
using ConsoleMonster.Rendering;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Interactive.App {
    /// <summary>Non-editing terminal events preserve validation feedback in both text dialogs.</summary>
    public sealed class ModalValidationTests {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Validation_RemainsUntilAKeyOrPasteEditsTheDialog(bool multiline) {
            Canvas.RenderContext context = Terminal.CreateContext(TerminalProfile.Fixed(80, 24));
            Modal modal = multiline
                ? (Modal)new TextAreaDialog("Edit", "seed", 100, 10, 5, null, _ => "Unacceptable value", _ => { }, new PromptLabels())
                : new TextDialog("Edit", null, "seed", _ => "Unacceptable value", _ => { }, new PromptLabels());
            ViewFrame frame = new ViewFrame(context, 80, 24, false, TimeSpan.Zero);
            List<KeyBinding> bindings = new List<KeyBinding>();
            modal.AddBindings(bindings, frame);
            Assert.True(KeyFooter.Dispatch(bindings, InputEvent.Press(ConsoleKey.Enter, multiline ? KeyModifiers.Control : KeyModifiers.None)));
            foreach (InputEvent input in new[] { InputEvent.Focus(false), InputEvent.Resized(70, 20), InputEvent.Pointer(1, 1, MouseAction.Move) }) {
                Assert.False(modal.HandleUnbound(input));
                Assert.Contains("Unacceptable value", FrameComposer.Compose(modal.Build(frame, frame.Caret), 80, 24, context).ToPlainText());
            }
            Assert.True(modal.HandleUnbound(InputEvent.Paste("x")));
            Assert.DoesNotContain("Unacceptable value", FrameComposer.Compose(modal.Build(frame, frame.Caret), 80, 24, context).ToPlainText());
        }
    }
}
