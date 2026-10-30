using System.Management.Automation;
using ConsoleMonster.PowerShell;

// A compiled cmdlet: the current PowerShell runspace owns native commands and callbacks.
[Cmdlet(VerbsLifecycle.Invoke, "FormatCommandsExample") ]
public sealed class FormatCommandsExample : PSCmdlet {
    protected override void ProcessRecord() {
        const string inventory = @"# Invented services: the same inventory before and after formatting is enabled.
$services = @(
    [pscustomobject] @{ Name = 'WebApi'; Status = 'Running'; StartType = 'Automatic' }
    [pscustomobject] @{ Name = 'Worker'; Status = 'Running'; StartType = 'Automatic' }
    [pscustomobject] @{ Name = 'Archive'; Status = 'Stopped'; StartType = 'Manual' }
)
";
        InvokeCommand.InvokeScript(inventory + @"& {
        # Format-Table, Format-List and Format-Wide as PowerShell draws them
        $services | Format-Table Name, Status, StartType -AutoSize
        $services[0] | Format-List Name, Status, StartType
        $services | Format-Wide Name -Column 3


        } | Microsoft.PowerShell.Core\Out-Default");
        ConsoleHostFormatting.Enable(
            this,
            new ConsoleFormattingOptions
            {
                Include = new[] { ConsoleFormattingFeature.FormatCommands },
                Exclude = new[] { ConsoleFormattingFeature.Output, ConsoleFormattingFeature.Errors,
                    ConsoleFormattingFeature.Streams, ConsoleFormattingFeature.ReadLine }
            });
        try { InvokeCommand.InvokeScript(inventory + @"# The same commands, drawn by ConsoleMonster
        $services | Format-Table Name, Status, StartType -AutoSize
        $services[0] | Format-List Name, Status, StartType
        $services | Format-Wide Name -Column 3"); }
        finally { ConsoleHostFormatting.Disable(
            this,
            new[] { ConsoleFormattingFeature.FormatCommands }); }
    }
}
