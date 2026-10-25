using ConsoleMonster.Input;
using ConsoleMonster.Interactive;

// Browse fictional services; this demo never changes the local machine.
var services = new Grid("Service browser");
services.AddColumn("Name").AddColumn("Status").AddColumn("StartType");
services.AddRow("Spooler", "Spooler", "Running", "Automatic");
services.AddRow("WinRM", "WinRM", "Running", "Automatic");
services.AddRow("WSearch", "WSearch", "Stopped", "Manual");

var restart = new AppCommand("Restart", "Restart", async (context, cancellation) => {
    foreach (object? row in context.Rows) {
        context.Write($"Stopping {row} in Lab");
        await Task.Delay(200, cancellation);
        context.Write($"Starting {row} in Lab");
        await Task.Delay(200, cancellation);
        context.Write($"{row}: Running");
    }
}) {
    Shortcut = new KeyChord(System.ConsoleKey.F5),
    ConfirmPrompt = "Restart the selected services?"
};
var options = new GridOptions();
options.Commands.Add(restart);
services.Show(options);
