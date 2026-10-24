using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Defines a live console dashboard: a header with title and clock, the body and a footer.</para>
    /// <para type="description">The scriptblock emits the body widgets top to bottom. Pass the result to Invoke-ConsoleDashboard to run it live, or to Show-Console to print it once.</para>
    /// </summary>
    /// <example>
    ///   <para>A dashboard with lanes and a log that fills the rest of the screen</para>
    ///   <code>$d = New-ConsoleDashboard -Title 'Deploy' -Content { New-ConsolePanel -Title 'Servers' -Content { New-ConsoleLanes -WidgetId servers }; New-ConsolePanel -Title 'Log' -Fill -Content { New-ConsoleLogRegion } }</code>
    /// </example>
    [Cmdlet(VerbsCommon.New, "ConsoleDashboard")]
    [OutputType(typeof(Dashboard))]
    public sealed class CmdletNewConsoleDashboard : PSCmdlet {
        /// <summary><para type="description">Scriptblock that emits the body widgets.</para></summary>
        [Parameter(Position = 1, Mandatory = true)]
        public ScriptBlock Content { get; set; } = null!;

        /// <summary><para type="description">The title shown in the header bar (markup allowed).</para></summary>
        [Parameter(Mandatory = true)]
        public object Title { get; set; } = string.Empty;

        /// <summary><para type="description">A subtitle shown after the title.</para></summary>
        [Parameter]
        public object? Subtitle { get; set; }

        /// <summary><para type="description">Footer text; defaults to a Ctrl+C hint.</para></summary>
        [Parameter]
        public object? Footer { get; set; }

        /// <summary><para type="description">Live name of the dashboard (for Send-ConsoleEvent title/status updates).</para></summary>
        [Parameter(Position = 0)]
        public string WidgetId { get; set; } = "dashboard";

        /// <summary><para type="description">Blank rows between the body widgets.</para></summary>
        [Parameter]
        public int Gap { get; set; }

        /// <summary><para type="description">Hides the default elapsed clock in the dashboard header.</para></summary>
        [Parameter] public SwitchParameter NoElapsed { get; set; }

        /// <inheritdoc />
        protected override void ProcessRecord() {
            List<IWidget> children = PSWidgets.Collect(Content);
            Dashboard dashboard = new Dashboard(PSText.Markup(Title) ?? string.Empty, new Rows(children.ToArray()) { Gap = Gap }, WidgetId) { Subtitle = PSText.Markup(Subtitle), ShowElapsed = !NoElapsed.IsPresent };
            if (MyInvocation.BoundParameters.ContainsKey(nameof(Footer))) {
                dashboard.Footer = PSText.Markup(Footer);
            }
            WriteObject(dashboard);
        }
    }
}
