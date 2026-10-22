using System;
using System.Threading;
#if !NETFRAMEWORK
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using ConsoleMonster.Input;
#endif

namespace ConsoleMonster.TestHost {
    /// <summary>Native Unix input oracle kept alive after disposal so its parent can inspect the actual TTY mode.</summary>
    internal static class UnixInputProbe {
        internal static int Run(string pipe) {
#if NETFRAMEWORK
            throw new PlatformNotSupportedException();
#else
static void Report(object value) => Console.Error.WriteLine(JsonSerializer.Serialize(value));
static object Describe(InputEvent e) => new {
    kind = e.Kind.ToString(), key = e.Key.ToString(), character = (int)e.Char,
    modifiers = (int)e.Modifiers, column = e.Column, row = e.Row,
    button = e.MouseButton, pressed = e.MousePressed
};

Console.InputEncoding = new UTF8Encoding(false);
Console.OutputEncoding = new UTF8Encoding(false);
using var control = new AnonymousPipeClientStream(PipeDirection.In, pipe);
InputLease? lease = null;
int result = 0;
try {
    if (Console.IsInputRedirected || Console.IsOutputRedirected || ConsoleInput.Override != null)
        throw new InvalidOperationException("Expected an actual TTY and the default console source.");
    using (var direct = ConsoleInput.OpenConsole()) {
        if (direct is not ConsoleKeyInput) throw new InvalidOperationException("OpenConsole did not select ConsoleKeyInput.");
        Report(new { phase = "opened", source = direct.GetType().FullName });
    }
    lease = ConsoleInput.Acquire();
    if (lease.Source is not ConsoleKeyInput || !ConsoleInput.IsAcquired)
        throw new InvalidOperationException("Acquire did not lease ConsoleKeyInput.");
    Report(new { phase = "acquired", source = lease.Source.GetType().FullName,
        runtime = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
        pid = Environment.ProcessId });
    var cases = new (string Name, InputEvent[] Expected)[] {
        ("unicode", new[] { InputEvent.Character('a'), InputEvent.Character('Ż'), InputEvent.Character('中') }),
        ("arrows", new[] { InputEvent.Press(ConsoleKey.UpArrow), InputEvent.Press(ConsoleKey.DownArrow),
            InputEvent.Press(ConsoleKey.RightArrow), InputEvent.Press(ConsoleKey.LeftArrow) }),
        ("tab", new[] { InputEvent.Press(ConsoleKey.Tab) }),
        ("shift_tab", new[] { InputEvent.Press(ConsoleKey.Tab, KeyModifiers.Shift) }),
        ("escape", new[] { InputEvent.Press(ConsoleKey.Escape) }),
        ("sgr_origin", new[] { InputEvent.Mouse(0, 0), InputEvent.Mouse(0, 0, 1, false), InputEvent.Character('z') }),
        ("sgr_all_modifiers", new[] {
            InputEvent.Mouse(119, 39, 1, true, KeyModifiers.Shift | KeyModifiers.Alt | KeyModifiers.Control),
            InputEvent.Mouse(119, 39, 1, false, KeyModifiers.Shift | KeyModifiers.Alt | KeyModifiers.Control),
            InputEvent.Character('z') }),
        ("sgr_middle_alt", new[] { InputEvent.Mouse(42, 16, 2, true, KeyModifiers.Alt),
            InputEvent.Mouse(42, 16, 2, false, KeyModifiers.Alt), InputEvent.Character('z') }),
        ("sgr_right_control", new[] { InputEvent.Mouse(6, 8, 3, true, KeyModifiers.Control),
            InputEvent.Mouse(6, 8, 3, false, KeyModifiers.Control), InputEvent.Character('z') })
    };
    foreach (var test in cases) {
        Report(new { phase = "ready", name = test.Name, count = test.Expected.Length });
        foreach (var expected in test.Expected) {
            InputEvent? actual;
            do { actual = lease.Source.Read(TimeSpan.FromMilliseconds(250), CancellationToken.None); }
            while (actual == null);
            Report(new { phase = "event", name = test.Name, value = Describe(actual) });
            string wanted = JsonSerializer.Serialize(Describe(expected));
            string got = JsonSerializer.Serialize(Describe(actual));
            if (got != wanted) throw new InvalidOperationException(test.Name + ": expected " + wanted + "; got " + got);
        }
        Report(new { phase = "pass", name = test.Name, count = test.Expected.Length });
    }
} catch (Exception ex) {
    result = 1;
    Report(new { phase = "failure", error = ex.ToString() });
} finally {
    lease?.Dispose();
    Report(new { phase = "disposed", acquired = ConsoleInput.IsAcquired, result });
    // Keep the runtime alive while the parent observes termios after lease disposal.
    if (control.ReadByte() != 1) result = 1;
}
Report(new { phase = "finished", result });
return result;

#endif
        }
    }
}
