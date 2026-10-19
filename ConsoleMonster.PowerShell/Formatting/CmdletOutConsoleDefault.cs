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
    /// <example>
    ///   <para>Show processes the ConsoleMonster way once</para>
    ///   <code>Get-Process | Out-ConsoleDefault</code>
    /// </example>
    /// </summary>
    [Cmdlet(VerbsData.Out, "ConsoleDefault")]
    public sealed class CmdletOutConsoleDefault : PSCmdlet {
        private readonly List<PSObject> _batch = new List<PSObject>();
        private ConsoleViewRegistration? _batchView;
        private SteppablePipeline? _inner;
        private bool _active;

        /// <summary><para type="description">The objects to show.</para></summary>
        [Parameter(ValueFromPipeline = true)]
        public PSObject? InputObject { get; set; }

        /// <summary><para type="description">Passed to Out-Default (the host transcribes the output).</para></summary>
        [Parameter]
        public SwitchParameter Transcript { get; set; }

        /// <summary><para type="description">Renders registered types even when the output is redirected or the host is not a console (as plain lines there).</para></summary>
        [Parameter]
        public SwitchParameter Force { get; set; }

        /// <inheritdoc />
        protected override void BeginProcessing() {
            _active = Force.IsPresent || Displayed();
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
            if (item == null) {
                Flush();
                _inner!.Process(item);
                return;
            }
            ConsoleViewRegistration? view = _active ? ConsoleFormatting.Find(item, ConsoleFormatting.TakeOverBuiltIns) ?? DictionaryView(item) : null;
            if (view != null) {
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
            _inner!.Process(item);
        }

        /// <inheritdoc />
        protected override void EndProcessing() {
            Flush();
            _inner?.End();
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
            PSFormatting.Write(this, widget, 0);
            _batch.Clear();
            _batchView = null;
        }
        // Only output displayed on a real local console: not redirected, not a job, remoting or another host.
        private bool Displayed() {
            if (!string.Equals(Host?.Name, "ConsoleHost", StringComparison.Ordinal)) {
                return false;
            }
            try {
                if (Console.IsOutputRedirected) {
                    return false;
                }
            } catch (System.IO.IOException) {
                return false;
            }
            return Terminal.IsConsole(Terminal.Target);
        }
    }
}
