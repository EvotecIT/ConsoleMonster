using System.Management.Automation;
using ConsoleMonster.PowerShell;

// A compiled cmdlet: the current PowerShell runspace owns native commands and callbacks.
[Cmdlet(VerbsLifecycle.Invoke, "FormatCommandsExample")]
public sealed class FormatCommandsExample : PSCmdlet
{
    protected override void ProcessRecord()
    {
        InvokeCommand.InvokeScript(@"& {
        # Format-Table, Format-List and Format-Wide as PowerShell draws them
        Get-Service W* | Select-Object -First 8 | Format-Table Name, Status, StartType -AutoSize
        Get-Command Get-Service | Format-List Name, CommandType, Source, Version
        Get-ChildItem | Format-Wide Name -Column 3


        } | Microsoft.PowerShell.Core\Out-Default");
        ConsoleHostFormatting.Enable(
            this,
            new ConsoleFormattingOptions
            {
                Feature = new[] { ConsoleFormattingFeature.FormatCommands }
            });
        try
        {
            InvokeCommand.InvokeScript(@"# The same commands, drawn by ConsoleMonster
        Get-Service W* | Select-Object -First 8 | Format-Table Name, Status, StartType -AutoSize
        Get-Command Get-Service | Format-List Name, CommandType, Source, Version
        Get-ChildItem | Format-Wide Name -Column 3");
        }
        finally
        {
            ConsoleHostFormatting.Disable(this, new[] { ConsoleFormattingFeature.FormatCommands });
        }
    }
}
