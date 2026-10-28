using System;
using System.Management.Automation;

namespace ConsoleMonster.PowerShell {
    /// <summary>Reports unsupported pipeline input once before a non-input command performs its work.</summary>
    public abstract class NoPipelineInputCmdlet : PSCmdlet {
        /// <summary>Receives unsupported pipeline input so it can be rejected without one binding error per object.</summary>
        [Parameter(ValueFromPipeline = true, DontShow = true)]
        public PSObject? PipelineInput { get; set; }

        /// <inheritdoc />
        protected sealed override void ProcessRecord() {
            if (PipelineInput != null) {
                string name = MyInvocation.MyCommand.Name;
                ThrowTerminatingError(new ErrorRecord(new ArgumentException(name + " does not accept pipeline objects. To display them, pipe them to Show-ConsoleTable."), "UnexpectedPipelineInput", ErrorCategory.InvalidArgument, PipelineInput));
                return;
            }
            ProcessWithoutPipelineInput();
        }

        /// <summary>Performs the command's normal per-record operation.</summary>
        protected virtual void ProcessWithoutPipelineInput() { }
    }
}
