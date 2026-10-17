using System;
using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Layout;
using ConsoleMonster.Views;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Shows objects of registered types through ConsoleMonster and passes everything else to PowerShell's Out-Default.</para>
    /// <para type="description">This is what Enable-ConsoleFormatting puts behind Out-Default. Consecutive objects of registered types (Register-ConsoleView; files and folders, processes and services after Enable-ConsoleFormatting) are collected and shown as one ConsoleMonster view; any other object, format data from Format-*, errors and strings go to PowerShell's Out-Default unchanged. It only takes over when the output is displayed on a real local console: redirected output, jobs, remoting and other hosts get PowerShell's default output (-Force renders anyway). The objects themselves are never changed.</para>
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
            if (InputObject == null) {
                Flush();
                _inner!.Process(InputObject);
                return;
            }
            ConsoleViewRegistration? view = _active ? ConsoleFormatting.Find(InputObject) : null;
            if (view != null) {
                if (_batchView != null && !string.Equals(_batchView.Group, view.Group, StringComparison.OrdinalIgnoreCase)) {
                    Flush();
                }
                _batchView ??= view;
                _batch.Add(InputObject);
                return;
            }
            Flush();
            _inner!.Process(InputObject);
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

        private void Flush() {
            if (_batch.Count == 0 || _batchView == null) {
                return;
            }
            IWidget widget;
            switch (_batchView.View) {
                case ConsoleViewKind.Table:
                case ConsoleViewKind.Cards:
                    object? groupBy = _batchView.GroupBy;
                    string? title = null;
                    if (_batchView.Title != null) {
                        title = SharedTitle(_batchView.Title);
                        if (title == null) {
                            groupBy ??= _batchView.Title;
                        }
                    }
                    Table table = PSFormatting.BuildTable(_batch, _batchView.Property, groupBy, false, true);
                    table.Title = title == null ? null : Text.Markup.Escape(title);
                    if (_batchView.View == ConsoleViewKind.Cards) {
                        table.AlwaysCards = true;
                    }
                    widget = table;
                    break;
                default:
                    ConsoleViewOptions options = new ConsoleViewOptions { View = _batchView.View };
                    options.Sources.Add(new Binding.PSObjectMemberSource());
                    widget = ConsoleView.Auto(new List<object?>(_batch), options);
                    break;
            }
            PSFormatting.Write(this, widget, 0);
            _batch.Clear();
            _batchView = null;
        }

        // The title every object in the batch shares, or null when they differ.
        private string? SharedTitle(ScriptBlock title) {
            string? shared = null;
            foreach (PSObject item in _batch) {
                string text = string.Join(" ", title.InvokeWithContext(null, new List<PSVariable> { new PSVariable("_", item) }));
                if (shared == null) {
                    shared = text;
                } else if (!string.Equals(shared, text, StringComparison.Ordinal)) {
                    return null;
                }
            }
            return shared;
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
