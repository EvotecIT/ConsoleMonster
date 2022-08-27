using System;
using System.Management.Automation;
using System.Management.Automation.Runspaces;

namespace ConsoleMonster {
    [Cmdlet(VerbsCommon.New, "SpectreImage")]
    [OutputType(typeof(void))]
    public class SpectreImage : PSCmdlet {
        [Parameter(
            Mandatory = true
        )]
        [AllowNull]
        [AllowEmptyString]
        public string FilePath { get; set; }

        // This method will be called once at the end of pipeline execution; if no input is received, this method is not called
        protected override void EndProcessing() {
            WriteObject("test");
        }
    }
}
