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
        // The capture runner sends Ctrl+C through its terminal after the host reports its first batch.
        await ConsoleMonsterApp.Program.Main(new[] { "serve" });
        return 0;
    }
}
