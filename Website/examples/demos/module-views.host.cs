using System.Management.Automation;
using ConsoleMonster.PowerShell;

// A compiled cmdlet: the current PowerShell runspace owns native commands and callbacks.
[Cmdlet(VerbsLifecycle.Invoke, "ModuleViewsExample")]
public sealed class ModuleViewsExample : PSCmdlet
{
    protected override void ProcessRecord()
    {
        InvokeCommand.InvokeScript("Import-Module ./Contoso.Servers");
        ConsoleHostViews.Import(this, "./Contoso.Servers/Contoso.Servers.consoleviews.psd1");
        using var output = new ConsoleDefaultOutput(
            this,
            new ConsoleDefaultOutputOptions { Force = true });
        foreach (var row in InvokeCommand.InvokeScript("Get-ContosoServer"))
        {
            output.Write(row);
        }

        output.Complete();
    }
}
