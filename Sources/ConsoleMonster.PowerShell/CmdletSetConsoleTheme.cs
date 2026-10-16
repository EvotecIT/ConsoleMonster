using System.Management.Automation;
using ConsoleMonster.Styling;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Selects the theme used by every ConsoleMonster widget.</para>
    /// <para type="description">Themes map semantic tokens (pass, fail, warn, muted, accent, border...) to colours for every colour tier. Dark suits dark terminals (the default), Light suits light backgrounds, HighContrast uses saturated primaries.</para>
    /// <example>
    ///   <para>Use the light theme</para>
    ///   <code>Set-ConsoleTheme -Name Light</code>
    /// </example>
    /// </summary>
    [Cmdlet(VerbsCommon.Set, "ConsoleTheme")]
    public sealed class CmdletSetConsoleTheme : PSCmdlet {
        /// <summary><para type="description">The theme: Dark, Light or HighContrast.</para></summary>
        [Parameter(Position = 0, Mandatory = true)]
        [ValidateSet("Dark", "Light", "HighContrast")]
        public string Name { get; set; } = "Dark";

        /// <inheritdoc />
        protected override void ProcessRecord() {
            Terminal.Theme = Theme.FromName(Name);
        }
    }
}
