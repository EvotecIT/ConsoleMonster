using System;
using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Layout;
using ConsoleMonster.Views;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Shows objects of registered types through ConsoleMonster and passes everything else to PowerShell's Out-Default.</para>
    /// <para type="description">This is what Enable-ConsoleFormatting puts behind Out-Default. Consecutive objects of registered types (Register-ConsoleView; after Enable-ConsoleFormatting also files and folders, processes, services, commands, modules, event log records, IP addresses, volumes, versions and dictionaries) are collected and shown as one ConsoleMonster view; any other object, format data from Format-*, errors and strings go to PowerShell's Out-Default unchanged. It only takes over when the output is displayed on a real local console: redirected output, jobs, remoting and other hosts get PowerShell's default output (-Force renders anyway). The objects themselves are never changed.</para>
    /// </summary>
    /// <example>
    /// <para>Show processes explicitly</para>
    /// <code>Get-Process | Select-Object -First 5 | Show-Console</code>
    /// </example>
    internal sealed class CmdletOutConsoleDefault : PSCmdlet {
        private ConsoleDefaultOutput? _output;

        /// <summary><para type="description">The objects to show.</para></summary>
        [Parameter(ValueFromPipeline = true)]
        public PSObject? InputObject { get; set; }

        /// <summary><para type="description">Passed to Out-Default (the host transcribes the output).</para></summary>
        [Parameter]
        public SwitchParameter Transcript { get; set; }

        /// <summary><para type="description">Renders registered types even when the output is redirected or the host is not a console (as plain lines there).</para></summary>
        [Parameter]
        public SwitchParameter Force { get; set; }

        /// <summary><para type="description">Set by the Out-Default function of Enable-ConsoleFormatting: only the enabled features apply (registered types, the error view, the progress area).</para></summary>
        [Parameter(DontShow = true)]
        public SwitchParameter Shim { get; set; }

        /// <summary><para type="description">Set by the Out-Default function when PowerShell added it to a command typed at the prompt (not an explicit Out-Default in a script): the progress area ends with that command.</para></summary>
        [Parameter(DontShow = true)]
        public SwitchParameter TopLevel { get; set; }

        /// <summary>The implicit Out-Default shim; used internally to check native help provenance during record delivery.</summary>
        [Parameter(DontShow = true)]
        public PSCmdlet? HelpSink { get; set; }

        /// <inheritdoc />
        protected override void BeginProcessing() => _output = new ConsoleDefaultOutput(this, new ConsoleDefaultOutputOptions {
            Force = Force.IsPresent, Transcript = Transcript.IsPresent, Shim = Shim.IsPresent,
            TopLevel = TopLevel.IsPresent, HelpSink = HelpSink
        });
        /// <inheritdoc />
        protected override void ProcessRecord() => _output!.Write(InputObject);
        /// <inheritdoc />
        protected override void EndProcessing() {
            try { _output?.Complete(); }
            finally { _output?.Dispose(); }
        }
        /// <inheritdoc />
        protected override void StopProcessing() => _output?.Dispose();
    }
}
