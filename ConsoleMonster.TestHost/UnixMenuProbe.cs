using System;
#if !NETFRAMEWORK
using ConsoleMonster.Capabilities;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;
using ConsoleMonster.Live;
using ConsoleMonster.Widgets;
#endif

namespace ConsoleMonster.TestHost {
    /// <summary>Exercises actual menu-to-live-work signal handling and resumed keyboard navigation on a Unix TTY.</summary>
    internal static class UnixMenuProbe {
#if !NETFRAMEWORK
        internal static void Run(Action<object> report) {
            bool cancelled = false;
            using ConsoleKeyInput input = new ConsoleKeyInput();
            while (input.Read(TimeSpan.Zero, System.Threading.CancellationToken.None) != null) { }
            Menu menu = new Menu("Input lifetime probe");
            menu.Item("Run", item => item.Action(context => {
                context.Run(new TextBlock("Working"), run => {
                    report(new { phase = "signal_ready" });
                    run.CancellationToken.WaitHandle.WaitOne();
                    cancelled = run.CancellationToken.IsCancellationRequested;
                    return new MenuRunResult("Stopped");
                }, new LiveSessionOptions { CaptureConsole = false, TaskbarProgress = false });
                report(new { phase = "menu_resumed", cancelled });
            }));
            menu.Item("Quit", item => item.Hotkey('q').Action(context => context.Exit()));
            report(new { phase = "menu_ready" });
            menu.Run(new MenuOptions {
                ConfirmExit = false,
                Layout = MenuLayout.Shell,
                Interactive = new InteractiveOptions {
                    Input = input,
                    Mode = InteractionMode.Rich,
                    Capture = new ScreenCapture(120, 40),
                    Profile = TerminalProfile.Fixed(120, 40, ColorDepth.TrueColor, GlyphTier.Full)
                }
            });
            if (!cancelled) { throw new InvalidOperationException("Keyboard Ctrl+C did not cancel the menu's live work."); }
            report(new { phase = "menu_pass" });
        }
#endif
    }
}
