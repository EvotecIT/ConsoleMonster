using ConsoleMonster.Input;
using ConsoleMonster.Interactive;
using ConsoleMonster.Live;
using ConsoleMonster.Testing;

namespace ConsoleMonster.Tests.Interactive.App {
    /// <summary>Memory-only app options shared by this assigned app test slice.</summary>
    internal static class AppTestHarness {
        internal static ConsoleAppOptions Options(string script, out ScreenCapture capture) {
            capture = new ScreenCapture(80, 24);
            InteractiveOptions interactive = TestTerminal.Interactive(script, capture);
            interactive.Target = TestTerminal.Target();
            return new ConsoleAppOptions {
                Interactive = interactive,
                Live = new LiveSessionOptions { CaptureConsole = false, TaskbarProgress = false }
            };
        }
    }
}
