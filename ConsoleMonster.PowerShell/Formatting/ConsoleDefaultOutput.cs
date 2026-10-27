using System;
using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Layout;
using ConsoleMonster.Views;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>Native output routing policy; the provenance fields are supplied only by a PowerShell host adapter.</summary>
    public sealed class ConsoleDefaultOutputOptions {
        /// <summary>Renders registered views even outside a direct console.</summary>
        public bool Force { get; set; }
        /// <summary>Retains native transcript routing.</summary>
        public bool Transcript { get; set; }
        /// <summary>Applies only the features enabled for the installed default-output adapter.</summary>
        public bool Shim { get; set; }
        /// <summary>The host added this sink to a top-level submission.</summary>
        public bool TopLevel { get; set; }
        /// <summary>The native help producer whose output provenance the host already verified.</summary>
        public PSCmdlet? HelpSink { get; set; }
    }

    /// <summary>Owns registered-view batching and the native Out-Default pipeline in a C# PowerShell host. Use it on the owning pipeline thread, call Complete for normal completion and Dispose for cancellation.</summary>
    public sealed class ConsoleDefaultOutput : IDisposable {
        private readonly PSCmdlet _host;
        private readonly ConsoleDefaultOutputOptions _options;
        private readonly List<PSObject> _batch = new List<PSObject>();
        private ConsoleViewRegistration? _batchView;
        private int _omittedRows;
        private SteppablePipeline? _inner;
        private bool _active;


        /// <summary>Starts output routing for one pipeline.</summary>
        /// <param name="host">The owning host cmdlet.</param>
        /// <param name="options">Routing and verified host provenance.</param>
        public ConsoleDefaultOutput(PSCmdlet host, ConsoleDefaultOutputOptions? options = null) {
            _host = host ?? throw new ArgumentNullException(nameof(host));
            _options = options ?? new ConsoleDefaultOutputOptions();
            Begin();
        }

        private bool _types = true;
        private bool _errors;
        private bool _compactErrors;
        private bool _progress;

        /// <summary>Starts native routing. Called once by the constructor.</summary>
        private void Begin() {
            ConsoleModuleViews.Discover(_host);

            _active = _options.Force || (!_options.Transcript && ConsoleFormatting.Displayed(_host) && FormatCommandRouter.IsUntranscribed(_host)
                && (!_options.Shim || Display.DefaultDisplayHostGate.CanRender(_host, _options.TopLevel, _options.Transcript, Terminal.GetCurrentProfile())));
            if (_options.Shim) {
                ConsoleFormattingFeature features = ConsoleFormatting.Features;
                _types = (features & ConsoleFormattingFeature.Output) != 0;
                _errors = _active && (features & ConsoleFormattingFeature.ErrorView) != 0 && (features & ConsoleFormattingFeature.Errors) == 0
                    && (_options.Force || !ErrorRedirected());
                _compactErrors = _active && (features & ConsoleFormattingFeature.Errors) != 0 && _options.TopLevel
                    && !_options.Transcript && !ErrorRedirected() && FormatCommandRouter.IsUnrecordedHost(_host);
                _progress = (features & ConsoleFormattingFeature.Progress) != 0;
                if (_progress && _options.TopLevel) {
                    // A new pipeline: an area left by an interrupted one (Ctrl+C) goes.
                    SessionProgress.EndPipeline();
                }
            }
            StartNative();
        }

        private void StartNative() {
            ScriptBlock inner = ScriptBlock.Create(_options.Transcript ? "Microsoft.PowerShell.Core\\Out-Default -Transcript" : "Microsoft.PowerShell.Core\\Out-Default");
            _inner = inner.GetSteppablePipeline(CommandOrigin.Internal);
            _inner.Begin(_host);
        }

        /// <summary>Routes one pipeline item through the registered view or native output.</summary>
        /// <param name="inputObject">The item, preserving its PowerShell wrapper.</param>
        public void Write(PSObject? inputObject) { Handle(inputObject, true); }

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
            if (_active && _options.Shim && !_options.Transcript && _options.HelpSink != null
                && HelpHostFormatting.FindSource(_options.HelpSink) is PSCmdlet source) {
                if (HelpHostFormatting.IsQuit(source)) { return; }
                if (HelpHostFormatting.CreateView(source, item, HelpHostFormatting.IsPaged(source)) is IWidget help) {
                    // Finish native output before drawing or pausing: help must not overtake a buffered table.
                    _inner!.End();
                    _inner.Dispose();
                    _inner = null;
                    Flush();
                    if (!HelpPager.Write(_host, help, HelpHostFormatting.IsPaged(source))) { HelpHostFormatting.Quit(source); }
                    StartNative();
                    return;
                }
            }
            // Get-Error's detailed records (PSExtendedError) keep PowerShell's view.
            if ((_errors || _compactErrors) && item.BaseObject is ErrorRecord record && !IsExtended(item) && WriteErrorView(record)) {
                return;
            }
            ConsoleViewRegistration? view = _active && _types ? ConsoleFormatting.Find(item, ConsoleFormatting.TakeOverBuiltIns) ?? DictionaryView(item) : null;
            // Generic structured data must not replace an explicit native type table view.
            if (view?.IsBuiltIn == true && view.IsStructuredData
                && item.BaseObject is PSCustomObject && Binding.FormatViews.HasTypedTableView(item, _host)) {
                view = null;
            }
            if (view != null) {
                if (_batchView != null && (!string.Equals(_batchView.Group, view.Group, StringComparison.OrdinalIgnoreCase)
                    || _batchView.IsStructuredData != view.IsStructuredData)) {
                    Flush();
                }
                _batchView ??= view;
                if (view.IsStructuredData && _batch.Count >= EverydayDataView.RowLimit) { _omittedRows++; }
                else { _batch.Add(item); }
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

            _inner!.Process(item);
        }

        /// <summary>Flushes buffered views and completes native output.</summary>
        public void Complete() {
            HelpHostFormatting.EndSink(_options.HelpSink);
            if (_progress && _options.TopLevel) {
                SessionProgress.EndPipeline();
            }
            Flush();

            _inner?.End();
        }

        // The error view of Enable-ConsoleFormatting -Feature ErrorView: the type, the message, the category and id, the
        // position in the script (and the stack with $ErrorView = 'DetailedView'). CategoryView keeps PowerShell's view.
        private bool WriteErrorView(ErrorRecord record) {
            // Upstream commands can start transcription or disable the feature after _host pipeline's BeginProcessing.
            if (_compactErrors && ((ConsoleFormatting.Features & ConsoleFormattingFeature.Errors) == 0
                || _options.Transcript || ErrorRedirected() || !FormatCommandRouter.IsUnrecordedHost(_host))) { return false; }
            string view = Convert.ToString(_host.SessionState.PSVariable.GetValue("ErrorView"), System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;
            if (string.Equals(view, "CategoryView", StringComparison.OrdinalIgnoreCase)) {
                return false;
            }
            Flush();
            ExceptionView widget = new ExceptionView(PSErrors.Describe(record, _compactErrors)) {
                Compact = _compactErrors,
                ShowStack = _compactErrors ? FormattingSessionState.Current.DetailedErrors : string.Equals(view, "DetailedView", StringComparison.OrdinalIgnoreCase),
                BasePath = _host.SessionState.Path.CurrentFileSystemLocation.ProviderPath,
                DetailsHint = _compactErrors && _host.InvokeCommand.GetCommand("Get-Error", CommandTypes.Cmdlet) != null
                    ? Localization.WidgetLabels.For(Terminal.GetCurrentProfile().Language).GetErrorDetails : null
            };
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

        /// <summary>Stops the native pipeline and releases its help paging marker.</summary>
        public void Dispose() {
            HelpHostFormatting.EndSink(_options.HelpSink);
            _inner?.Dispose();
            _inner = null;
        }

        // Dictionaries (@{ }, [ordered]) are shown as ConsoleMonster records (one) or tables of their keys (several), like
        // every other taken-over type, instead of PowerShell's Name/Value table.
        private static readonly ConsoleViewRegistration Dictionaries = ConsoleViewRegistration.Define("System.Collections.IDictionary", ConsoleViewKind.Table, null, null, "Dictionary", null, true);

        private static ConsoleViewRegistration? DictionaryView(PSObject item) =>
            ConsoleFormatting.TakeOverBuiltIns && item.BaseObject is System.Collections.IDictionary ? Dictionaries : null;

        private void Flush() {
            if (_batch.Count == 0 || _batchView == null) {
                return;
            }
            if (_batchView.IsStructuredData) {
                PSFormatting.Write(_host, EverydayDataView.Build(_batch, _omittedRows), 0);
                _batch.Clear();
                _omittedRows = 0;
                _batchView = null;
                return;
            }
            IWidget widget;
            switch (_batchView.View) {
                case ConsoleViewKind.Table:
                case ConsoleViewKind.Cards:
                    bool cards = _batchView.View == ConsoleViewKind.Cards;
                    // The same columns, titles and cell values as Show-ConsoleTable: the registered view is found again
                    // by the column resolver, so its columns, its title ("Directory: ...") and its grouping apply.
                    List<IWidget> tables = PSFormatting.BuildTables(_batch, null, null, new TableBuildOptions { AutoPresenters = true, Cmdlet = _host }, table => {
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
            PSFormatting.Write(_host, widget, 0);
            _batch.Clear();
            _batchView = null;
        }


    }
}
