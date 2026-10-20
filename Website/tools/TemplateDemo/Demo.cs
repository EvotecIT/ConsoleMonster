using System.Runtime.InteropServices;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;

namespace TemplateDemo;

/// <summary>
/// The demo of the template app: its menu (the status report, the service page, Exit) driven by scripted keys, then
/// <c>dotnet run -- serve</c>, the generic host with the live console dashboard, stopped with Ctrl+C after a few batches.
/// </summary>
internal static class Demo {
    public static async Task<int> Main() {
        InteractiveOptions.Default.Input = new ScriptedInput("wait:1500 Enter wait:3500 Escape wait:900 Down wait:800 Enter wait:3500 Escape wait:900 Down wait:800 Enter") { RealTime = true };
        await ConsoleMonsterApp.Program.Main(Array.Empty<string>());
        // Ctrl+C reaches the host the way it does when a user presses it, so the dashboard ends as it would.
        _ = Task.Run(async () => {
            await Task.Delay(TimeSpan.FromSeconds(9));
            GenerateConsoleCtrlEvent(0, 0);
        });
        await ConsoleMonsterApp.Program.Main(new[] { "serve" });
        return 0;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GenerateConsoleCtrlEvent(uint ctrlEvent, uint processGroupId);
}