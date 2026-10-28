using System;
using System.IO;
using System.Management.Automation;
using ConsoleMonster.Changes;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Compares text or files with numbered lines, highlighted words and collapsed unchanged regions.</para>
    /// <para type="description">Shows the reference and difference side by side at 120 columns or more, and inline in narrower terminals. Text headings default to Before and After; file headings use the resolved filenames. ReferenceName and DifferenceName override these headings in both the widget and unified output. Minus and plus markers distinguish changes without colour. Context controls the unchanged lines around changes. CRLF, LF and CR endings compare equally; a missing final newline remains significant. Large inputs use bounded comparison work and linear auxiliary memory, so a large changed range may produce a non-minimal diff. AsUnified returns plain unified diff text instead of rendering.</para>
    /// </summary>
    /// <example>
    /// <para>Compare configuration strings with one line of context.</para>
    /// <code>Compare-ConsoleText -ReferenceText $before -DifferenceText $after -Context 1</code>
    /// </example>
    /// <example>
    /// <para>Compare files using the current PowerShell location and ignore whitespace within lines.</para>
    /// <code>Compare-ConsoleText -ReferencePath .\before.txt -DifferencePath .\after.txt -IgnoreWhitespace</code>
    /// </example>
    /// <example>
    /// <para>Return a unified diff with custom side names for storage or further processing.</para>
    /// <code>Compare-ConsoleText -ReferenceText $before -DifferenceText $after -ReferenceName 'Deployed' -DifferenceName 'Proposed' -AsUnified | Set-Content .\changes.diff</code>
    /// </example>
    [Cmdlet(VerbsData.Compare, "ConsoleText", DefaultParameterSetName = "Text")]
    [OutputType(typeof(string))]
    public sealed class CmdletCompareConsoleText : NoPipelineInputCmdlet {
        /// <summary><para type="description">The original text. Empty strings and null represent an empty document.</para></summary>
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "Text")]
        [AllowEmptyString]
        [AllowNull]
        public string? ReferenceText { get; set; }

        /// <summary><para type="description">The new text. Empty strings and null represent an empty document.</para></summary>
        [Parameter(Mandatory = true, Position = 1, ParameterSetName = "Text")]
        [AllowEmptyString]
        [AllowNull]
        public string? DifferenceText { get; set; }

        /// <summary><para type="description">The original file's PowerShell path, relative to the current location or qualified by a filesystem drive. Wildcards must resolve to exactly one file.</para></summary>
        [Parameter(Mandatory = true, ParameterSetName = "Path")]
        [ValidateNotNullOrEmpty]
        public string? ReferencePath { get; set; }

        /// <summary><para type="description">The new file's PowerShell path. The provider must be FileSystem and the path must resolve to exactly one readable file. Files are read as UTF-8 with BOM detection.</para></summary>
        [Parameter(Mandatory = true, ParameterSetName = "Path")]
        [ValidateNotNullOrEmpty]
        public string? DifferencePath { get; set; }

        /// <summary><para type="description">The original-text heading in the widget and unified output. Overrides the resolved reference filename for files or Before for text. Control characters are replaced by spaces.</para></summary>
        [Parameter]
        [ValidateNotNullOrEmpty]
        public string? ReferenceName { get; set; }

        /// <summary><para type="description">The new-text heading in the widget and unified output. Overrides the resolved difference filename for files or After for text. Control characters are replaced by spaces.</para></summary>
        [Parameter]
        [ValidateNotNullOrEmpty]
        public string? DifferenceName { get; set; }

        /// <summary><para type="description">The nonnegative number of unchanged lines shown before and after each change. Default three; zero shows changes only.</para></summary>
        [Parameter]
        [ValidateRange(0, int.MaxValue)]
        public int Context { get; set; } = 3;

        /// <summary><para type="description">Ignores Unicode whitespace within lines. Line boundaries and a missing final newline are still compared.</para></summary>
        [Parameter]
        public SwitchParameter IgnoreWhitespace { get; set; }

        /// <summary><para type="description">Returns a single plain unified diff string with LF separators and hunk ranges. Equal texts return an empty string. Without this switch, writes a widget to the host and returns no objects.</para></summary>
        [Parameter]
        public SwitchParameter AsUnified { get; set; }

        /// <inheritdoc />
        protected override void ProcessWithoutPipelineInput() {
            string? before = ReferenceText;
            string? after = DifferenceText;
            TextDiffOptions options = new TextDiffOptions { Context = Context, IgnoreWhitespace = IgnoreWhitespace.IsPresent };
            if (ParameterSetName == "Path") {
                try {
                    before = ReadText(ReferencePath!, out string referenceName);
                    after = ReadText(DifferencePath!, out string differenceName);
                    options.ReferenceLabel = referenceName;
                    options.DifferenceLabel = differenceName;
                } catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException ||
                    exception is ArgumentException || exception is RuntimeException) {
                    ThrowTerminatingError(new ErrorRecord(exception, "ConsoleTextFileReadFailed", ErrorCategory.ReadError, ReferencePath + " / " + DifferencePath));
                    return;
                }
            }
            if (ReferenceName != null) { options.ReferenceLabel = ReferenceName; }
            if (DifferenceName != null) { options.DifferenceLabel = DifferenceName; }
            TextDiffResult comparison = TextDiff.Compare(before, after, options);
            if (AsUnified.IsPresent) {
                WriteObject(comparison.ToUnifiedText());
            } else {
                PSFormatting.Write(this, new TextDiffView(comparison), 0);
            }
        }

        private string ReadText(string path, out string name) {
            var paths = SessionState.Path.GetResolvedProviderPathFromPSPath(path, out ProviderInfo provider);
            if (!string.Equals(provider.Name, "FileSystem", StringComparison.OrdinalIgnoreCase) || paths.Count != 1) {
                throw new ArgumentException("A text comparison path must resolve to exactly one FileSystem file.", nameof(path));
            }
            name = Path.GetFileName(paths[0]);
            return File.ReadAllText(paths[0]);
        }
    }
}
