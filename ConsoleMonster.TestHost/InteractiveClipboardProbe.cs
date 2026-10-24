using System;
using System.IO;
using System.Text;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;
using ConsoleMonster.Live;
using ConsoleMonster.Output;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.TestHost {
    /// <summary>Exercises the default form/menu copy actions while console capture is active.</summary>
    internal static class InteractiveClipboardProbe {
        internal const string Command = "Invoke-Thing zażółć 日本";

        internal static void Run(string scenario, string path) {
            using (MemoryStream bytes = new MemoryStream())
            using (StreamTerminalWriter writer = new StreamTerminalWriter(bytes)) {
                ITerminalTarget target = scenario == "console" ? (ITerminalTarget)ConsoleTarget.Instance : new BufferedTarget(writer);
                // The ambient target is deliberately different from the interaction's explicit target.
                using (Terminal.Use(NullTarget.Instance))
                using (LiveSession session = new LiveSession(new LogRegion("Captured"), new LiveSessionOptions { Target = NullTarget.Instance, TaskbarProgress = false })) {
                    session.Start();
                    InteractiveOptions options = new InteractiveOptions {
                        Target = target, Capture = new ScreenCapture(80, 24), Mode = InteractionMode.Rich,
                        Profile = TerminalProfile.Fixed(80, 24), Labels = new PromptLabels(),
                        Input = new ScriptedInput(scenario == "menu" ? "Enter Enter Ctrl+Q" : "PageDown Right Enter Ctrl+C") { Paced = true }
                    };
                    if (scenario == "menu") {
                        Menu menu = new Menu("Copy probe");
                        menu.Item("Review", item => item.Review(_ => new ReviewModel { Command = Command }));
                        menu.Run(new MenuOptions { Interactive = options, Layout = MenuLayout.Shell, ConfirmExit = false, StorageRoot = Path.GetDirectoryName(path) });
                    } else {
                        new Form("Copy probe") { Command = Command }.Show(new FormOptions { Interactive = options });
                    }
                    session.Stop();
                }
                // No caller flush: the copy action itself must flush its borrowed writer.
                File.WriteAllBytes(path, bytes.ToArray());
            }
        }

        private sealed class BufferedTarget : ITerminalTarget {
            internal BufferedTarget(ITerminalWriter writer) { Writer = writer; }
            public string Name => "Buffered clipboard probe";
            public TerminalProfile Profile => TerminalProfile.Fixed(80, 24);
            public ITheme? Theme => null;
            public ITerminalWriter? Writer { get; }
            public IInputSource? Input => null;
        }
    }
}
