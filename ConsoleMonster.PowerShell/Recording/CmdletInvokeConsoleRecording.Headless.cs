using ConsoleMonster.Headless;
using ConsoleMonster.Sessions;

namespace ConsoleMonster.PowerShell {
    public sealed partial class CmdletInvokeConsoleRecording {
        // The script runs on this pipeline thread with a headless terminal as its ambient target, so prompts, menus, apps
        // and dashboards draw there; the keys are queued first and input ends after them, so an app finishes on its own.
        private string RecordHeadless(SessionRecordingOptions options) {
            HeadlessTerminal terminal = HeadlessTerminal.LikeConsole(Terminal.ResolveWidth(Width), Height);
            using (SessionRecorder recorder = terminal.Record(options)) {
                if (!string.IsNullOrEmpty(InputScript)) { terminal.Send(InputScript!); }
                terminal.CompleteInput();
                using (Terminal.Use(terminal)) { ScriptBlock.Invoke(); }
                return recorder.Path;
            }
        }
    }
}
