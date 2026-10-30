using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using ConsoleMonster.Interactive;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Creates retained tabs with keyboard and pointer navigation.</para>
    /// <para type="description">Content emits records with Title and Content, or an ordered dictionary mapping page titles to widgets or script blocks. A dictionary containing both Title and Content is a page record, including when Content is null. Only the selected page receives input; switching pages retains its values and focus.</para>
    /// </summary>
    /// <example><para>Compose two retained pages.</para><code>Show-ConsoleApp -Mode Snapshot -Content {
    ///     New-ConsoleTabs -Content ([ordered]@{
    ///         Search = { New-ConsoleTextInput -Key Query }
    ///         About = { New-ConsoleText 'Inventory browser' }
    ///     })
    /// }</code></example>
    [Cmdlet(VerbsCommon.New, "ConsoleTabs")]
    [OutputType(typeof(Tabs))]
    public sealed class CmdletNewConsoleTabs : PSCmdlet {
        private readonly List<LayoutRegion> _pages = new List<LayoutRegion>();
        /// <summary><para type="description">Page records or an ordered dictionary of page titles and content.</para></summary>
        [Parameter(Position = 0)] public object? Content { get; set; }
        /// <summary><para type="description">Page records from the pipeline.</para></summary>
        [Parameter(ValueFromPipeline = true)] public PSObject? InputObject { get; set; }
        /// <summary><para type="description">Initial zero-based active page.</para></summary>
        [Parameter, ValidateRange(0, int.MaxValue)] public int SelectedIndex { get; set; }
        /// <inheritdoc />
        protected override void BeginProcessing() {
            foreach (object item in PSItems.Items(Content)) { Add(item); }
        }
        /// <inheritdoc />
        protected override void ProcessRecord() {
            if (InputObject == null) { return; }
            foreach (object item in PSItems.Items(InputObject)) { Add(item); }
        }
        /// <inheritdoc />
        protected override void EndProcessing() {
            if (_pages.Count == 0) { throw new PSArgumentException("New-ConsoleTabs needs at least one page with Title and Content."); }
            var tabs = new Tabs();
            foreach (LayoutRegion page in _pages) { tabs.Add(page.Key, page.Content); }
            tabs.SelectedIndex = SelectedIndex;
            WriteObject(tabs);
        }

        private void Add(object item) {
            if (PSText.Unwrap(item) is LayoutRegion page) { _pages.Add(page); return; }
            if (PSText.Unwrap(item) is IDictionary dictionary) {
                string[] keys = dictionary.Keys.Cast<object>().Select(PSWidgets.ToText).ToArray();
                if (!keys.Contains("Title", StringComparer.OrdinalIgnoreCase) || !keys.Contains("Content", StringComparer.OrdinalIgnoreCase)) {
                    foreach (DictionaryEntry entry in dictionary) { AddPage(PSWidgets.ToText(entry.Key), entry.Value); }
                    return;
                }
            }
            PSItems.RecordFields(item, "Title Content");
            string title = PSWidgets.ToText(PSItems.Field(item, "Title"));
            if (string.IsNullOrWhiteSpace(title)) { throw new PSArgumentException("A tab record needs Title and Content. To display objects, use Show-ConsoleTable."); }
            AddPage(title, PSItems.Field(item, "Content"));
        }

        private void AddPage(string title, object? content) {
            var children = PSWidgets.Collect(PSItems.Items(content).Select(PSObject.AsPSObject));
            _pages.Add(new TabOptions { Title = title, Content = children.Count == 1 ? children[0] : new ConsoleMonster.Layout.Rows(children.ToArray()) }.Build());
        }
    }
}
