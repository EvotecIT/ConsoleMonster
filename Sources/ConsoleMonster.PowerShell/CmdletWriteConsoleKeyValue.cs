using System.Management.Automation;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Writes an aligned key/value list.</para>
    /// <para type="description">Same parameters as New-ConsoleKeyValue; the list is written immediately.</para>
    /// <example>
    ///   <para>Show an object's properties</para>
    ///   <code>Get-Item $PROFILE | Select-Object Name, Length, LastWriteTime | Write-ConsoleKeyValue</code>
    /// </example>
    /// </summary>
    [Cmdlet(VerbsCommunications.Write, "ConsoleKeyValue")]
    public sealed class CmdletWriteConsoleKeyValue : CmdletNewConsoleKeyValue {
        /// <summary><para type="description">An object from the pipeline whose properties become the pairs.</para></summary>
        [Parameter(ValueFromPipeline = true)]
        public PSObject? InputObject { get; set; }

        /// <inheritdoc />
        protected override void ProcessRecord() {
            if (InputObject != null && Data == null) {
                Data = InputObject;
            }
            base.ProcessRecord();
        }

        /// <inheritdoc />
        protected override void Emit(KeyValueList list) => Terminal.Write(list);
    }
}
