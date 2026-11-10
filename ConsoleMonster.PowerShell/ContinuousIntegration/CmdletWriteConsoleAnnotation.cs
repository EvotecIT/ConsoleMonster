using System.Management.Automation;

namespace ConsoleMonster.PowerShell {
    /// <summary>Writes a CI error, warning or notice, or a styled terminal status line.</summary>
    /// <remarks><para>GitHub Actions and Azure Pipelines receive native annotations with escaped literal text. Azure notices and GitLab annotations are ordinary lines. Other hosts show a status line with a source-file link where supported. This command does not change the process exit code or write to PowerShell's error stream.</para></remarks>
    /// <example><para>Point to a build error</para><code>Write-ConsoleAnnotation -Level Error -Message 'Missing configuration' -File ./build.ps1 -Line 12 -Column 5</code></example>
    /// <example>
    /// <para>Highlight an inventory warning in a CI log</para>
    /// <code>$result = [pscustomobject]@{ Name = 'Inventory'; Detail = 'Two servers need an update' }
    /// Write-ConsoleAnnotation -Level Warning -Title $result.Name -Message $result.Detail
    /// </code>
    /// </example>
    [Cmdlet(VerbsCommunications.Write, "ConsoleAnnotation", HelpUri = "https://consolemonster.dev/powershell/commands/write-consoleannotation/")]
    [OutputType(typeof(void))]
    public sealed class CmdletWriteConsoleAnnotation : NoPipelineInputCmdlet {
        /// <summary><para type="description">Error, Warning or Notice.</para></summary>
        [Parameter(Mandatory = true)] [PSDefaultValue(Help = "Error")]
        public AnnotationLevel Level { get; set; }
        /// <summary><para type="description">The literal annotation text.</para></summary>
        [Parameter(Mandatory = true)] [AllowEmptyString] public string Message { get; set; } = string.Empty;
        /// <summary><para type="description">An optional title; Azure and plain lines prepend it to the message.</para></summary>
        [Parameter] public string? Title { get; set; }
        /// <summary><para type="description">The optional source file; relative paths resolve from the current PowerShell location. Files inside the provider workspace use checkout-relative paths with forward slashes; outside paths stay as supplied. Terminal labels are relative to the current location and show the file and line once, linked where supported.</para></summary>
        [Parameter] public string? File { get; set; }
        /// <summary><para type="description">The one-based source line.</para></summary>
        [Parameter] [ValidateRange(1, int.MaxValue)] public int? Line { get; set; }
        /// <summary><para type="description">The one-based source column.</para></summary>
        [Parameter] [ValidateRange(1, int.MaxValue)] public int? Column { get; set; }

        /// <inheritdoc />
        protected override void EndProcessing() {
            string directory = SessionState.Path.CurrentFileSystemLocation.ProviderPath;
            string? resolvedFile = string.IsNullOrEmpty(File) ? null : SessionState.Path.GetUnresolvedProviderPathFromPSPath(File);
            if (ContinuousIntegration.Provider == ContinuousIntegrationProvider.None || ContinuousIntegration.Provider == ContinuousIntegrationProvider.GitLab) {
                var profile = Terminal.GetCurrentProfile();
                HostOutput.WriteMarkup(this, ContinuousIntegration.AnnotationMarkup(Level, Message, Title, File, Line, !HostOutput.UseHost(this, profile) && profile.SupportsHyperlinks, directory, resolvedFile));
            } else { ContinuousIntegration.Annotate(Level, Message, Title, File, Line, Column, text => HostOutput.WriteHostLine(this, text), directory, resolvedFile); }
        }
    }
}
