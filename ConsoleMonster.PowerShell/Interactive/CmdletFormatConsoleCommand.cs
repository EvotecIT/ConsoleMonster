using System.Collections;
using System.Management.Automation;
using ConsoleMonster.Interactive;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Builds a copyable command line from named values without executing it.</para>
    /// <para type="description">Formats strings, numbers, switches and lists through the same quoting rules as forms. Use an ordered dictionary to retain argument order. Null, empty strings and false switches are omitted. The result is a plain string, suitable for a review's equivalent command.</para>
    /// </summary>
    /// <example>
    /// <para>Build an equivalent command from selected servers</para>
    /// <code>Format-ConsoleCommand -Command Invoke-FleetCheck -Arguments ([ordered]@{ ComputerName = @('WEB01', 'SQL01'); ThrottleLimit = 3; WhatIf = $true })</code>
    /// </example>
    [Cmdlet(VerbsCommon.Format, "ConsoleCommand")]
    [OutputType(typeof(string))]
    public sealed class CmdletFormatConsoleCommand : PSCmdlet {
        /// <summary><para type="description">The command text, which is never executed.</para></summary>
        [Parameter(Mandatory = true)]
        public string Command { get; set; } = string.Empty;

        /// <summary><para type="description">Parameter names and values. Use an ordered dictionary when order matters.</para></summary>
        [Parameter(Mandatory = true)]
        public IDictionary Arguments { get; set; } = new Hashtable();

        /// <summary><para type="description">PowerShell, Posix or Windows argument quoting (default PowerShell).</para></summary>
        [Parameter]
        public CommandStyle Style { get; set; } = CommandStyle.PowerShell;

        /// <inheritdoc />
        protected override void ProcessRecord() => WriteObject(CommandLine.Build(Command, PSForms.ToFieldValues(Arguments, null), Style));
    }
}
