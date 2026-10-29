using System;
using System.Management.Automation;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Shows the formatting features enabled in this runspace.</para>
    /// <para type="description">Returns the enabled feature names, whether compact errors are enabled, and whether built-in views are included. A refused installation is not reported as enabled. AddOptIns shows how to add Help, Prompt, Progress or GridView.</para>
    /// </summary>
    /// <example>
    /// <para>Inspect the current session's formatting</para>
    /// <code>Enable-ConsoleFormatting -Include Help; Get-ConsoleFormatting</code>
    /// </example>
    [Cmdlet(VerbsCommon.Get, "ConsoleFormatting")]
    [OutputType(typeof(PSObject))]
    public sealed class CmdletGetConsoleFormatting : PSCmdlet {
        /// <inheritdoc />
        protected override void EndProcessing() {
            FormattingSessionState state = FormattingSessionState.Current;
            var result = new PSObject();
            string names = ConsoleFormatting.FeatureList(state.Features);
            result.Properties.Add(new PSNoteProperty("Features", names.Length == 0 ? Array.Empty<string>() : names.Split(new[] { ", " }, StringSplitOptions.None)));
            result.Properties.Add(new PSNoteProperty("AddOptIns", "Enable-ConsoleFormatting -Include Help"));
            result.Properties.Add(new PSNoteProperty("OptionalFeatures", new[] { "Help", "Prompt", "Progress", "GridView" }));
            result.Properties.Add(new PSNoteProperty("CompactErrors", state.CompactErrors));
            result.Properties.Add(new PSNoteProperty("DetailedErrors", state.DetailedErrors));
            result.Properties.Add(new PSNoteProperty("BuiltIn", state.TakeOverBuiltIns));
            WriteObject(result);
        }
    }
}
