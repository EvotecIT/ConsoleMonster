using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Drafts a reusable console view from sample objects.</para>
    /// <para type="description">Returns a ready-to-paste Register-ConsoleView script with plain property names, named one-line presenter columns and a default count summary. Examines the first 50 objects and chooses up to six nonempty columns from DefaultDisplayPropertySet or declared properties, with a name or identity column first. Reuses default-table presenters, with status words, byte bars for a range greater than tenfold and relative dates for the last 30 days. Long registrations use a splat. Input objects are not changed. Empty input produces no draft.</para>
    /// <para type="description">Keep the script in your profile, or use -AsDataFile for a ModuleName.consoleviews.psd1 file beside the module manifest. Deserialized input drafts for the base type with a warning; Selected input keeps its selected type name with a warning. A draft describes the first object's type; sample one shape at a time.</para>
    /// </summary>
    /// <example>
    /// <para>Draft Name, Id, CPU, Handles and SI with a processes count. The Id presenter uses a named variable, plain columns stay names, and the registration uses a splat when needed. Paste the returned script into your profile; drop -Summary $summary to omit its count line.</para>
    /// <code>Get-Process | ConvertTo-ConsoleView</code>
    /// </example>
    /// <example>
    /// <para>Register the drafted columns and summary now, and return the compact script for later editing. The next listing uses this view when console formatting is enabled.</para>
    /// <code>
    /// Get-ChildItem -File | ConvertTo-ConsoleView -Register
    /// Get-ChildItem -File
    /// </code>
    /// </example>
    /// <example>
    /// <para>Save a data-only view and load it explicitly. Modules can ship a same-named file beside their manifest.</para>
    /// <code>
    /// Get-Process | ConvertTo-ConsoleView -AsDataFile | Set-Content Processes.consoleviews.psd1 -Encoding UTF8
    /// Register-ConsoleView -Path ./Processes.consoleviews.psd1
    /// </code>
    /// </example>
    /// <example>
    /// <para>Keep a process draft in your profile. Plain columns remain names; the Id presenter and count summary are named variables. Remove the Summary entry to leave the count line out.</para>
    /// <code>
    /// $id = @{ Name = 'Id'; Expression = 'Id'; Presenter = 'Identifier' }
    /// $summary = {
    ///     param($items)
    ///     @{ Value = @($items).Count; Label = 'processes' }
    /// }
    /// $view = @{
    ///     TypeName = 'System.Diagnostics.Process'
    ///     Property = @('Name', $id, 'CPU', 'Handles', 'SI')
    ///     Summary = $summary
    /// }
    /// Register-ConsoleView @view
    /// Get-Process
    /// </code>
    /// </example>
    [Cmdlet(VerbsData.ConvertTo, "ConsoleView")]
    [OutputType(typeof(string))]
    public sealed class CmdletConvertToConsoleView : PSCmdlet {
        /// <summary><para type="description">Objects of one shape to sample. Only the first 50 objects influence the draft.</para></summary>
        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true)]
        public PSObject[] InputObject { get; set; } = System.Array.Empty<PSObject>();

        /// <summary><para type="description">Returns a complete data-only .consoleviews.psd1 document with one Views entry. Expressions are property names; no script blocks are written. A name too long to fit a line under 100 characters produces a warning and no draft; omit this switch to use a script instead.</para></summary>
        [Parameter]
        public SwitchParameter AsDataFile { get; set; }

        /// <summary><para type="description">Also registers the drafted columns and default summary in this session. Use Enable-ConsoleFormatting to show registered views on the console. The summary counts items using the type name and adds lowercase status counts with pass, warning and failure roles.</para></summary>
        [Parameter]
        public SwitchParameter Register { get; set; }

        private readonly List<PSObject> _samples = new List<PSObject>();

        /// <inheritdoc />
        protected override void ProcessRecord() {
            foreach (PSObject item in InputObject) {
                if (item != null && _samples.Count < ConsoleViewDraft.SampleLimit) { _samples.Add(item); }
            }
        }

        /// <inheritdoc />
        protected override void EndProcessing() {
            if (_samples.Count == 0) { return; }
            string typeName = _samples[0].TypeNames[0];
            if (typeName.StartsWith("Deserialized.", System.StringComparison.OrdinalIgnoreCase)) {
                typeName = typeName.Substring("Deserialized.".Length);
                WriteWarning("Deserialized input: drafting for '" + typeName
                    + "'. This view will not apply to job or remote output.");
            }
            if (typeName.StartsWith("Selected.", System.StringComparison.OrdinalIgnoreCase)) {
                WriteWarning("Selected input: drafting for '" + typeName
                    + "'. This view applies to that selected shape, not the original type.");
            }
            ConsoleViewDraft draft = ConsoleViewDraft.Create(typeName, _samples);
            if (draft.Property.Length == 0) {
                WriteWarning("No nonempty properties were found in the first 50 samples; no view was drafted.");
                return;
            }
            string output = AsDataFile ? draft.DataFile() : draft.Script(ProtectedVariables());
            if (AsDataFile && System.Array.Exists(output.Split('\n'), line => line.Length >= 100)) {
                WriteWarning("A type or property name cannot fit in a data-file literal under 100 characters. "
                    + "No view was drafted or registered; omit -AsDataFile to use a script instead.");
                return;
            }
            if (Register) {
                ConsoleHostViews.Register(new ConsoleViewRegistration(typeName, property: draft.Property) { Summary = draft.Summary() });
            }
            WriteObject(output);
        }

        // Ask this PowerShell runtime for its protected and type-constrained variables instead of copying its list.
        private IEnumerable<string> ProtectedVariables() => InvokeCommand.InvokeScript("Microsoft.PowerShell.Utility\\Get-Variable")
            .Select(item => item.BaseObject).OfType<PSVariable>()
            .Where(variable => (variable.Options & (ScopedItemOptions.ReadOnly | ScopedItemOptions.Constant)) != 0
                || variable.Attributes.Count > 0)
            .Select(variable => variable.Name);
    }
}
