using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary><para type="synopsis">Creates retained tabs with keyboard and pointer navigation.</para></summary>
    /// <example><para>Compose two pages.</para><code>$pages | New-ConsoleTabs | Show-ConsoleApp</code></example>
    [Cmdlet(VerbsCommon.New, "ConsoleTabs")]
    [OutputType(typeof(Tabs))]
    public sealed class CmdletNewConsoleTabs : PSCmdlet {
        private readonly List<LayoutRegion> _pages = new List<LayoutRegion>();
        /// <summary><para type="description">Pages returned by New-ConsoleTab, in display order.</para></summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true)] public LayoutRegion[] Tab { get; set; } = System.Array.Empty<LayoutRegion>();
        /// <summary><para type="description">Initial zero-based active page.</para></summary>
        [Parameter, ValidateRange(0, int.MaxValue)] public int SelectedIndex { get; set; }
        /// <inheritdoc />
        protected override void ProcessRecord() => _pages.AddRange(Tab);
        /// <inheritdoc />
        protected override void EndProcessing() {
            var tabs = new Tabs();
            foreach (LayoutRegion page in _pages) { tabs.Add(page.Key, page.Content); }
            tabs.SelectedIndex = SelectedIndex;
            WriteObject(tabs);
        }
    }
}
