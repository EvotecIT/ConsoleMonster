using System;
using System.Management.Automation;
using System.Threading;
using ConsoleMonster.Interactive;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Shows a form or wizard and returns the answers.</para>
    /// <para type="description">The form opens on the alternate screen (or in the scrollback with -Inline). Fields are edited in place; choice fields open a filterable list; dependent options load with a spinner. The review page shows every answer and the equivalent command (copy it, save the answers as a named preset, or run). The answers come back as one object with a property per visible field (secrets as SecureString unless -AsPlainText); -PassThru returns the full result (Submitted, Values, CommandLine) instead. Cancelling returns nothing. -AppName stores presets, and with -RememberLastChoices the last answers, per user under %APPDATA%\ConsoleMonster\&lt;app&gt;. In remoting and the ISE the fields are asked one by one; without a user the defaults are used, or an error names the first field that needs a value.</para>
    /// </summary>
    /// <example>
    ///   <para>Run a wizard and use the answers</para>
    ///   <code>
    /// $answers = Read-ConsoleForm $form -AppName 'ShareTool' -RememberLastChoices
    /// if ($answers) { New-AppShare -Name $answers.Name -Path $answers.Path }</code>
    /// </example>
    [Cmdlet(VerbsCommunications.Read, "ConsoleForm")]
    [OutputType(typeof(PSObject), typeof(FormResult))]
    public sealed class CmdletReadConsoleForm : PSCmdlet {
        private CancellationTokenSource? _stop;

        /// <summary><para type="description">The form from New-ConsoleForm.</para></summary>
        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true)]
        public Form? Form { get; set; }

        /// <summary><para type="description">The app name presets and remembered choices are stored under.</para></summary>
        [Parameter]
        public string? AppName { get; set; }

        /// <summary><para type="description">Remembers the submitted answers and offers them next time (needs -AppName).</para></summary>
        [Parameter]

        public SwitchParameter RememberLastChoices { get; set; }

        /// <summary><para type="description">The folder that holds the app's storage folder (default: per user).</para></summary>
        [Parameter]
        public string? StorageRoot { get; set; }

        /// <summary><para type="description">Draws the form in the scrollback instead of the alternate screen.</para></summary>
        [Parameter]
        public SwitchParameter Inline { get; set; }

        /// <summary><para type="description">A preset to apply first: a built-in one (New-ConsoleForm -Preset) or one saved under -AppName.</para></summary>
        [Parameter]
        public string? Preset { get; set; }

        /// <summary><para type="description">Returns the full result (Submitted, Values, CommandLine) instead of the answers.</para></summary>
        [Parameter]
        public SwitchParameter PassThru { get; set; }

        /// <summary><para type="description">Returns secrets as plain strings.</para></summary>
        [Parameter]
        public SwitchParameter AsPlainText { get; set; }

        /// <summary><para type="description">Forces how to ask: Rich, Line, Accessible or None.</para></summary>
        [Parameter]
        public InteractionMode? Mode { get; set; }

        /// <inheritdoc />
        protected override void ProcessRecord() {
            if (Form == null) {
                return;
            }
            _stop = new CancellationTokenSource();
            FormResult result;
            try {
                result = Form.Show(new FormOptions {
                    Interactive = PSInteractive.Options(this, Mode),
                    AppName = AppName,
                    RememberLastChoices = RememberLastChoices.IsPresent,
                    StorageRoot = StorageRoot,
                    Inline = Inline.IsPresent,
                    Preset = Preset
                }, _stop.Token);
            } catch (Exception ex) when (ex is PromptCanceledException || ex is PromptUnavailableException || ex is Input.InputClosedException) {
                PSInteractive.Fail(this, ex, Form.Title);
                return;
            }
            if (PassThru.IsPresent) {
                WriteObject(result);
                return;
            }
            if (result.Submitted) {
                WriteObject(PSForms.ToObject(result.Values, Form, AsPlainText.IsPresent));
            }
        }

        /// <inheritdoc />
        protected override void StopProcessing() {
            try {
                _stop?.Cancel();
            } catch (ObjectDisposedException) {
            }
        }
    }
}
