using System;
using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Interactive;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Asks for a file or folder path, with completion.</para>
    /// <para type="description">Matching entries of the typed folder are listed under the box (folders first); Tab completes the common part or the focused entry, Up and Down move through them. -Kind File or Folder and -MustExist are checked on Enter. Returns the path as typed (environment variables and ~ are expanded only when checking).</para>
    /// <example>
    ///   <para>An existing CSV file</para>
    ///   <code>$file = Read-ConsolePath 'Import file' -Kind File -MustExist -Extension .csv</code>
    /// </example>
    /// </summary>
    [Cmdlet(VerbsCommunications.Read, "ConsolePath")]
    [OutputType(typeof(string))]
    public sealed class CmdletReadConsolePath : PromptCmdlet {
        /// <summary><para type="description">What is accepted: Any, File or Folder.</para></summary>
        [Parameter]
        public PathKind Kind { get; set; } = PathKind.Any;

        /// <summary><para type="description">Requires the path to exist.</para></summary>
        [Parameter]
        public SwitchParameter MustExist { get; set; }

        /// <summary><para type="description">The default path.</para></summary>
        [Parameter]
        public string? Default { get; set; }

        /// <summary><para type="description">File extensions offered as suggestions (.csv, .json).</para></summary>
        [Parameter]
        public string[]? Extension { get; set; }

        /// <summary><para type="description">The folder relative paths start from (default: the current location).</para></summary>
        [Parameter]
        public string? BasePath { get; set; }

        /// <inheritdoc />
        protected override void EndProcessing() {
            PathPrompt prompt = new PathPrompt(Title) {
                Kind = Kind,
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
            WriteObject(Ask(prompt));
        }
    }
}
