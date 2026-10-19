#if !NETFRAMEWORK
using System;
using System.Management.Automation;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// Runs when the module is removed (PowerShell 7): the Write-Progress alias of Enable-ConsoleFormatting -Feature
    /// Progress points to a command of this module, so it goes too (and a user alias it replaced comes back). The Out-Default
    /// function falls back to PowerShell's own by itself.
    /// </summary>
    public sealed class ModuleCleanup : IModuleAssemblyCleanup {
        /// <inheritdoc />
        public void OnRemove(PSModuleInfo psModuleInfo) {
            if ((ConsoleFormatting.Features & ConsoleFormattingFeature.Progress) == 0) {
                return;
            }
            try {
                SessionProgress.EndPipeline();
                SessionProgress.Enabled = false;
                string restore = ConsoleFormatting.SavedWriteProgressAlias == null
                    ? string.Empty
                    : "; Microsoft.PowerShell.Utility\\Set-Alias -Name Write-Progress -Value '" + ConsoleFormatting.SavedWriteProgressAlias.Replace("'", "''") + "' -Scope Global -Force";
                ScriptBlock.Create("if ((Microsoft.PowerShell.Utility\\Get-Alias -Name Write-Progress -Scope Global -ErrorAction Ignore).Definition -eq '"
                    + ConsoleFormatting.ProgressCommand + "') { Microsoft.PowerShell.Management\\Remove-Item -Path 'Alias:\\Write-Progress' -Force" + restore + " }").Invoke();
            } catch (Exception ex) when (!(ex is PipelineStoppedException)) {
                // Removal goes on; the alias then reports that Write-ConsoleProgress is missing until it is removed by hand.
            }
            ConsoleFormatting.Features = ConsoleFormattingFeature.None;
            ConsoleFormatting.SavedWriteProgressAlias = null;
        }
    }
}
#endif
