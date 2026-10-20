using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Replaces the content of a region (New-ConsoleRegion) in the running dashboard.</para>
    /// <para type="description">The new widget is shown from the next frame on; the surrounding frame stays, and live widgets inside the new content (lanes, logs, live tables) can be updated by name right away. Use it from the dashboard script, like the Update-Console* commands.</para>
    /// </summary>
    /// <example>
    ///   <para>Show a table when the query finishes</para>
    ///   <code>Set-ConsoleRegion -Name main { $rows | Format-ConsoleTable -AsWidget }</code>
    /// </example>
    [Cmdlet(VerbsCommon.Set, "ConsoleRegion", DefaultParameterSetName = "Content")]
    public sealed class CmdletSetConsoleRegion : LiveCmdlet {
        /// <summary><para type="description">The region name.</para></summary>
        [Parameter(Position = 0, Mandatory = true)]
        public string Name { get; set; } = string.Empty;

        /// <summary><para type="description">A script block whose widgets become the content.</para></summary>
        [Parameter(Position = 1, ParameterSetName = "Content")]
        public ScriptBlock? Content { get; set; }

        /// <summary><para type="description">The widget that becomes the content.</para></summary>
        [Parameter(ValueFromPipeline = true, ParameterSetName = "Content")]
        public IWidget? Widget { get; set; }

        /// <summary><para type="description">Empties the region.</para></summary>
        [Parameter(Mandatory = true, ParameterSetName = "Clear")]
        public SwitchParameter Clear { get; set; }

        /// <inheritdoc />
        protected override void ProcessRecord() {
            if (Clear.IsPresent) {
                Send(new LiveEvent(Name, "clear"));
                return;
            }
            List<IWidget> children = PSWidgets.Collect(Content);
            if (Widget != null) {
                children.Add(Widget);
            }
            if (children.Count == 0) {
                Send(new LiveEvent(Name, "clear"));
                return;
            }
            IWidget content = children.Count == 1 ? children[0] : new Rows(children.ToArray());
            Send(new LiveEvent(Name, "set") { Payload = content });
        }
    }
}
