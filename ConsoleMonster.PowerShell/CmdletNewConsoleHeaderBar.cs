using System;
using System.Management.Automation;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Creates a header with a title, breadcrumbs, detail and optional elapsed time.</para>
    /// <para type="description">Returns a reusable HeaderBar widget without opening a session. Status is a semantic outcome; Detail is the text beside it. HeaderRight supplies independent right-aligned content.</para>
    /// </summary>
    /// <example><para>Show a compact build header.</para><code>New-ConsoleHeaderBar 'Build' -Subtitle 'Release' -Status Pass -Detail 'Ready' | Show-Console</code></example>
    [Cmdlet(VerbsCommon.New, "ConsoleHeaderBar")]
    [OutputType(typeof(HeaderBar))]
    public sealed class CmdletNewConsoleHeaderBar : PSCmdlet {
        /// <summary><para type="description">The header's title.</para></summary>
        [Parameter(Mandatory = true, Position = 0)] public object Title { get; set; } = string.Empty;
        /// <summary><para type="description">Text qualifying the title.</para></summary>
        [Parameter] public object? Subtitle { get; set; }
        /// <summary><para type="description">Breadcrumbs shown before the title.</para></summary>
        [Parameter] public string[] Crumb { get; set; } = Array.Empty<string>();
        /// <summary><para type="description">Current descriptive text.</para></summary>
        [Parameter] public object? Detail { get; set; }
        /// <summary><para type="description">Semantic outcome displayed before the detail.</para></summary>
        [Parameter] [Binding.StatusKindTransformation] public StatusKind? Status { get; set; }
        /// <summary><para type="description">Independent text at the right of the header.</para></summary>
        [Parameter] public object? HeaderRight { get; set; }
        /// <summary><para type="description">Shows elapsed time in the header.</para></summary>
        [Parameter] public SwitchParameter ShowElapsed { get; set; }
        /// <summary><para type="description">An explicit elapsed duration; otherwise the rendering session supplies it.</para></summary>
        [Parameter] public double? ElapsedSeconds { get; set; }
        /// <inheritdoc />
        protected override void EndProcessing() {
            HeaderBar header = new HeaderBar(PSText.Markup(Title) ?? string.Empty) {
                Subtitle = PSText.Markup(Subtitle), Detail = PSText.Markup(Detail), Status = Status,
                HeaderRight = PSText.Markup(HeaderRight), ShowElapsed = ShowElapsed.IsPresent, Elapsed = ElapsedSeconds.HasValue ? TimeSpan.FromSeconds(ElapsedSeconds.Value) : (TimeSpan?)null
            };
            foreach (string crumb in Crumb) { header.Crumbs.Add(crumb); }
            WriteObject(header);
        }
    }
}
