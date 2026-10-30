using System.Management.Automation;
using ConsoleMonster.PowerShell;

// A compiled cmdlet: the current PowerShell runspace owns native commands and callbacks.
[Cmdlet(VerbsLifecycle.Invoke, "ProgressExample") ]
public sealed class ProgressExample : PSCmdlet {
    protected override void ProcessRecord() {
        ConsoleHostFormatting.Enable(
            this,
            new ConsoleFormattingOptions {
                Include = new[] { ConsoleFormattingFeature.Progress },
                Exclude = new[] { ConsoleFormattingFeature.Output, ConsoleFormattingFeature.FormatCommands,
                    ConsoleFormattingFeature.Errors, ConsoleFormattingFeature.Streams, ConsoleFormattingFeature.ReadLine }
            });
        try { InvokeCommand.InvokeScript(@"Use-ConsoleProgress {
        $sites = 'Warsaw', 'Dublin', 'Berlin'
        for ($i = 0; $i -lt $sites.Count; $i++) {
            Write-Progress -Id 1 -Activity 'Collecting inventory' -Status $sites[$i] -PercentComplete ($i * 100 / $sites.Count)
            foreach ($server in 1..12) {
                Write-Progress -Id 2 -ParentId 1 -Activity ""Servers in $($sites[$i])"" -Status ""SRV$server"" -PercentComplete ($server * 100 / 12)
                Wait-ConsoleDashboard -Seconds 0.11
            }
            Write-Host ""$($sites[$i]) done: 12 servers""
        }
        Write-Progress -Id 2 -Activity 'Servers' -Completed
        Write-Progress -Id 1 -Activity 'Collecting inventory' -Completed
        } -Keep
        Write-ConsoleText 'Inventory collected: 36 servers across 3 sites.'"); }
        finally { ConsoleHostFormatting.Disable(
            this,
            new[] { ConsoleFormattingFeature.Progress }); }
    }
}
