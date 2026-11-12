using System;
using System.Management.Automation;
using ConsoleMonster.Interactive;

namespace ConsoleMonster.PowerShell {
    /// <summary>Creates a breadcrumb for Show-ConsoleApp: a path such as Settings › Network › Proxy, its own or that of an outline's current entry.</summary>
    /// <remarks><para>The last segment is where you are. With -Outline it follows that outline's current entry, after any -Ancestors segments, and opening a heading in it opens that outline entry. When the row is too narrow the leading segments give way to an ellipsis. It is a focus stop that initial focus skips: Left/Right choose a segment and Enter or a click opens it; the final state holds the last segment opened.</para></remarks>
    /// <example>
    /// <para>Show where the reader is in an outline.</para>
    /// <code>
    /// $sections = New-ConsoleOutline -Key Sections -Items 'General', @{ Title = 'Proxy'; Level = 2 }, 'Logging' -SelectedIndex 1
    /// Show-ConsoleApp -Content {
    ///     New-ConsoleBreadcrumb -Key Where -Ancestors 'Settings' -Outline $sections
    ///     $sections
    /// }
    /// </code>
    /// </example>
    /// <example>
    /// <para>Compose an inventory breadcrumb</para>
    /// <code>New-ConsoleBreadcrumb -Key location -Segments 'Inventory', 'Servers', 'web01' | Show-Console
    /// </code>
    /// </example>
    [Cmdlet(VerbsCommon.New, "ConsoleBreadcrumb", DefaultParameterSetName = SegmentSet, HelpUri = "https://consolemonster.dev/powershell/commands/new-consolebreadcrumb/")]
    [OutputType(typeof(Breadcrumb))]
    public sealed class CmdletNewConsoleBreadcrumb : NoPipelineInputCmdlet {
        private const string SegmentSet = "Segment";
        private const string OutlineSet = "Outline";

        /// <summary><para type="description">Unique key in the application's state; the value is the last segment opened.</para></summary>
        [Parameter(Mandatory = true, Position = 0)]
        [ValidateNotNullOrEmpty]
        public string Key { get; set; } = string.Empty;

        /// <summary><para type="description">The segments, outermost first.</para></summary>
        [Parameter(Mandatory = true, Position = 1, ParameterSetName = SegmentSet)]
        [AllowEmptyCollection]
        public string[] Segments { get; set; } = Array.Empty<string>();

        /// <summary><para type="description">The outline whose current entry the breadcrumb shows (New-ConsoleOutline output).</para></summary>
        [Parameter(Mandatory = true, ParameterSetName = OutlineSet)]
        [ValidateNotNull]
        public object? Outline { get; set; }

        /// <summary><para type="description">Segments shown before the outline's headings, such as the screen or tab name.</para></summary>
        [Parameter(ParameterSetName = OutlineSet)]
        public string[] Ancestors { get; set; } = Array.Empty<string>();

        /// <summary><para type="description">Removes the breadcrumb from keyboard focus traversal and pointer input.</para></summary>
        [Parameter]
        [PSDefaultValue(Help = "$false")]
        public SwitchParameter Disabled { get; set; }

        /// <summary><para type="description">Keyboard focus order. Equal values preserve layout declaration order.</para></summary>
        [Parameter]
        [PSDefaultValue(Help = "0")]
        public int TabIndex { get; set; }

        /// <summary><para type="description">Runs once each time a segment is opened, on the owning input thread. Receives param($Value, $State) and injected $Value, $State and $Control. Output is discarded.</para></summary>
        [Parameter]
        public ScriptBlock? OnChange { get; set; }

        /// <inheritdoc />
        protected override void EndProcessing() {
            Outline? outline = Outline == null ? null : PSText.Unwrap(Outline) as Outline ?? throw new PSArgumentException("-Outline needs New-ConsoleOutline output.");
            BreadcrumbInputOptions options = new BreadcrumbInputOptions { Key = Key, Outline = outline, Disabled = Disabled.IsPresent, TabIndex = TabIndex };
            foreach (string segment in Segments) { options.Segments.Add(segment); }
            foreach (string segment in Ancestors) { options.Ancestors.Add(segment); }
            Breadcrumb breadcrumb = AppInput.Breadcrumb(options);
            PSAppCallbacks.Register(breadcrumb, OnChange);
            WriteObject(breadcrumb);
        }
    }
}