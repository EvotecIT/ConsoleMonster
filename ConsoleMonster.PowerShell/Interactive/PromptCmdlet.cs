using System;
using System.Management.Automation;
using System.Threading;
using ConsoleMonster.Interactive;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// The parameters every Read-Console* prompt shares: the question, help text, the parameter that supplies the value
    /// non-interactively, and an interaction mode override. Esc and Ctrl+C end the prompt with a terminating
    /// PromptCanceled error; without a user (CI, redirected input, -NonInteractive) the default is returned, or a
    /// PromptNeedsValue error names the parameter to pass.
    /// </summary>
    public abstract class PromptCmdlet : PSCmdlet {
        private CancellationTokenSource? _stop;

        /// <summary><para type="description">The question.</para></summary>
        [Parameter(Position = 0), Alias("Prompt")]

        public string Title { get; set; } = "Enter a value";

        /// <summary><para type="description">Help text under the question.</para></summary>
        [Parameter]
        public string? Description { get; set; }

        /// <summary><para type="description">The parameter of your script that supplies this value without a prompt (named in the non-interactive error, for example -Domain).</para></summary>
        [Parameter]

        public string? ParameterName { get; set; }

        /// <summary><para type="description">Forces how to ask: Rich (in-place prompt), Line (question and typed answer), Accessible (numbered, no redraws) or None (use the default or fail).</para></summary>
        [Parameter]
        public InteractionMode? Mode { get; set; }

        /// <summary><para type="description">Prints the initialized rich prompt without reading input or returning an answer. Useful for documentation and unattended previews.</para></summary>
        [Parameter]
        public SwitchParameter StaticSnapshot { get; set; }

        /// <summary>Options for the current host.</summary>
        protected InteractiveOptions Options() => PSInteractive.Options(this, Mode);

        /// <summary>Cancelled when the pipeline stops.</summary>
        protected CancellationToken Token => (_stop ??= new CancellationTokenSource()).Token;

        /// <summary>Applies the shared settings to a prompt.</summary>
        protected void Configure<T>(Prompt<T> prompt) {
            prompt.Description = Description;
            prompt.ParameterName = ParameterName;
        }

        /// <summary>Prints a requested snapshot through the ordinary PowerShell output route, without asking for input.</summary>
        /// <param name="prompt">The fully configured prompt.</param>
        /// <returns>True when the snapshot was printed and the command should return without an answer.</returns>
        protected bool TrySnapshot<T>(Prompt<T> prompt) {
            if (!StaticSnapshot.IsPresent) { return false; }
            PSFormatting.Write(this, prompt.Snapshot(Options()), 0);
            return true;
        }

        /// <summary>Shows a prompt, turning cancellation and missing values into PowerShell errors.</summary>
        protected T Ask<T>(Prompt<T> prompt) {
            try {
                InteractiveOptions options = Options();
                if (options.ResolveMode() == InteractionMode.None && prompt.HasDefault) {
                    WriteVerbose("'" + Title + "': no interactive user, using the default.");
                }
                return prompt.Show(options, Token);
            } catch (Exception ex) when (ex is PromptCanceledException || ex is PromptUnavailableException || ex is Input.InputClosedException) {
                PSInteractive.Fail(this, ex, Title);
                throw;
            }
        }

        /// <inheritdoc />
        protected override void StopProcessing() {
            try {
                _stop?.Cancel();
            } catch (ObjectDisposedException) {
            }
            base.StopProcessing();
        }
    }
}
