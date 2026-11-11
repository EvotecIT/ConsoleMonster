using System;
using System.Management.Automation;
using ConsoleMonster.PowerShell.Display;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Runs a script inside a collapsible CI log group or beneath a muted terminal heading.</para>
    /// <para type="description">Uses GitHub Actions, Azure Pipelines or GitLab CI commands when detected. CONSOLEMONSTER_CI can force github, azure, gitlab or none. Groups nest and close in finally, including when the script throws. The script runs with the caller's variables and returns its output.</para>
    /// </summary>
    /// <example><para>Group a build log</para><code>Use-ConsoleGroup 'Build' { dotnet build }</code></example>
    [Cmdlet(VerbsOther.Use, "ConsoleGroup")]
    [OutputType(typeof(object))]
    public sealed class CmdletUseConsoleGroup : NoPipelineInputCmdlet {
        /// <summary><para type="description">The literal log heading.</para></summary>
        [Parameter(Mandatory = true, Position = 0)]
        public string Title { get; set; } = string.Empty;

        /// <summary><para type="description">The work to run inside the group.</para></summary>
        [Parameter(Mandatory = true, Position = 1)]
        public ScriptBlock ScriptBlock { get; set; } = null!;

        /// <inheritdoc />
        protected override void EndProcessing() {
            bool provider = ContinuousIntegration.Provider != ContinuousIntegrationProvider.None;
            using (ContinuousIntegration.Group(Title, line => {
                if (!provider) { HostOutput.WriteMarkup(this, "[muted]" + Text.Markup.Escape(line) + "[/]"); }
                else { HostOutput.WriteHostLine(this, line); }
            })) {
                foreach (PSObject item in ScriptBlock.InvokeWithContext(null, null)) { WriteObject(item); }
            }
        }
    }
}
