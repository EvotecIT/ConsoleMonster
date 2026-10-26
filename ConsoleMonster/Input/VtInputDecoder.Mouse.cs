using System.Collections.Generic;

namespace ConsoleMonster.Input {
    public sealed partial class VtInputDecoder {
        // SGR 1006 uses decimal button/modifier bits and one-based cell coordinates.
        // Consume unsupported reports without turning motion, wheels or extra buttons into clicks.
        private static void DecodeMouse(string parameters, char final, List<InputEvent> output) {
            if (final != 'M' && final != 'm') return;
            if (!TryParameters(parameters, out int[] values) || values.Length != 3) return;
            int code = values[0];
            if ((code & ~127) != 0 || values[1] <= 0 || values[2] <= 0) return;

            KeyModifiers modifiers = KeyModifiers.None;
            if ((code & 4) != 0) modifiers |= KeyModifiers.Shift;
            if ((code & 8) != 0) modifiers |= KeyModifiers.Alt;
            if ((code & 16) != 0) modifiers |= KeyModifiers.Control;
            int column = values[1] - 1, row = values[2] - 1;
            int button = code & 3;
            if ((code & 64) != 0) {
                if (final != 'M' || (code & 32) != 0) { return; }
                output.Add(InputEvent.Pointer(column, row, MouseAction.Wheel,
                    wheelDelta: button < 2 ? (button == 0 ? 1 : -1) : 0,
                    horizontalWheelDelta: button >= 2 ? (button == 2 ? -1 : 1) : 0, modifiers: modifiers));
            } else if ((code & 32) != 0) {
                if (final != 'M') { return; }
                output.Add(InputEvent.Pointer(column, row, MouseAction.Move, heldButtons: button == 3 ? 0 : 1 << button, modifiers: modifiers));
            } else if (button != 3) {
                output.Add(InputEvent.Mouse(column, row, button + 1, final == 'M', modifiers));
            }
        }
    }
}
