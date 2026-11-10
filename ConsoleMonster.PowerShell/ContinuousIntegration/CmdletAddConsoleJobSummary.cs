using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Export;

namespace ConsoleMonster.PowerShell {
    /// <summary>Adds widgets or objects as Markdown to the CI job summary.</summary>
    /// <remarks><para>Uses the same semantic Markdown exporter as Export-ConsoleFrame. GitHub appends to GITHUB_STEP_SUMMARY. Azure writes a UTF-8 Markdown file in the job workspace and emits task.uploadsummary; the agent owns its later cleanup. GitLab and ordinary terminals write no files and return the Markdown so a script can save it. GitHub and Azure return no pipeline output.</para></remarks>
    /// <example><para>Add a result table to the job summary</para><code>@([pscustomobject]@{ Check = 'Build'; Result = 'Passed' }) | Add-ConsoleJobSummary</code></example>
    /// <example>
    /// <para>Add an inventory table to the CI summary</para>
    /// <code>$servers = @([pscustomobject]@{ Name = 'web01'; State = 'Pass' })
    /// $servers | New-ConsoleTable | Add-ConsoleJobSummary
    /// </code>
    /// </example>
    [Cmdlet(VerbsCommon.Add, "ConsoleJobSummary", HelpUri = "https://consolemonster.dev/powershell/commands/add-consolejobsummary/")]
    [OutputType(typeof(string))]
    public sealed class CmdletAddConsoleJobSummary : PSCmdlet {
        private readonly List<object> _items = new List<object>();
        /// <summary><para type="description">Widgets or objects to export as Markdown. Objects use the automatic view; multiple widgets are stacked.</para></summary>
        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true)] [AllowNull]
        public object? InputObject { get; set; }
        /// <inheritdoc />
        protected override void ProcessRecord() { if (InputObject != null) { _items.Add(InputObject); } }
        /// <inheritdoc />
        protected override void EndProcessing() {
            if (_items.Count == 0) { return; }
            string markdown = PSOutputExport.Document(_items, "Markdown", new ExportOptions(), this);
            ContinuousIntegrationProvider provider = ContinuousIntegration.Provider;
            ContinuousIntegration.AddSummary(markdown, text => HostOutput.WriteHostLine(this, text));
            if (provider == ContinuousIntegrationProvider.None || provider == ContinuousIntegrationProvider.GitLab) { WriteObject(markdown); }
        }
    }
}
