using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>Creates a clipped vertical viewport for a small widget tree inside an app.</summary>
    /// <remarks><para>Mouse wheel scrolls the viewport. Tab navigation reveals focused inputs. Lists, grids and logs keep their own row windows for large data.</para></remarks>
    /// <example>
    /// <para>Build a scrollable detail pane</para>
    /// <code>New-ConsoleScrollView -Content {
    ///     1..20 | ForEach-Object { New-ConsoleText "Detail line $_" }
    /// } -Height 12</code>
    /// </example>
    /// <example>
    /// <para>Compose an inventory scroll view</para>
    /// <code>New-ConsoleScrollView -Height 4 -Content {
    ///     foreach ($server in 'web01', 'web02', 'db01', 'db02', 'dc01') { New-ConsoleText $server }
    /// } | Show-Console
    /// </code>
    /// </example>
    [Cmdlet(VerbsCommon.New, "ConsoleScrollView", HelpUri = "https://consolemonster.dev/powershell/commands/new-consolescrollview/")]
    [OutputType(typeof(IWidget))]
    public sealed class CmdletNewConsoleScrollView : ContainerCmdlet {
        /// <summary><para type="description">Widgets, text, objects, or a script block that emits them.</para></summary>
        [Parameter(Position = 0, ValueFromPipeline = true)]
        [AllowNull, AllowEmptyCollection]
        public override object? Content { get; set; }
        /// <summary><para type="description">First visible content row.</para></summary>
        [Parameter, ValidateRange(0, int.MaxValue)] [PSDefaultValue(Help = "0")]
        public int ScrollOffset { get; set; }
        /// <summary><para type="description">Maximum composed rows for ordinary content; use virtualized lists or grids for larger data.</para></summary>
        [Parameter, ValidateRange(1, 16384)] [PSDefaultValue(Help = "16384")]
        public int MaxContentHeight { get; set; } = 16384;
        /// <summary><para type="description">Share of the parent layout.</para></summary>
        [Parameter] [PSDefaultValue(Help = "0")]
        public double Weight { get; set; }
        /// <summary><para type="description">Fixed viewport height in cells.</para></summary>
        [Parameter] [PSDefaultValue(Help = "0")]
        public int Height { get; set; }
        /// <summary><para type="description">Use the available height.</para></summary>
        [Parameter] [PSDefaultValue(Help = "$false")]
        public SwitchParameter Fill { get; set; }
        /// <inheritdoc />
        protected override IWidget Build(List<IWidget> children) {
            ScrollView scroll = new ScrollView(children.Count == 1 ? children[0] : new Rows(children.ToArray())) { MaxContentHeight = MaxContentHeight };
            if (MyInvocation.BoundParameters.ContainsKey(nameof(ScrollOffset))) { scroll.ScrollOffset = ScrollOffset; }
            return PSWidgets.WithSize(scroll, Weight, Height, Fill.IsPresent);
        }
    }
}
