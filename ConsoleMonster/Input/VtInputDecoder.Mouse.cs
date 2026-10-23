using System.Collections.Generic;

namespace ConsoleMonster.Input {
    public sealed partial class VtInputDecoder {
        // SGR 1006 uses decimal button/modifier bits and one-based cell coordinates.
        // Consume unsupported reports without turning motion, wheels or extra buttons into clicks.
        private static void DecodeMouse(string parameters, char final, List<InputEvent> output) {
            if (final != 'M' && final != 'm') return;
            if (!TryParameters(parameters, out int[] values) || values.Length != 3) return;
            int code = values[0];
            if ((code & ~31) != 0 || (code & 3) == 3 || values[1] <= 0 || values[2] <= 0) return;

            KeyModifiers modifiers = KeyModifiers.None;
            if ((code & 4) != 0) modifiers |= KeyModifiers.Shift;
            if ((code & 8) != 0) modifiers |= KeyModifiers.Alt;
            if ((code & 16) != 0) modifiers |= KeyModifiers.Control;
            output.Add(InputEvent.Mouse(values[1] - 1, values[2] - 1, (code & 3) + 1, final == 'M', modifiers));
        }
    }
}
