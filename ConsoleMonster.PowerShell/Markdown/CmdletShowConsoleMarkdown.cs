using System;
using System.IO;
using System.Management.Automation;
using System.Text;
using System.Threading;
using ConsoleMonster.Interactive;
using ConsoleMonster.Markdown;
using ConsoleMonster.Output;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Reads Markdown with native console tables, highlighted code and interactive paging.</para>
    /// <para type="description">Reads CommonMark with pipe tables through the optional OfficeIMO-backed add-on. Headings use accent rules, lists have hanging indents, and block quotes have a vertical bar. Code uses the registered highlighter and its language label. Links use OSC 8 when supported and show a muted URL otherwise. HTML stays literal, images become labelled links, and document content is never executed or fetched. Long documents page only when output goes directly to an interactive host. Piped, redirected, recorded and non-interactive output never acquires input. Pipeline strings are joined before parsing, preserving reference links and block structure. Works on Windows PowerShell 5.1 and PowerShell 7.</para>
    /// </summary>
    /// <example>
    /// <para>Read a local document, paging only when it exceeds the interactive screen</para>
    /// <code>Show-ConsoleMarkdown -Path .\README.md</code>
    /// </example>
    /// <example>
    /// <para>Render inline content without a pager</para>
    /// <code>Show-ConsoleMarkdown -Content '# Deployment notes' -NoPager</code>
    /// </example>
    /// <example>
    /// <para>Join pipeline source lines and return the complete plain rendering</para>
    /// <code>Get-Content .\deployment.md | Show-ConsoleMarkdown -Width 60 -AsString</code>
    /// </example>
    [Cmdlet(VerbsCommon.Show, "ConsoleMarkdown", DefaultParameterSetName = "Content")]
    [OutputType(typeof(string))]
    public sealed class CmdletShowConsoleMarkdown : PSCmdlet {
        private readonly StringBuilder _content = new StringBuilder();
        private bool _hasLine;
        private volatile bool _stopped;
        private CancellationTokenSource? _paging;

        /// <summary><para type="description">The Markdown source. Pipeline strings are joined with newlines before parsing.</para></summary>
        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true, ParameterSetName = "Content")]
        [AllowEmptyString]
        [Alias("Text", "InputObject")]
        public string Content { get; set; } = string.Empty;

        /// <summary><para type="description">A literal file path, resolved relative to the current PowerShell FileSystem location.</para></summary>
        [Parameter(Mandatory = true, ParameterSetName = "Path")]
        [ValidateNotNullOrEmpty]
        [Alias("FullName", "LiteralPath")]
        public string Path { get; set; } = string.Empty;

        /// <summary><para type="description">The layout width in terminal cells. Omit it to use the current terminal width.</para></summary>
        [Parameter]
        [ValidateRange(1, 32767)]
        public int Width { get; set; }

        /// <summary><para type="description">Prints the entire document without acquiring interactive input.</para></summary>
        [Parameter]
        public SwitchParameter NoPager { get; set; }

        /// <summary><para type="description">Returns the complete view as one plain string and never opens the pager.</para></summary>
        [Parameter]
        public SwitchParameter AsString { get; set; }

        /// <inheritdoc />
        protected override void ProcessRecord() {
            if (ParameterSetName != "Content") { return; }
            if (_hasLine) { _content.Append('\n'); }
            _content.Append(Content);
            _hasLine = true;
        }

        /// <inheritdoc />
        protected override void EndProcessing() {
            using TerminalGuard outputMode = new TerminalGuard(() => { }, false);
            using CancellationTokenSource stop = new CancellationTokenSource();
            _paging = stop;
            try {
                if (_stopped) { stop.Cancel(); }
                MarkdownView view = MarkdownConsole.Parse(ReadContent());
                if (!NoPager && !AsString && FormatCommandRouter.IsDirectHostOutput(this)) {
                    InteractiveOptions options = PSInteractive.Options(this, null);
                    if (Width > 0) {
                        var profile = options.Profile ?? Terminal.GetCurrentProfile();
                        options.Profile = profile.WithSize(Width, profile.Height);
                    }
                    if (WidgetPager.TryShow(view, PSMarkdownMessages.Get("Title"), options, stop.Token)) { return; }
                }
                stop.Token.ThrowIfCancellationRequested();
                PSFormatting.Emit(this, view, Width, null, AsString.IsPresent);
            } catch (OperationCanceledException) when (_stopped) {
                // PowerShell owns pipeline-stop reporting; the pager has already released its surface and input.
            } finally {
                _paging = null;
            }
        }

        private string ReadContent() {
            if (ParameterSetName != "Path") { return _content.ToString(); }
            string path = SessionState.Path.GetUnresolvedProviderPathFromPSPath(Path, out ProviderInfo provider, out PSDriveInfo _);
            if (!string.Equals(provider.Name, "FileSystem", StringComparison.OrdinalIgnoreCase)) {
                throw new PSArgumentException(PSMarkdownMessages.Get("FileSystemPath"));
            }
            return File.ReadAllText(path);
        }

        /// <inheritdoc />
        protected override void StopProcessing() {
            _stopped = true;
            try { _paging?.Cancel(); } catch (ObjectDisposedException) { }
            base.StopProcessing();
        }
    }
}
