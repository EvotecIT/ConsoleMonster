using System;
using System.IO;
using System.Management.Automation;
using System.Text;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Lists the copyable ConsoleMonster gallery demos .</para>
    /// <para type="description">Returns the embedded gallery metadata, including slug, title, pitch, category, tags, terminal size, runtimes, add-ons, customization ideas, mode and snapshot count. Listing accepts a slug or wildcard pattern. Use Export-ConsoleDemo to save a script.</para>
    /// </summary>
    /// <example><para>Browse the gallery.</para><code>Get-ConsoleDemo | Select-Object Slug, Title, Pitch</code></example>
    /// <example><para>List the welcome demos.</para><code>Get-ConsoleDemo 'welcome-*'</code></example>
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
