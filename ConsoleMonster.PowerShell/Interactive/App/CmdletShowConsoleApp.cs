using System;
using System.Collections;
using System.Management.Automation;
using System.Threading;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Runs native input controls and dependent displays in a console application.</para>
    /// <para type="description">Builds the layout once, then runs input synchronously on the caller's PowerShell thread. Callbacks are queued by controls and drained before input reads; dependent builders run on that same thread. Escape exits. Cancellation restores the terminal through the core runner. Use -StaticSnapshot to capture or print initialized values without interactive input. CONSOLEMONSTER_SNAPSHOT=1 also selects an initialized snapshot when no input source is supplied; CONSOLEMONSTER_INPUT supplies scripted keys centrally. Nothing is sent to the pipeline unless -PassThru requests one ordinary hashtable.</para>
    /// </summary>
    /// <example>
    /// <para>Filter application data without leaving PowerShell.</para>
    /// <code>Show-ConsoleApp -Content {
    ///     New-ConsoleTextInput -Key Query -OnChange { param($Value, $State) $State.Edits = 1 + $State.Edits }
    ///     New-ConsoleTable -Bind { $services | Where-Object Name -Like "*$($State.Query)*" } -Property Name, Status
    ///     New-ConsoleBinding -Content { "Edits: $($State.Edits)" }
    /// } -PassThru</code>
    /// </example>
    /// <example>
    /// <para>Capture a scripted run without a physical console.</para>
    /// <code># Using the object directly
    /// $capture = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Interactive.ScreenCapture'))::new(80, 24)
    /// $keys = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Input.ScriptedInput'))::new('"abc" Escape')
    /// Show-ConsoleApp -Content { New-ConsoleTextInput -Key Query } -InputSource $keys -Capture $capture -PassThru</code>
    /// </example>
    [Cmdlet(VerbsCommon.Show, "ConsoleApp")]
    [OutputType(typeof(Hashtable))]
    public sealed class CmdletShowConsoleApp : PSCmdlet {
        private readonly object _stopGate = new object();
        private CancellationTokenSource? _stop;
        private bool _stopping;

        /// <summary><para type="description">Layout script emitting stable inputs and widgets. Invoked once on the caller's thread.</para></summary>
        [Parameter(Mandatory = true, Position = 0)]
        public ScriptBlock Content { get; set; } = null!;

        /// <summary><para type="description">Returns final named values as one ordinary hashtable. Callback output is discarded.</para></summary>
        [Parameter]
        public SwitchParameter PassThru { get; set; }

        /// <summary><para type="description">Prints or captures one initialized snapshot without reading input; usable in CI or redirected output.</para></summary>
        [Parameter]
        public SwitchParameter StaticSnapshot { get; set; }

        /// <summary><para type="description">Disables optional terminal mouse reporting while retaining keyboard operation.</para></summary>
        [Parameter]
        public SwitchParameter NoMouse { get; set; }

        /// <summary><para type="description">Interaction mode override, as in Read-ConsoleText. A running app requires Rich; use -StaticSnapshot without interactive input.</para></summary>
        [Parameter]
        public InteractionMode? Mode { get; set; }

        /// <summary><para type="description">Input source, including ScriptedInput for examples and tests. Uses the existing core input lease.</para></summary>
        [Parameter]
        public IInputSource? InputSource { get; set; }

        /// <summary><para type="description">Memory capture instead of terminal output; also supplies default screen dimensions.</para></summary>
        [Parameter]
        public ScreenCapture? Capture { get; set; }

        /// <summary><para type="description">Terminal capabilities and dimensions; defaults to the existing interactive profile resolution.</para></summary>
        [Parameter]
        public TerminalProfile? Profile { get; set; }

        /// <summary><para type="description">Core theme used to render controls and dependent widgets.</para></summary>
        [Parameter]
        public ITheme? Theme { get; set; }

        /// <summary><para type="description">Optional full-screen live rendering settings, passed directly to the core runner. Inline mode is rejected before input or state initialization.</para></summary>
        [Parameter]
        public LiveSessionOptions? Live { get; set; }

        /// <inheritdoc />
        protected override void EndProcessing() {
            CancellationTokenSource stop;
            lock (_stopGate) {
                stop = _stop = new CancellationTokenSource();
                if (_stopping) { stop.Cancel(); }
            }
            try {
                stop.Token.ThrowIfCancellationRequested();
                IWidget layout = new Rows(PSWidgets.Collect(Content).ToArray());
                InteractiveOptions interactive = PSInteractive.Options(this, Mode);
                if (InputSource != null) {
                    interactive.Input = InputSource;
                    // The canonical helper may choose Line for a non-console PowerShell host.
                    // An explicitly injected input is the interactive device for this run.
                    if (!Mode.HasValue) { interactive.Mode = InteractionMode.Rich; }
                }
                if (Capture != null) { interactive.Capture = Capture; }
                if (Profile != null) { interactive.Profile = Profile; }
                if (Theme != null) { interactive.Theme = Theme; }
                using (PSAppCallbacks callbacks = new PSAppCallbacks(this)) {
                    callbacks.Attach(layout);
                    AppState state = ConsoleApp.Run(layout, null, new ConsoleAppOptions {
                        Interactive = interactive,
                        Live = Live,
                        Mouse = !NoMouse.IsPresent,
                        StaticSnapshot = StaticSnapshot.IsPresent,
                        ProcessEvents = callbacks.Drain
                    }, stop.Token);
                    if (PassThru.IsPresent) { WriteObject(PSAppScript.Snapshot(state), false); }
                }
            } catch (Exception ex) when (ex is PromptUnavailableException || ex is PromptCanceledException || ex is InputClosedException) {
                PSInteractive.Fail(this, ex, MyInvocation.MyCommand.Name);
                throw;
            } finally {
                lock (_stopGate) {
                    _stop = null;
                    stop.Dispose();
                }
            }
        }

        /// <inheritdoc />
        protected override void StopProcessing() {
            lock (_stopGate) {
                _stopping = true;
                _stop?.Cancel();
            }
            base.StopProcessing();
        }
    }
}
