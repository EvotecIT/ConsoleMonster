using System;
using System.IO;
using System.Management.Automation;
using System.Text;

namespace ConsoleMonster.PowerShell {
    /// <summary>Lists gallery demos and returns their complete PowerShell source.</summary>
    /// <remarks><para>Returns each demo's title, category, tags, terminal size and Source. Slug accepts an exact name or wildcard pattern. Source is the complete script, ready to inspect or save with Set-Content.</para></remarks>
    /// <example><para>Browse the gallery.</para><code>Get-ConsoleDemo | Select-Object Slug, Title, Pitch</code></example>
    /// <example><para>List the welcome demos.</para><code>
    /// $demoOptions = @{
    ///     Slug = 'welcome-*'
    /// }
    /// Get-ConsoleDemo @demoOptions</code></example>
    /// <example><para>Read the complete assessment script. Source includes the sample data, layout and live updates as they are embedded in the module. Get-ConsoleDemo returns that text for inspection. Show-ConsoleDemo is the command that executes it.</para><code>
    /// $demo = Get-ConsoleDemo -Slug 'assessment'
    /// $demo.Source</code></example>
    [Cmdlet(VerbsCommon.Get, "ConsoleDemo", HelpUri = "https://consolemonster.dev/powershell/commands/get-consoledemo/")]
    [OutputType(typeof(PSObject))]
    public sealed class CmdletGetConsoleDemo : NoPipelineInputCmdlet {
        /// <summary><para type="description">Slug or wildcard pattern to list. Export requires one exact slug.</para></summary>
        [Parameter(Position = 0)]
        [ValidateNotNullOrEmpty]
        [SupportsWildcards]
        [PSDefaultValue(Help = "*")]
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
