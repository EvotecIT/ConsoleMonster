using System;
using System.IO;
using System.Management.Automation;
using System.Text;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Lists the copyable ConsoleMonster gallery demos or exports one PowerShell script.</para>
    /// <para type="description">Returns the embedded gallery metadata, including slug, title, pitch, category, tags, terminal size, runtimes, add-ons, customization ideas, mode and snapshot count. Listing accepts a slug or wildcard pattern. With -Path, an exact slug exports the canonical self-contained script as UTF-8; ConsoleMonster must be installed to run it. The parent directory must exist. Existing files are preserved unless -Force is supplied.</para>
    /// </summary>
    /// <example><para>Browse the gallery.</para><code>Get-ConsoleDemo | Select-Object Slug, Title, Pitch</code></example>
    /// <example><para>List the welcome demos.</para><code>Get-ConsoleDemo 'welcome-*'</code></example>
    /// <example><para>Export a demo to customize and run.</para><code>Get-ConsoleDemo assessment -Path ./assessment.ps1
    /// ./assessment.ps1 -StaticSnapshot -Step 0</code></example>
    [Cmdlet(VerbsCommon.Get, "ConsoleDemo", DefaultParameterSetName = "List", SupportsShouldProcess = true)]
    [OutputType(typeof(PSObject), ParameterSetName = new[] { "List" })]
    [OutputType(typeof(FileInfo), ParameterSetName = new[] { "Export" })]
    public sealed class CmdletGetConsoleDemo : PSCmdlet {
        /// <summary><para type="description">Slug or wildcard pattern to list. Export requires one exact slug.</para></summary>
        [Parameter(Position = 0, ParameterSetName = "List")]
        [Parameter(Position = 0, Mandatory = true, ParameterSetName = "Export")]
        [ValidateNotNullOrEmpty]
        [SupportsWildcards]
        public string Slug { get; set; } = "*";

        /// <summary><para type="description">Literal filesystem destination for the script. Returns the exported FileInfo.</para></summary>
        [Parameter(Mandatory = true, ParameterSetName = "Export")]
        [ValidateNotNullOrEmpty]
        public string Path { get; set; } = string.Empty;

        /// <summary><para type="description">Allows replacing the destination file when exporting.</para></summary>
        [Parameter(ParameterSetName = "Export")]
        public SwitchParameter Force { get; set; }

        /// <inheritdoc />
        protected override void ProcessRecord() {
            if (ParameterSetName == "Export") {
                Export();
                return;
            }

            var pattern = new WildcardPattern(Slug, WildcardOptions.IgnoreCase | WildcardOptions.CultureInvariant);
            bool found = false;
            foreach (string slug in PSDemoResources.Slugs()) {
                if (!pattern.IsMatch(slug)) { continue; }
                found = true;
                foreach (PSObject metadata in InvokeCommand.InvokeScript(
                    "param($json) Microsoft.PowerShell.Utility\\ConvertFrom-Json -InputObject $json -ErrorAction Stop",
                    true, System.Management.Automation.Runspaces.PipelineResultTypes.None, null, PSDemoResources.Metadata(slug))) {
                    WriteObject(metadata);
                }
            }
            if (!found && !WildcardPattern.ContainsWildcardCharacters(Slug)) { NotFound(); }
        }

        private void Export() {
            string? slug = PSDemoResources.Find(Slug);
            if (slug == null) { NotFound(); return; }
            try {
                string path = SessionState.Path.GetUnresolvedProviderPathFromPSPath(Path, out ProviderInfo provider, out _);
                if (!string.Equals(provider.Name, "FileSystem", StringComparison.OrdinalIgnoreCase)) {
                    throw new ArgumentException("The demo destination must be a filesystem path.");
                }
                string source = PSDemoResources.Script(slug);
                if (!ShouldProcess(path, "Export ConsoleMonster demo " + slug)) { return; }
                using (var stream = new FileStream(path, Force.IsPresent ? FileMode.Create : FileMode.CreateNew, FileAccess.Write))
                using (var writer = new StreamWriter(stream, new UTF8Encoding(true))) {
                    writer.Write(source);
                }
                WriteObject(new FileInfo(path));
            } catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException || ex is NotSupportedException) {
                ThrowTerminatingError(new ErrorRecord(ex, "ConsoleDemoExportFailed", ErrorCategory.WriteError, Path));
            }
        }

        private void NotFound() => ThrowTerminatingError(new ErrorRecord(
            new ItemNotFoundException("Unknown ConsoleMonster demo '" + Slug + "'. Use Get-ConsoleDemo to list available slugs."),
            "ConsoleDemoNotFound", ErrorCategory.ObjectNotFound, Slug));
    }
}
