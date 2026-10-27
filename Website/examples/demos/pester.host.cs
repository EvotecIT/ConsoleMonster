using System.Management.Automation;
using ConsoleMonster.PowerShell.Pester;

// A compiled cmdlet: the current PowerShell runspace owns native commands and callbacks.
[Cmdlet(VerbsLifecycle.Invoke, "PesterExample") ]
public sealed class PesterExample : PSCmdlet {
    protected override void ProcessRecord() {
        using var runner = new ConsolePester(this, new ConsolePesterOptions { Path = new[] { "./fixtures" }, Title = "Mail domain checks" });
        runner.Run();
    }
}
