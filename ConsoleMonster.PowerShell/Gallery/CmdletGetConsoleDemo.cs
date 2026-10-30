using System;
using System.IO;
using System.Management.Automation;
using System.Text;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Lists gallery demos and returns their complete PowerShell source.</para>
    /// <para type="description">Returns each demo's title, category, tags, terminal size and Source. Slug accepts an exact name or wildcard pattern. Source is the complete script, ready to inspect or save with Set-Content.</para>
    /// </summary>
    /// <example><para>Browse the gallery.</para><code>Get-ConsoleDemo | Select-Object Slug, Title, Pitch</code></example>
    /// <example><para>List the welcome demos.</para><code>Get-ConsoleDemo 'welcome-*'</code></example>
    /// <example><para>Read a complete demo without running it.</para><code>(Get-ConsoleDemo assessment).Source</code></example>
    [Cmdlet(VerbsCommon.Get, "ConsoleDemo")]
    [OutputType(typeof(PSObject))]
    public sealed class CmdletGetConsoleDemo : NoPipelineInputCmdlet {
        /// <summary><para type="description">Slug or wildcard pattern to list. Export requires one exact slug.</para></summary>
        [Parameter(Position = 0)]
        [ValidateNotNullOrEmpty]
        [SupportsWildcards]
        public string Slug { get; set; } = "*";

        /// <inheritdoc />
        protected override void ProcessWithoutPipelineInput() {
            try {
                foreach (PSObject metadata in ConsoleGallery.List(Slug, InvokeCommand)) { WriteObject(metadata); }
            } catch (ItemNotFoundException error) {
                ThrowTerminatingError(new ErrorRecord(error, "ConsoleDemoNotFound", ErrorCategory.ObjectNotFound, Slug));
            }
        }

    }
}
