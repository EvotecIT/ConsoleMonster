using System.Management.Automation;
using ConsoleMonster;
using ConsoleMonster.PowerShell;

// A compiled cmdlet: the current PowerShell runspace owns native commands and callbacks.
[Cmdlet(VerbsLifecycle.Invoke, "ShapingExample") ]
public sealed class ShapingExample : PSCmdlet {
    protected override void ProcessRecord() {
        var rows = InvokeCommand.InvokeScript(@"$servers = @(
            [pscustomobject] @{ Name = 'DC01'; Site = 'Warsaw'; Roles = 'DNS', 'GC', 'PDC'; LastSync = (New-TimeSpan -Seconds 42); Status = 'Pass' }
            [pscustomobject] @{ Name = 'DC02'; Site = 'Warsaw'; Roles = 'DNS', 'GC'; LastSync = (New-TimeSpan -Minutes 42); Status = 'Warn' }
            [pscustomobject] @{ Name = 'DC03'; Site = 'Dublin'; Roles = 'DNS', 'GC', 'RID', 'Infrastructure', 'Schema', 'Naming'; LastSync = (New-TimeSpan -Seconds 64); Status = 'Pass' }
            [pscustomobject] @{ Name = 'DC04'; Site = 'Dublin'; Roles = @(); LastSync = (New-TimeSpan -Hours 4); Status = 'Fail' }
        )

        $servers").ToList();
        Terminal.Write(ConsoleHostViews.Table(
            rows,
            new ConsoleTableViewOptions { GroupBy = "Site" }));
    }
}
