using System;
using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Layout;
using ConsoleMonster.Views;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Shows objects of registered types through ConsoleMonster and passes everything else to PowerShell's Out-Default.</para>
    /// <para type="description">This is what Enable-ConsoleFormatting puts behind Out-Default. Consecutive objects of registered types (Register-ConsoleView; after Enable-ConsoleFormatting also files and folders, processes, services, commands, modules, event log records, IP addresses, volumes and dictionaries) are collected and shown as one ConsoleMonster view; any other object, format data from Format-*, errors and strings go to PowerShell's Out-Default unchanged. It only takes over when the output is displayed on a real local console: redirected output, jobs, remoting and other hosts get PowerShell's default output (-Force renders anyway). The objects themselves are never changed.</para>
    /// </summary>
    /// <example>
    ///   <para>Show processes the ConsoleMonster way once</para>
    ///   <code>Get-Process | Out-ConsoleDefault</code>
    /// </example>
    [Cmdlet(VerbsData.Out, "ConsoleDefault")]
    public sealed class CmdletOutConsoleDefault : PSCmdlet {
        private readonly List<PSObject> _batch = new List<PSObject>();
        private ConsoleViewRegistration? _batchView;
        private SteppablePipeline? _inner;
        private bool _active;
        private readonly List<IWidget> _exportWidgets = new List<IWidget>();
        private readonly List<object> _exportObjects = new List<object>();

        /// <summary><para type="description">Returns registered views as semantic Markdown, fenced Text or an Html page, including in redirected output. Other objects use the normal ConsoleMonster table exporter; native Format-* records should go through Out-ConsoleFormatCommand -AsWidget and Export-ConsoleFrame. Cannot be combined with -Transcript or the internal -Shim.</para></summary>
        [Parameter]
        [ValidateSet("Text", "Markdown", "Html")]
        public string? As { get; set; }

        /// <summary><para type="description">Width in cells for an exported document (default 100).</para></summary>
        [Parameter]
        [ValidateRange(1, 1000)]
        public int Width { get; set; } = 100;

        /// <summary><para type="description">The objects to show.</para></summary>
        [Parameter(ValueFromPipeline = true)]
        public PSObject? InputObject { get; set; }

        /// <summary><para type="description">Passed to Out-Default (the host transcribes the output).</para></summary>
        [Parameter]
        public SwitchParameter Transcript { get; set; }

        /// <summary><para type="description">Renders registered types even when the output is redirected or the host is not a console (as plain lines there).</para></summary>
        [Parameter]
        public SwitchParameter Force { get; set; }

        /// <summary><para type="description">Set by the Out-Default function of Enable-ConsoleFormatting: only the enabled features apply (registered types, the error view, the progress area).</para></summary>
        [Parameter(DontShow = true)]
        public SwitchParameter Shim { get; set; }

        /// <summary><para type="description">Set by the Out-Default function when PowerShell added it to a command typed at the prompt (not an explicit Out-Default in a script): the progress area ends with that command.</para></summary>
        [Parameter(DontShow = true)]
        public SwitchParameter TopLevel { get; set; }

        private bool _types = true;
        private bool _errors;
        private bool _progress;

        /// <inheritdoc />
        protected override void BeginProcessing() {
            ConsoleModuleViews.Discover(this);
            if (As != null) {
                if (Transcript.IsPresent || Shim.IsPresent) {
                    ThrowTerminatingError(new ErrorRecord(new ArgumentException("-As cannot be combined with -Transcript or -Shim."), "IncompatibleOutputOptions", ErrorCategory.InvalidArgument, null));
                }
                _active = true;
                return;
            }
            _active = Force.IsPresent || (!Transcript.IsPresent && ConsoleFormatting.Displayed(this) && FormatCommandRouter.IsUntranscribed(this));
            if (Shim.IsPresent) {
                ConsoleFormattingFeature features = ConsoleFormatting.Features;
                _types = (features & ConsoleFormattingFeature.Output) != 0;
                _errors = _active && (features & ConsoleFormattingFeature.ErrorView) != 0 && (Force.IsPresent || !ErrorRedirected());
                _progress = (features & ConsoleFormattingFeature.Progress) != 0;
                if (_progress && TopLevel.IsPresent) {
                    // A new pipeline: an area left by an interrupted one (Ctrl+C) goes.
                    SessionProgress.EndPipeline();
                }
            }
            ScriptBlock inner = ScriptBlock.Create(Transcript.IsPresent ? "Microsoft.PowerShell.Core\\Out-Default -Transcript" : "Microsoft.PowerShell.Core\\Out-Default");
            _inner = inner.GetSteppablePipeline(CommandOrigin.Internal);
            _inner.Begin(this);
        }

        /// <inheritdoc />
        protected override void ProcessRecord() {
            Handle(InputObject, true);
        }

        // A collection that arrives as one object (Write-Output -NoEnumerate, an array of arrays) is shown one level
        // unrolled, as PowerShell's Out-Default shows it, using PowerShell's own rule for what is enumerable (not strings,
        // dictionaries or XML nodes).
        private void Handle(PSObject? item, bool unroll) {
            if (_progress) {
                // Output goes where the progress area was; the next Write-Progress draws it again below.
                SessionProgress.Suspend();
            }
            if (item == null) {
                Flush();
                _inner?.Process(item);
                return;
            }
            // Get-Error's detailed records (PSExtendedError) keep PowerShell's view.
            if (_errors && item.BaseObject is ErrorRecord record && !IsExtended(item) && WriteErrorView(record)) {
                return;
            }
            ConsoleViewRegistration? view = _active && _types ? ConsoleFormatting.Find(item, As != null || ConsoleFormatting.TakeOverBuiltIns) ?? DictionaryView(item) : null;
            if (view != null) {
                FlushExportObjects();
                if (_batchView != null && !string.Equals(_batchView.Group, view.Group, StringComparison.OrdinalIgnoreCase)) {
                    Flush();
                }
                _batchView ??= view;
                _batch.Add(item);
                return;
            }
            System.Collections.IEnumerable? elements = unroll ? LanguagePrimitives.GetEnumerable(item.BaseObject) : null;
            if (elements != null) {
                foreach (object? element in elements) {
                    Handle(element == null ? null : PSObject.AsPSObject(element), false);
                }
                return;
            }
            Flush();
            if (As != null) {
                _exportObjects.Add(item);
                return;
            }
            _inner!.Process(item);
        }

        /// <inheritdoc />
        protected override void EndProcessing() {
            if (_progress && TopLevel.IsPresent) {
                SessionProgress.EndPipeline();
            }
            Flush();
            if (As != null) {
                FlushExportObjects();
                if (_exportWidgets.Count > 0) {
                    WriteObject(PSFormatting.Export(PSFormatting.Combine(_exportWidgets), PSOutputExport.Format(As), Width));
                }
            }
            _inner?.End();
        }

        // The error view of Enable-ConsoleFormatting -Feature ErrorView: the type, the message, the category and id, the
        // position in the script (and the stack with $ErrorView = 'DetailedView'). CategoryView keeps PowerShell's view.
        private bool WriteErrorView(ErrorRecord record) {
            string view = Convert.ToString(GetVariableValue("ErrorView"), System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;
            if (string.Equals(view, "CategoryView", StringComparison.OrdinalIgnoreCase)) {
                return false;
            }
            Flush();
            ExceptionView widget = new ExceptionView(PSErrors.Describe(record)) { ShowStack = string.Equals(view, "DetailedView", StringComparison.OrdinalIgnoreCase) };
            // Through PowerShell's Out-Default as a rendering (drawn by ConsoleMonster's out-of-band view), so it stays in
            // order with the table rows PowerShell may still be holding.
            _inner!.Process(PSObject.AsPSObject(new ConsoleRendering(widget, 0, null) { DisplayOnly = true }));
            return true;
        }

        private static bool IsExtended(PSObject item) {
            foreach (string name in item.TypeNames) {
                if (name.EndsWith("#PSExtendedError", StringComparison.OrdinalIgnoreCase)) {
                    return true;
                }
            }
            return false;
        }

        // Errors written to a redirected stderr (2> file) keep PowerShell's route, so they land in the file.
        private static bool ErrorRedirected() {
            try {
                return Console.IsErrorRedirected;
            } catch (System.IO.IOException) {
                return true;
            }
        }

        /// <inheritdoc />
        protected override void StopProcessing() {
            _inner?.Dispose();
        }

        // Dictionaries (@{ }, [ordered]) are shown as ConsoleMonster records (one) or tables of their keys (several), like
        // every other taken-over type, instead of PowerShell's Name/Value table.
        private static readonly ConsoleViewRegistration Dictionaries = new ConsoleViewRegistration("System.Collections.IDictionary", ConsoleViewKind.Table, null, null, "Dictionary", null, true);

        private static ConsoleViewRegistration? DictionaryView(PSObject item) =>
            ConsoleFormatting.TakeOverBuiltIns && item.BaseObject is System.Collections.IDictionary ? Dictionaries : null;

        private void Flush() {
            if (_batch.Count == 0 || _batchView == null) {
                return;
            }
            IWidget widget;
            switch (_batchView.View) {
                case ConsoleViewKind.Table:
                case ConsoleViewKind.Cards:
                    bool cards = _batchView.View == ConsoleViewKind.Cards;
                    // The same columns, titles and cell values as Format-ConsoleTable: the registered view is found again
                    // by the column resolver, so its columns, its title ("Directory: ...") and its grouping apply.
                    List<IWidget> tables = PSFormatting.BuildTables(_batch, null, null, new TableBuildOptions { AutoPresenters = true, Cmdlet = this }, table => {
                        if (_batchView.NativePresentation) { table.RecordViewRows = 0; }
                        if (cards) {
                            table.AlwaysCards = true;
                        }
                    });
                    widget = PSFormatting.Combine(tables);
                    break;
                default:
                    ConsoleViewOptions options = new ConsoleViewOptions { View = _batchView.View };
                    options.Sources.Add(new Binding.DirectoryEntryMemberSource());
                    options.Sources.Add(new Binding.PSObjectMemberSource());
                    widget = ConsoleView.Auto(new List<object?>(_batch), options);
                    break;
            }
            if (As != null) {
                _exportWidgets.Add(widget);
            } else {
                PSFormatting.Write(this, widget, 0);
            }
            _batch.Clear();
            _batchView = null;
        }

        private void FlushExportObjects() {
            if (_exportObjects.Count > 0) {
                _exportWidgets.Add(PSOutputExport.Widget(_exportObjects, this));
                _exportObjects.Clear();
            }
        }

    }
}
