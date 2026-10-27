using ConsoleMonster;
using ConsoleMonster.Widgets;

// PowerShell supplies an ErrorRecord; a .NET application supplies its exception to the same error view.
var missing = new FileNotFoundException("The weekly report could not be loaded.",
    "Weekly-2026-43.pdf");
Terminal.Write(new ExceptionView(missing) { ShowStack = false });
