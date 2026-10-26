using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;
using ConsoleMonster.Interactive;

namespace ConsoleMonster.PowerShell {
    /// <summary><para type="synopsis">Creates a retained tab page for New-ConsoleTabs.</para></summary>
    /// <example><para>Create a page.</para><code>New-ConsoleTab -Title Services -Content { New-ConsoleTextInput -Key Filter }</code></example>
    [Cmdlet(VerbsCommon.New, "ConsoleTab")]
    [OutputType(typeof(LayoutRegion))]
    public sealed class CmdletNewConsoleTab : ContainerCmdlet {
        /// <summary><para type="description">Unique tab label.</para></summary>
        [Parameter(Mandatory = true)] public string Title { get; set; } = string.Empty;
        /// <summary><para type="description">Widgets or a script block that emits the page's retained inputs.</para></summary>
        [Parameter(Position = 0, ValueFromPipeline = true)] public override object? Content { get; set; }
        /// <inheritdoc />
        protected override IWidget Build(List<IWidget> children) => new TabOptions { Title = Title, Content = children.Count == 1 ? children[0] : new Rows(children.ToArray()) }.Build();
    }
}
