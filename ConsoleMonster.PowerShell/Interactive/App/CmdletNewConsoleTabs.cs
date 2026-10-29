using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Creates retained tabs with keyboard and pointer navigation.</para>
    /// <para type="description">Combines pages from New-ConsoleTab in display order. Only the selected page receives input; switching pages retains each page's values and focus for the next visit.</para>
    /// </summary>
    /// <example><para>Compose two pages.</para><code>$pages | New-ConsoleTabs | Show-ConsoleApp</code></example>
    [Cmdlet(VerbsCommon.New, "ConsoleTabs")]
    [OutputType(typeof(Tabs))]
    public sealed class CmdletNewConsoleTabs : PSCmdlet {
        private readonly List<LayoutRegion> _pages = new List<LayoutRegion>();
        /// <summary><para type="description">Pages returned by New-ConsoleTab, in display order.</para></summary>
        [Parameter(Position = 0)] public LayoutRegion[] Tab { get; set; } = System.Array.Empty<LayoutRegion>();
        /// <summary>Pages from the pipeline.</summary>
        [Parameter(ValueFromPipeline = true, DontShow = true)] public PSObject? PipelineInput { get; set; }
        /// <summary><para type="description">Initial zero-based active page.</para></summary>
        [Parameter, ValidateRange(0, int.MaxValue)] public int SelectedIndex { get; set; }
        /// <inheritdoc />
        protected override void BeginProcessing() => _pages.AddRange(Tab);
        /// <inheritdoc />
        protected override void ProcessRecord() {
            if (PipelineInput == null) { return; }
            foreach (object item in PSItems.Items(PipelineInput)) {
                if (!(PSText.Unwrap(item) is LayoutRegion page)) {
                    ThrowTerminatingError(new ErrorRecord(new PSArgumentException("New-ConsoleTabs takes pages from New-ConsoleTab. To display objects, use Show-ConsoleTable."), "ExpectedConsoleTab", ErrorCategory.InvalidArgument, item));
                    return;
                }
                _pages.Add(page);
            }
        }
        /// <inheritdoc />
        protected override void EndProcessing() {
            if (_pages.Count == 0) { throw new PSArgumentException("New-ConsoleTabs needs at least one page from New-ConsoleTab."); }
            var tabs = new Tabs();
            foreach (LayoutRegion page in _pages) { tabs.Add(page.Key, page.Content); }
            tabs.SelectedIndex = SelectedIndex;
            WriteObject(tabs);
        }
    }
}
