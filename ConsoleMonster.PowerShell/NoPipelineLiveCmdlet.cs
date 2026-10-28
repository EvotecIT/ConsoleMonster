using System.Management.Automation;

namespace ConsoleMonster.PowerShell {
    /// <summary>A live operation that rejects pipeline input before writing a dashboard event.</summary>
    public abstract class NoPipelineLiveCmdlet : LiveCmdlet {
        /// <summary>Unsupported pipeline input.</summary>
        [Parameter(ValueFromPipeline = true, DontShow = true)]
        public PSObject? PipelineInput { get; set; }

        /// <inheritdoc />
        protected sealed override void ProcessRecord() {
            if (PipelineInput != null) { NoPipelineInputCmdlet.Reject(this, PipelineInput); return; }
            ProcessWithoutPipelineInput();
        }

        /// <summary>Runs the live operation when no pipeline value was supplied.</summary>
        protected abstract void ProcessWithoutPipelineInput();
    }
}
