using System;
using System.IO;
using System.Management.Automation;
using System.Text;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Exports one embedded demo as a copyable PowerShell script.</para>
    /// <para type="description">Use Get-ConsoleDemo to list slugs. Saves the canonical script as UTF-8 with a BOM for PowerShell 5.1. The destination folder must exist. Existing files are preserved unless Force is explicit.</para>
    /// </summary>
    /// <example><para>Save a demo to customize.</para><code>Export-ConsoleDemo assessment -Path ./assessment.ps1</code></example>
    [Cmdlet(VerbsData.Export, "ConsoleDemo", SupportsShouldProcess = true)]
    [OutputType(typeof(FileInfo))]
    public sealed class CmdletExportConsoleDemo : NoPipelineInputCmdlet {
        /// <summary><para type="description">The exact gallery slug.</para></summary>
        [Parameter(Mandatory = true, Position = 0)] [ValidateNotNullOrEmpty]
        public string Slug { get; set; } = string.Empty;
        /// <summary><para type="description">Literal filesystem destination for the script.</para></summary>
        [Parameter(Mandatory = true)] [ValidateNotNullOrEmpty]
        public string Path { get; set; } = string.Empty;
        /// <summary><para type="description">Allows replacing an existing destination file.</para></summary>
        [Parameter] public SwitchParameter Force { get; set; }
        /// <summary><para type="description">Returns the saved file.</para></summary>
        [Parameter] public SwitchParameter PassThru { get; set; }
        /// <inheritdoc />
        protected override void ProcessWithoutPipelineInput() {
            string? slug = PSDemoResources.Find(Slug);
            if (slug == null) { NotFound(); return; }
            try {
                string path = SessionState.Path.GetUnresolvedProviderPathFromPSPath(Path, out ProviderInfo provider, out _);
                if (!string.Equals(provider.Name, "FileSystem", StringComparison.OrdinalIgnoreCase)) {
                    throw new ArgumentException("The demo destination must be a filesystem path.");
                }
                if (!ShouldProcess(path, "Export ConsoleMonster demo " + slug)) { return; }
                FileInfo saved = ConsoleGallery.Export(slug, path, Force.IsPresent);
                if (PassThru.IsPresent) { WriteObject(saved); }
            } catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException || ex is NotSupportedException) {
                ThrowTerminatingError(new ErrorRecord(ex, "ConsoleDemoExportFailed", ErrorCategory.WriteError, Path));
            }
        }

        private void NotFound() => ThrowTerminatingError(new ErrorRecord(
            new ItemNotFoundException("Unknown ConsoleMonster demo '" + Slug + "'. Use Get-ConsoleDemo to list available slugs."),
            "ConsoleDemoNotFound", ErrorCategory.ObjectNotFound, Slug));
    }
}
