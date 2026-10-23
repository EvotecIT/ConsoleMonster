using System.Collections.Generic;
using ConsoleMonster.Interop;

namespace ConsoleMonster.Input {
    public sealed partial class WindowsConsoleInput {
        // Windows numbers the right button before the middle button in its state mask.
        // A double-click record is the second press, so it participates in normal transitions.
        private static void TranslateMouse(ConsoleInputRecord record, List<InputEvent> output, ref uint previousButtons) {
            uint buttons = record.MouseButtonState & 7;
            uint changed = previousButtons ^ buttons;
            // Keep the baseline even for filtered records: a later snapshot must not invent a missed press.
            previousButtons = buttons;
            if ((record.MouseEventFlags != 0 && record.MouseEventFlags != 2) || record.MouseX < 0 || record.MouseY < 0) return;

            KeyModifiers modifiers = KeyModifiers.None;
            uint state = record.MouseControlKeyState;
            if ((state & ConsoleInputNative.ShiftPressed) != 0) modifiers |= KeyModifiers.Shift;
            if ((state & (ConsoleInputNative.LeftAlt | ConsoleInputNative.RightAlt)) != 0) modifiers |= KeyModifiers.Alt;
            if ((state & (ConsoleInputNative.LeftCtrl | ConsoleInputNative.RightCtrl)) != 0) modifiers |= KeyModifiers.Control;

            AddMouseTransition(1, 1, record, changed, buttons, modifiers, output);
            AddMouseTransition(4, 2, record, changed, buttons, modifiers, output);
            AddMouseTransition(2, 3, record, changed, buttons, modifiers, output);
        }

        private static void AddMouseTransition(uint mask, int button, ConsoleInputRecord record, uint changed, uint buttons,
            KeyModifiers modifiers, List<InputEvent> output) {
            if ((changed & mask) != 0) {
                output.Add(InputEvent.Mouse(record.MouseX, record.MouseY, button, (buttons & mask) != 0, modifiers));
            }
        }
    }
}
