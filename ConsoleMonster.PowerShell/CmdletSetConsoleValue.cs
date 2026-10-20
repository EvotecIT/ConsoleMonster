using System.Management.Automation;
using ConsoleMonster.Live;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Sets a value in a running dashboard: a key/value pair, a bar or a stacked-bar segment.</para>
    /// <para type="description">Sends a "set" update to the named widget. For a key/value list the value is shown as text; for bar charts and stacked bars it must be numeric.</para>
    /// </summary>
    /// <example>
    ///   <para>Update a summary pair</para>
    ///   <code>Set-ConsoleValue -Name summary -Key 'Evaluated' -Value '154 / 332'</code>
    /// </example>
    [Cmdlet(VerbsCommon.Set, "ConsoleValue")]
    public sealed class CmdletSetConsoleValue : LiveCmdlet {
        /// <summary><para type="description">The key, bar label or segment.</para></summary>
        [Parameter(Position = 0, Mandatory = true)]
        public string Key { get; set; } = string.Empty;

        /// <summary><para type="description">The value.</para></summary>
        [Parameter(Position = 1, Mandatory = true)]
        public object Value { get; set; } = string.Empty;

        /// <summary><para type="description">The widget name (default keyvalue).</para></summary>
        [Parameter]
        public string Name { get; set; } = "keyvalue";

        /// <inheritdoc />
        protected override void ProcessRecord() {
            object raw = Value is PSObject ps ? ps.BaseObject : Value;
            double number = 0;
            bool numeric = raw is double || raw is int || raw is long || raw is float || raw is decimal;
            if (numeric) {
                number = LanguagePrimitives.ConvertTo<double>(raw);
            }
            Send(new LiveEvent(Name, "set", Key, Text.Markup.Escape(PSWidgets.ToText(raw)), value: number));
        }
    }
}
