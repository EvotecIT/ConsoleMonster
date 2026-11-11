using System.Management.Automation;
using ConsoleMonster.PowerShell.Display;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Writes a CI error, warning or notice, or a styled terminal status line.</para>
    /// <para type="description">GitHub Actions and Azure Pipelines receive native annotations with escaped literal text. Azure notices and GitLab annotations are ordinary lines. Other hosts show a status line with a source-file link where supported. This command does not change the process exit code or write to PowerShell's error stream.</para>
    /// </summary>
    /// <example><para>Point to a build error</para><code>Write-ConsoleAnnotation -Level Error -Message 'Missing configuration' -File ./build.ps1 -Line 12 -Column 5</code></example>
    [Cmdlet(VerbsCommunications.Write, "ConsoleAnnotation")]
    [OutputType(typeof(void))]
    public sealed class CmdletWriteConsoleAnnotation : NoPipelineInputCmdlet {
        /// <summary><para type="description">Error, Warning or Notice.</para></summary>
        [Parameter(Mandatory = true)] public AnnotationLevel Level { get; set; }
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
