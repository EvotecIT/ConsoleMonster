using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using ConsoleMonster.Interactive;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>Creates retained tabs with keyboard and pointer navigation.</summary>
    /// <remarks><para>Content emits records with Title and Content, or an ordered dictionary mapping page titles to widgets or script blocks. A dictionary containing both Title and Content is a page record, including when Content is null. Only the selected page receives input; switching pages retains its values and focus. With -Key, the app state holds the active page's title under that key, and -OnChange runs when the page changes.</para></remarks>
    /// <example><para>Compose two retained pages.</para><code>Show-ConsoleApp -Mode Snapshot -Content {
    ///     New-ConsoleTabs -Key Page -Content ([ordered]@{
    ///         Search = { New-ConsoleTextInput -Key Query }
    ///         About = { New-ConsoleText 'Inventory browser' }
    ///     })
    /// }</code></example>
    /// <example>
    /// <para>Compose two retained inventory pages</para>
    /// <code>New-ConsoleTabs -Key inventory -Content ([ordered]@{
    ///     Servers = { [pscustomobject]@{ Name = 'web01'; State = 'Online' } | Show-Console -View List -AsWidget }
    ///     Summary = { New-ConsoleText 'One server online' -Role Pass }
    /// }) | Show-Console
    /// </code>
    /// </example>
    [Cmdlet(VerbsCommon.New, "ConsoleTabs", HelpUri = "https://consolemonster.dev/powershell/commands/new-consoletabs/")]
    [OutputType(typeof(Tabs))]
    public sealed class CmdletNewConsoleTabs : PSCmdlet {
        private readonly List<LayoutRegion> _pages = new List<LayoutRegion>();
        /// <summary><para type="description">Page records or an ordered dictionary of page titles and content.</para></summary>
        [Parameter(Position = 0)] public object? Content { get; set; }
        /// <summary><para type="description">Page records from the pipeline.</para></summary>
        [Parameter(ValueFromPipeline = true)] public PSObject? InputObject { get; set; }
        /// <summary><para type="description">Initial zero-based active page.</para></summary>
        [Parameter, ValidateRange(0, int.MaxValue)] [PSDefaultValue(Help = "0")]
        public int SelectedIndex { get; set; }
        /// <summary><para type="description">Unique key in the application's state; the value is the active page's title.</para></summary>
        [Parameter, ValidateNotNullOrEmpty] public string? Key { get; set; }
        /// <summary><para type="description">Runs once each time the active page changes, on the owning input thread. Receives param($Value, $State) and injected $Value, $State and $Control. Output is discarded.</para></summary>
        [Parameter] public ScriptBlock? OnChange { get; set; }
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
            var tabs = new Tabs { Key = Key };
            foreach (LayoutRegion page in _pages) { tabs.Add(page.Key, page.Content); }
            if (SelectedIndex >= _pages.Count) { throw new PSArgumentOutOfRangeException(nameof(SelectedIndex), SelectedIndex, "SelectedIndex is beyond the last page."); }
            tabs.SelectedIndex = SelectedIndex;
            PSAppCallbacks.Register(((IOwnedInput)tabs).OwnedInput, OnChange);
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
