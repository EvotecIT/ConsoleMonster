using System;
using System.Collections;
using System.Management.Automation;
using ConsoleMonster.Widgets;
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
    /// <para type="description">Builds the layout once, then runs input synchronously on the caller's PowerShell thread. Callbacks are queued by controls and drained before input reads; dependent builders run on that same thread. Escape exits. Cancellation restores the terminal through the core runner. Use -Mode Snapshot to capture or print initialized values without interactive input. CONSOLEMONSTER_SNAPSHOT=1 also selects an initialized snapshot when no input source is supplied; CONSOLEMONSTER_INPUT supplies scripted keys centrally. Nothing is sent to the pipeline unless -PassThru requests one ordinary hashtable.</para>
    /// </summary>
    /// <example>
    /// <para>Filter application data without leaving PowerShell</para>
    /// <code>$servers = @(
    ///     [pscustomobject]@{ Name = 'web01'; Status = 'Pass' }
    ///     [pscustomobject]@{ Name = 'web02'; Status = 'Warn' }
    /// )
    /// Show-ConsoleApp -Content {
    ///     New-ConsoleTextInput -Key Query -OnChange { param($Value, $State) $State.Edits = 1 + $State.Edits }
    ///     New-ConsoleTable -Bind { $servers | Where-Object Name -Like "*$($State.Query)*" } -Property Name, Status
    ///     New-ConsoleBinding -Content { "Edits: $($State.Edits)" }
    /// } -PassThru</code>
    /// </example>
    /// <example>
    /// <para>Print initialized values without reading keyboard input</para>
    /// <code>Show-ConsoleApp -Mode Snapshot -Content {
    ///     New-ConsoleTextInput -Key Query -Value 'web'
    ///     New-ConsoleBinding -Content { "Query: $($State.Query)" }
    /// } -PassThru</code>
    /// </example>
    [Cmdlet(VerbsCommon.Show, "ConsoleApp", DefaultParameterSetName = "Footer")]
    [OutputType(typeof(Hashtable))]
    public sealed class CmdletShowConsoleApp : NoPipelineInputCmdlet {
        /// <summary><para type="description">Host focus, cancel and palette gestures. Set a gesture to null to release it to the app registry.</para></summary>
        [Parameter] public AppHostKeys Keys { get; set; } = new AppHostKeys();
        /// <summary><para type="description">Allows the terminal to generate Ctrl+C and Ctrl+backslash signals instead of app input events.</para></summary>
        [Parameter] public SwitchParameter SignalKeys { get; set; }
        /// <summary><para type="description">App-thread command registry routing before the focused input. Return true when consumed.</para></summary>
        [Parameter] public Func<AppState, InputEvent, bool>? HandleInput { get; set; }
        /// <summary><para type="description">Child-terminal and last-output service attached for this app run.</para></summary>
        [Parameter] public TerminalHandoff? TerminalHandoff { get; set; }
        /// <summary><para type="description">Typed modal form service attached for this app run.</para></summary>
        [Parameter] public AppDialogs? Dialogs { get; set; }
        /// <summary><para type="description">App-thread action service for starting the shared worker from registry and dialog callbacks.</para></summary>
        [Parameter] public AppActions? Actions { get; set; }
        /// <summary><para type="description">Context key bar, normally a FooterBar reading the app's command registry.</para></summary>
        [Parameter(ParameterSetName = "Footer")] public object? Footer { get; set; }
        /// <summary><para type="description">Dictionaries mapping key names to labels, or typed KeyHint values.</para></summary>
        [Parameter(ParameterSetName = "Hints")] public object[]? FooterHints { get; set; }
        /// <summary><para type="description">Optional heading above the application content.</para></summary>
        [Parameter] public object? Title { get; set; }
        /// <summary><para type="description">Text qualifying the application title.</para></summary>
        [Parameter] public object? Subtitle { get; set; }
        /// <summary><para type="description">Current detail beside the application heading.</para></summary>
        [Parameter] public object? Detail { get; set; }
        /// <summary><para type="description">Semantic status in the application heading.</para></summary>
        [Parameter, Binding.StatusKindTransformation] public Styling.StatusKind? Status { get; set; }
        /// <summary><para type="description">Minimum terminal width; smaller windows show a resize message without losing focus.</para></summary>
        [Parameter, ValidateRange(0, 10000)] public int MinimumWidth { get; set; }
        /// <summary><para type="description">Minimum terminal height; zero permits any height.</para></summary>
        [Parameter, ValidateRange(0, 10000)] public int MinimumHeight { get; set; }
        private readonly object _stopGate = new object();
        private CancellationTokenSource? _stop;
        private bool _stopping;

        /// <summary><para type="description">Layout script emitting stable inputs and widgets. Invoked once on the caller's thread.</para></summary>
        [Parameter(Mandatory = true, Position = 0)]
        public ScriptBlock Content { get; set; } = null!;

        /// <summary><para type="description">Returns final named values as one ordinary hashtable. Callback output is discarded.</para></summary>
        [Parameter]
        public SwitchParameter PassThru { get; set; }

        /// <summary><para type="description">Disables optional terminal mouse reporting while retaining keyboard operation.</para></summary>
        [Parameter]
        public SwitchParameter NoMouse { get; set; }

        /// <summary><para type="description">Shared command definitions for shortcuts and the in-app palette.</para></summary>
        [Parameter]
        public AppCommand[] Command { get; set; } = Array.Empty<AppCommand>();

        /// <summary><para type="description">App-state key containing the selected row or row collection supplied to commands.</para></summary>
        [Parameter]
        public string? SelectionKey { get; set; }

        /// <summary><para type="description">Interaction mode override, as in Read-ConsoleText. A running app requires Rich; use -Mode Snapshot without interactive input.</para></summary>
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

        /// <summary><para type="description">Default width below which columns stack; explicit column thresholds take precedence.</para></summary>
        [Parameter, ValidateRange(0, int.MaxValue)] public int ReflowBelow { get; set; } = 60;

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
                if (Title != null || Subtitle != null || Detail != null || Status.HasValue) {
                    var header = new HeaderBar(PSText.Markup(Title) ?? string.Empty) { Subtitle = PSText.Markup(Subtitle), Detail = PSText.Markup(Detail), Status = Status };
                    layout = new Rows(header, PSWidgets.WithSize(layout, 0, 0, true));
                }
                IWidget? footer = PSWidgets.AsWidget(Footer);
                if (Footer != null && footer == null) { throw new PSArgumentException("Footer must be text or a widget."); }
                if (FooterHints != null) {
                    var hints = new FooterBar();
                    foreach (KeyHint hint in PSFooterHints.Read(FooterHints)) { hints.Hints.Add(hint); }
                    footer = hints;
                }
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
                    ConsoleAppOptions options = new ConsoleAppOptions {
                        Interactive = interactive,
                        Mouse = !NoMouse.IsPresent,
                        ReflowBelow = ReflowBelow,
                        ProcessEvents = callbacks.Drain,
                        SelectionKey = SelectionKey,
                        Keys = Keys, SignalKeys = SignalKeys.IsPresent, HandleInput = HandleInput,
                        TerminalHandoff = TerminalHandoff, Dialogs = Dialogs, Actions = Actions, Footer = footer,
                        MinimumWidth = MinimumWidth, MinimumHeight = MinimumHeight
                    };
                    foreach (AppCommand command in Command) { options.Command.Add(command); }
                    AppState state = ConsoleApp.Run(layout, null, options, stop.Token);
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
