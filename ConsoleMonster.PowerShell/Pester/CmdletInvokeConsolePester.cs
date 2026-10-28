using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Management.Automation;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Export;
using ConsoleMonster.Live;
using ConsoleMonster.TestRuns;

namespace ConsoleMonster.PowerShell.Pester {
    /// <summary>
    /// <para type="synopsis">Runs Pester tests with a live ConsoleMonster dashboard and a summary table.</para>
    /// <para type="description">Takes the parameters of Invoke-Pester (Path, ExcludePath, TagFilter, ExcludeTagFilter, FullNameFilter, Container, Output, CI, PassThru) or a -Configuration (a PesterConfiguration or a hashtable), and runs Pester 5 or 6, imported when first needed. While the tests run, an inline area (or the full screen with -FullScreen) shows the run totals and the clock, the test files as lanes with passed, failed and skipped counts, the running test, and with -FullScreen the failures (expected against actual, the file and line as a link), the slowest tests and a histogram of durations. When the run ends a table of results by file, the failures and the totals are written to the scrollback, and -ExportHtml saves the final dashboard. In redirected output, jobs and CI logs the output is one line per failure and a summary line. Pester's result object is returned with -PassThru (or Run.PassThru), Run.Throw and Run.Exit (-CI) are applied after the summary, and Write-Host output from tests appears above the inline area. Pester has no public reporter API, so the live view uses Pester's internal plugin steps through a guarded adapter; when the internals are not what it expects (or with Run.Parallel or Output None) Pester runs unchanged with its own output and the summary table follows. Works on Windows PowerShell 5.1 and PowerShell 7.</para>
    /// </summary>
    /// <example>
    ///   <para>The tests of the current folder</para>
    ///   <code>Invoke-ConsolePester</code>
    /// </example>
    /// <example>
    ///   <para>A configuration, on the full screen, with the final dashboard saved</para>
    ///   <code>
    /// $c = New-PesterConfiguration
    /// $c.Run.Path = '.\Tests'
    /// $c.Filter.TagFilter = 'Unit'
    /// $r = Invoke-ConsolePester -Configuration $c -FullScreen -ExportHtml .\pester.html -PassThru</code>
    /// </example>
    [Cmdlet(VerbsLifecycle.Invoke, "ConsolePester", DefaultParameterSetName = "Simple")]
    [OutputType(typeof(PSObject))]
    public sealed class CmdletInvokeConsolePester : NoPipelineInputCmdlet {
        /// <summary><para type="description">Test files or folders; omitted uses Pester's current-folder default.</para></summary>
        [Parameter(Position = 0, ParameterSetName = "Simple")]
        public string[]? Path { get; set; }

        /// <summary><para type="description">Files or folders to leave out (Run.ExcludePath).</para></summary>
        [Parameter(ParameterSetName = "Simple")]
        public string[]? ExcludePath { get; set; }

        /// <summary><para type="description">Run only tests with these tags (Filter.Tag).</para></summary>
        [Parameter(ParameterSetName = "Simple")]

        public string[]? TagFilter { get; set; }

        /// <summary><para type="description">Leave out tests with these tags (Filter.ExcludeTag).</para></summary>
        [Parameter(ParameterSetName = "Simple")]

        public string[]? ExcludeTagFilter { get; set; }

        /// <summary><para type="description">Run only tests whose full name matches (Filter.FullName).</para></summary>
        [Parameter(ParameterSetName = "Simple")]
        public string[]? FullNameFilter { get; set; }

        /// <summary><para type="description">Containers from New-PesterContainer (Run.Container).</para></summary>
        [Parameter(ParameterSetName = "Simple")]
        public object[]? Container { get; set; }

        /// <summary><para type="description">Pester's output verbosity (Output.Verbosity). None runs quietly: no dashboard and no summary.</para></summary>
        [Parameter(ParameterSetName = "Simple")]
        [ValidateSet("Diagnostic", "Detailed", "Normal", "Minimal", "None")]
        public string? Output { get; set; }

        /// <summary><para type="description">CI mode as in Invoke-Pester: test results are written and the process exits with the failure count.</para></summary>
        [Parameter(ParameterSetName = "Simple")]
        public SwitchParameter CI { get; set; }

        /// <summary><para type="description">Returns Pester's result object.</para></summary>
        [Parameter(ParameterSetName = "Simple")]
        public SwitchParameter PassThru { get; set; }

        /// <summary><para type="description">A PesterConfiguration (New-PesterConfiguration) or a hashtable with the same sections.</para></summary>
        [Parameter(Mandatory = true, ParameterSetName = "Advanced")]
        public object Configuration { get; set; } = null!;

        /// <summary><para type="description">Shows the dashboard on the full screen (alternate screen) instead of inline.</para></summary>
        [Parameter]
        public SwitchParameter FullScreen { get; set; }

        /// <summary><para type="description">Saves the final dashboard as an HTML page.</para></summary>
        [Parameter]
        public string? ExportHtml { get; set; }

        /// <summary><para type="description">The dashboard title (default Pester).</para></summary>
        [Parameter]
        public string Title { get; set; } = "Pester";

        /// <summary><para type="description">Runs Pester unchanged with its own output, followed by the summary table (no adapter).</para></summary>
        [Parameter]
        public SwitchParameter NoLive { get; set; }

        /// <inheritdoc />
        protected override void EndProcessing() {
            var options = new ConsolePesterOptions {
                Configuration = ParameterSetName == "Advanced" ? Configuration : null,
                Title = Title, Live = !NoLive.IsPresent, FullScreen = FullScreen.IsPresent,
                ExportHtml = ExportHtml, CI = CI.IsPresent, PassThru = PassThru.IsPresent
            };
            if (MyInvocation.BoundParameters.ContainsKey(nameof(Path))) { options.Path = Path; }
            if (MyInvocation.BoundParameters.ContainsKey(nameof(ExcludePath))) { options.ExcludePath = ExcludePath; }
            if (MyInvocation.BoundParameters.ContainsKey(nameof(TagFilter))) { options.TagFilter = TagFilter; }
            if (MyInvocation.BoundParameters.ContainsKey(nameof(ExcludeTagFilter))) { options.ExcludeTagFilter = ExcludeTagFilter; }
            if (MyInvocation.BoundParameters.ContainsKey(nameof(FullNameFilter))) { options.FullNameFilter = FullNameFilter; }
            if (MyInvocation.BoundParameters.ContainsKey(nameof(Container))) { options.Container = Container; }
            if (MyInvocation.BoundParameters.ContainsKey(nameof(Output))) { options.Output = Output; }
            using (var runner = new ConsolePester(this, options)) { runner.Run(); }
        }
    }
}
