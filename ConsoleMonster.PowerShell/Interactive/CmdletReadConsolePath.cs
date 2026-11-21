using System;
using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Interactive;

namespace ConsoleMonster.PowerShell {
    /// <summary>Asks for a file or folder path, with completion.</summary>
    /// <remarks><para>Matching entries of the typed folder are listed under the box (folders first); Tab completes the common part or the focused entry, Up and Down move through them. -Kind File or Folder and -MustExist are checked on Enter. Returns the path as typed (environment variables and ~ are expanded only when checking).</para></remarks>
    /// <example>
    ///   <para>An existing CSV file</para>
    ///   <code>$pathOptions = @{
    ///     MustExist = $true
    ///     Title = 'Import file'
    ///     Kind = 'File'
    ///     Extension = @('.csv')
    /// }
    /// $file = Read-ConsolePath @pathOptions</code>
    /// </example>
    /// <example>
    /// <para>Select an existing inventory directory</para>
    /// <code>$pathOptions = @{
    ///     MustExist = $true
    ///     Title = 'Inventory folder'
    ///     Default = '.'
    ///     Kind = 'Directory'
    /// }
    /// $path = Read-ConsolePath @pathOptions
    /// $textOptions = @{
    ///     Text = @("Inventory folder: $path")
    /// }
    /// Write-ConsoleText @textOptions</code>
    /// </example>
    /// <example>
    /// <para>Read the gallery script to see Read-ConsolePath in a complete example. Each lookup returns the script's source. Use the same slug with Show-ConsoleDemo to run it.</para>
    /// <code>Get-ConsoleDemo -Slug 'prompt-tour' | Select-Object -ExpandProperty Source</code>
    /// </example>
    [Cmdlet(VerbsCommunications.Read, "ConsolePath", HelpUri = "https://consolemonster.dev/powershell/commands/read-consolepath/")]
    [OutputType(typeof(string))]
    public sealed class CmdletReadConsolePath : NonSelectionPromptCmdlet {
        /// <summary><para type="description">What is accepted: Any, File or Folder.</para></summary>
        [Parameter]
        [PSDefaultValue(Help = "Any")]
        public PathKind Kind { get; set; } = PathKind.Any;

        /// <summary><para type="description">Requires the path to exist.</para></summary>
        [Parameter]
        [PSDefaultValue(Help = "$false")]
        public SwitchParameter MustExist { get; set; }

        /// <summary><para type="description">The path initially shown in the editor; pressing Enter accepts it.</para></summary>
        [Parameter]
        public string? Default { get; set; }

        /// <summary><para type="description">File extensions offered as suggestions (.csv, .json).</para></summary>
        [Parameter]
        public string[]? Extension { get; set; }

        /// <summary><para type="description">The folder relative paths start from (default: the current location).</para></summary>
        [Parameter]
        public string? BasePath { get; set; }

        /// <summary><para type="description">Includes hidden files and folders in completion suggestions.</para></summary>
        [Parameter] [PSDefaultValue(Help = "$false")]
        public SwitchParameter ShowHidden { get; set; }
        /// <summary><para type="description">Maximum visible completion suggestions; default six.</para></summary>
        [Parameter, ValidateRange(1, int.MaxValue)] [PSDefaultValue(Help = "6")]
        public int Suggestions { get; set; } = 6;

        /// <inheritdoc />
        protected override void EndProcessing() {
            PathPrompt prompt = new PathPrompt(Title) {
                Kind = Kind, ShowHidden = ShowHidden.IsPresent, Suggestions = Suggestions,
                MustExist = MustExist.IsPresent,
                BasePath = BasePath ?? SessionState.Path.CurrentFileSystemLocation.ProviderPath
            };
            Configure(prompt);
            if (Default != null) {
                prompt.Default = Default;
            }
            if (Extension != null) {
                foreach (string e in Extension) {
                    prompt.Extensions.Add(e.StartsWith(".", StringComparison.Ordinal) ? e : "." + e);
                }
            }
            if (TrySnapshot(prompt)) { return; }
            WriteObject(Ask(prompt));
        }
    }
}
