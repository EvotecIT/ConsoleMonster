using System.Management.Automation;
using ConsoleMonster.PowerShell;

// A compiled cmdlet: the current PowerShell runspace owns native commands and callbacks.
[Cmdlet(VerbsLifecycle.Invoke, "FormattingExample") ]
public sealed class FormattingExample : PSCmdlet {
    protected override void ProcessRecord() {
        const string inventory = @"# Invented services: the same inventory before and after formatting is enabled.
$services = @(
    [pscustomobject] @{ Name = 'WebApi'; Status = 'Running'; StartType = 'Automatic' }
    [pscustomobject] @{ Name = 'Worker'; Status = 'Running'; StartType = 'Automatic' }
    [pscustomobject] @{ Name = 'Archive'; Status = 'Stopped'; StartType = 'Manual' }
)
";
        InvokeCommand.InvokeScript(inventory + @"& {
        # PowerShell's own output
        $services
        [version] '2.0.0'
        [ordered] @{ Domain = 'evotec.xyz'; Records = 14; DNSSEC = $false }


        } | Microsoft.PowerShell.Core\Out-Default");
        ConsoleHostFormatting.Enable(
            this,
            new ConsoleFormattingOptions { Feature = new[] { ConsoleFormattingFeature.Output } });
        try {
            using var output = new ConsoleDefaultOutput(
                this,
                new ConsoleDefaultOutputOptions { Force = true });
            foreach (var item in InvokeCommand.InvokeScript(inventory + @"# The same objects now
        $services
        [version] '2.0.0'
        [ordered] @{ Domain = 'evotec.xyz'; Records = 14; DNSSEC = $false }")) { output.Write(item); }
            output.Complete();
            var services = InvokeCommand.InvokeScript(inventory + "$services").ToList();
            ConsoleMonster.Terminal.Write(ConsoleHostViews.Table(services,
                new ConsoleTableViewOptions { GroupBy = "Status" }, table => table.MaxRows = 6));
        } finally { ConsoleHostFormatting.Disable(
            this,
            new[] { ConsoleFormattingFeature.Output }); }
    }
}
