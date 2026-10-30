using System.Management.Automation;
using ConsoleMonster.PowerShell;

// A compiled cmdlet: the current PowerShell runspace owns native commands and callbacks.
[Cmdlet(VerbsLifecycle.Invoke, "FormattingExample") ]
public sealed class FormattingExample : PSCmdlet {
    protected override void ProcessRecord() {
        const string inventory = @"# Invented services: the same inventory before and after formatting is enabled.
$services = @(
    [pscustomobject] @{ PSTypeName = 'System.ServiceProcess.ServiceController'; ServiceName = 'WebApi'; Status = 'Running'; ServicesDependedOn = @(); DisplayName = 'Web API' }
    [pscustomobject] @{ PSTypeName = 'System.ServiceProcess.ServiceController'; ServiceName = 'Worker'; Status = 'Running'; ServicesDependedOn = @(); DisplayName = 'Background worker' }
    [pscustomobject] @{ PSTypeName = 'System.ServiceProcess.ServiceController'; ServiceName = 'Archive'; Status = 'Stopped'; ServicesDependedOn = @(); DisplayName = 'Archive' }
)
";
        InvokeCommand.InvokeScript(inventory + @"& {
        # PowerShell's own output
        $services
        [version] '2.0.0'
        [ordered] @{ Domain = 'evotec.xyz'; Records = 14; DNSSEC = $false }


        } | Microsoft.PowerShell.Core\Out-Default");
        InvokeCommand.InvokeScript("Clear-Host");
        ConsoleHostFormatting.Enable(
            this,
            new ConsoleFormattingOptions {
                Include = new[] { ConsoleFormattingFeature.Output },
                Exclude = new[]
                {
                    ConsoleFormattingFeature.FormatCommands,
                    ConsoleFormattingFeature.Errors,
                    ConsoleFormattingFeature.Streams,
                    ConsoleFormattingFeature.ReadLine
                }
            });
        try {
            using var output = new ConsoleDefaultOutput(
                this,
                new ConsoleDefaultOutputOptions { Force = true });
            foreach (var item in InvokeCommand.InvokeScript(inventory + @"# The same objects now
        $services
        [version] '2.0.0'
        [ordered] @{ Domain = 'evotec.xyz'; Records = 14; DNSSEC = $false }")) { output.Write(item); }
            output.Complete();
        } finally { ConsoleHostFormatting.Disable(
            this,
            new[] { ConsoleFormattingFeature.Output }); }
    }
}
