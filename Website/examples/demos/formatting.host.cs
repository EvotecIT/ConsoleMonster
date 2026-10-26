using System.Management.Automation;
using ConsoleMonster.PowerShell;

// A compiled cmdlet: the current PowerShell runspace owns native commands and callbacks.
[Cmdlet(VerbsLifecycle.Invoke, "FormattingExample") ]
public sealed class FormattingExample : PSCmdlet {
    protected override void ProcessRecord() {
        InvokeCommand.InvokeScript(@"& {
        # PowerShell's own output
        Get-Process svchost | Sort-Object CPU -Descending | Select-Object -First 8
        Get-ChildItem
        @{ Domain = 'evotec.xyz'; Records = 14; DNSSEC = $false }


        } | Microsoft.PowerShell.Core\Out-Default");
        ConsoleHostFormatting.Enable(
            this,
            new ConsoleFormattingOptions { Feature = new[] { ConsoleFormattingFeature.Output } }
        );
        try {
            using var output = new ConsoleDefaultOutput(
                this,
                new ConsoleDefaultOutputOptions { Force = true }
            );
            foreach (var item in InvokeCommand.InvokeScript(@"# The same commands now
        Get-Process svchost | Sort-Object CPU -Descending | Select-Object -First 8
        Get-ChildItem
        @{ Domain = 'evotec.xyz'; Records = 14; DNSSEC = $false }")) { output.Write(item); }
            output.Complete();
            var services = InvokeCommand.InvokeScript("Get-Service W*").ToList();
            ConsoleMonster.Terminal.Write(ConsoleHostViews.Table(services,
                new ConsoleTableViewOptions { GroupBy = "Status" }, table => table.MaxRows = 6));
        } finally { ConsoleHostFormatting.Disable(this, new[] { ConsoleFormattingFeature.Output }); }
    }
}
