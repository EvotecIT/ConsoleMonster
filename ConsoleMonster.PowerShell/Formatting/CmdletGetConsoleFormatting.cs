using System;
using System.Management.Automation;

namespace ConsoleMonster.PowerShell {
    /// <summary>Shows the formatting features enabled in this runspace.</summary>
    /// <remarks><para>Returns the enabled feature names, whether compact errors are enabled, and whether built-in views are included. A refused installation is not reported as enabled. AddOptIns shows how to add Help, Prompt, Progress or GridView.</para></remarks>
    /// <example>
    /// <para>Inspect the current session's formatting</para>
    /// <code>Enable-ConsoleFormatting -Include Help; Get-ConsoleFormatting</code>
    /// </example>
    /// <example>
    /// <para>Inspect which formatting features are enabled</para>
    /// <code>Get-ConsoleFormatting | Select-Object Feature, Enabled | Show-ConsoleTable
    /// </code>
    /// </example>
    [Cmdlet(VerbsCommon.Get, "ConsoleFormatting", HelpUri = "https://consolemonster.dev/powershell/commands/get-consoleformatting/")]
    [OutputType(typeof(PSObject))]
    public sealed class CmdletGetConsoleFormatting : NoPipelineInputCmdlet {
        /// <inheritdoc />
        protected override void EndProcessing() {
            FormattingSessionState state = FormattingSessionState.Current;
            var result = new PSObject();
            string names = ConsoleFormatting.FeatureList(state.Features);
            result.Properties.Add(new PSNoteProperty("Features", names.Length == 0 ? Array.Empty<string>() : names.Split(new[] { ", " }, StringSplitOptions.None)));
            result.Properties.Add(new PSNoteProperty("AddOptIns", "Enable-ConsoleFormatting -Include Help"));
            result.Properties.Add(new PSNoteProperty("OptionalFeatures", new[] { "Help", "Prompt", "Progress", "GridView" }));
            result.Properties.Add(new PSNoteProperty("ErrorLayout", state.DetailedErrors ? "Detailed" : state.CompactErrors ? "Compact" : "Standard"));
            result.Properties.Add(new PSNoteProperty("BuiltIn", state.TakeOverBuiltIns));
            WriteObject(result);
        }
    }
}
