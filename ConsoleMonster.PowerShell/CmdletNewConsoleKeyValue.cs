using System.Collections;
using System.Management.Automation;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Creates an aligned key/value list.</para>
    /// <para type="description">Keys come from a hashtable (use [ordered] to keep order) or from an object's properties. Keys are muted and aligned; values wrap. Values are shown literally unless -Markup is used. In a live dashboard, update pairs with Set-ConsoleValue.</para>
    /// <example>
    ///   <para>A summary block</para>
    ///   <code>New-ConsoleKeyValue -Data ([ordered] @{ Domain = 'evotec.xyz'; Records = 14; DNSSEC = 'not signed' }) | Write-ConsoleWidget</code>
    /// </example>
    /// </summary>
    [Cmdlet(VerbsCommon.New, "ConsoleKeyValue")]
    [OutputType(typeof(KeyValueList))]
    public class CmdletNewConsoleKeyValue : PSCmdlet {
        /// <summary><para type="description">A hashtable or an object whose properties become the pairs.</para></summary>
        [Parameter(Position = 0)]
        public object? Data { get; set; }

        /// <summary><para type="description">Treat values as markup.</para></summary>
        [Parameter]
        public SwitchParameter Markup { get; set; }

        /// <summary><para type="description">The live name used by Set-ConsoleValue (default keyvalue).</para></summary>
        [Parameter]
        public string Name { get; set; } = "keyvalue";

        /// <inheritdoc />
        protected override void ProcessRecord() {
            KeyValueList list = new KeyValueList(Name);
            object? raw = Data is PSObject ps ? ps.BaseObject : Data;
            if (raw is IDictionary dictionary) {
                foreach (DictionaryEntry entry in dictionary) {
                    list.Add(PSWidgets.ToText(entry.Key), Value(entry.Value));
                }
            } else if (raw != null) {
                foreach (PSPropertyInfo property in PSObject.AsPSObject(raw).Properties) {
                    if (property.IsGettable) {
                        object? value;
                        try {
                            value = property.Value;
                        } catch (GetValueException) {
                            value = null;
                        }
                        list.Add(property.Name, Value(value));
                    }
                }
            }
            Emit(list);
        }

        /// <summary>Emits the list (Write-ConsoleKeyValue overrides this to write it).</summary>
        /// <param name="list">The list.</param>
        protected virtual void Emit(KeyValueList list) => WriteObject(list);

        private string Value(object? value) {
            string text = PSWidgets.ToText(value);
            return Markup.IsPresent ? text : Text.Markup.Escape(text);
        }
    }
}
