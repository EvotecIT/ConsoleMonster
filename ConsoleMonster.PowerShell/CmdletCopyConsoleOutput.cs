using System;
using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Clipboard;
using ConsoleMonster.Export;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Copies widgets, frames, text or pipeline objects to the operating-system clipboard.</para>
    /// <para type="description">Serializes input with ConsoleExporter as Text (default), Markdown or Html and copies the source as Unicode text. Objects become tables, widgets retain their layout, and a single frame or live-session snapshot keeps its size. Markdown is a fenced preformatted block. Windows uses its native clipboard, macOS uses pbcopy, and Linux uses wl-copy (Wayland) or xclip (X11). SSH, a missing desktop/provider or a failing provider produces a non-terminating error; use -ErrorAction Stop to catch it. No output is written unless -PassThru succeeds. -WhatIf exports without accessing the clipboard.</para>
    /// </summary>
    /// <example>
    ///   <para>Copy a process table into a Markdown report</para>
    ///   <code>Get-Process | Select-Object -First 10 Name, Id | Format-ConsoleTable -AsWidget | Copy-ConsoleOutput -As Markdown</code>
    /// </example>
    [Cmdlet(VerbsCommon.Copy, "ConsoleOutput", SupportsShouldProcess = true)]
    [OutputType(typeof(string))]
    public sealed class CmdletCopyConsoleOutput : PSCmdlet {
        private readonly List<object> _items = new List<object>();

        /// <summary><para type="description">Widgets, rendered frames, live sessions, literal text or objects to display as tables.</para></summary>
        [Parameter(Mandatory = true, ValueFromPipeline = true, Position = 0)]
        [AllowNull]
        [AllowEmptyString]
        public object? InputObject { get; set; }

        /// <summary><para type="description">Copies Text, Markdown or Html source as clipboard text (default Text).</para></summary>
        [Parameter]
        [ValidateSet("Text", "Markdown", "Html")]
        public string As { get; set; } = "Text";

        /// <summary><para type="description">Width in cells for rendering widgets and objects (default 100); single frames keep their own size.</para></summary>
        [Parameter]
        [ValidateRange(1, 1000)]
        public int Width { get; set; } = 100;

        /// <summary><para type="description">Height in cells, or 0 to measure the full content.</para></summary>
        [Parameter]
        [ValidateRange(0, 1000)]
        public int Height { get; set; }

        /// <summary><para type="description">The culture for values in objects and widgets.</para></summary>
        [Parameter]
        public System.Globalization.CultureInfo? Culture { get; set; }

        /// <summary><para type="description">Also returns the serialized document after a successful copy.</para></summary>
        [Parameter]
        public SwitchParameter PassThru { get; set; }

        /// <inheritdoc />
        protected override void ProcessRecord() {
            if (InputObject != null) {
                _items.Add(InputObject);
            }
        }

        /// <inheritdoc />
        protected override void EndProcessing() {
            if (_items.Count == 0) {
                return;
            }
            string document = PSOutputExport.Document(_items, PSOutputExport.Format(As), new ExportOptions { Width = Width, Height = Height, Culture = Culture }, this);
            if (!ShouldProcess("Operating-system clipboard", "Copy " + As + " output")) {
                return;
            }
            if (!new ClipboardService().TryCopy(document, out string? error)) {
                WriteError(new ErrorRecord(new InvalidOperationException(error), "ConsoleClipboardUnavailable", ErrorCategory.ResourceUnavailable, null));
                return;
            }
            if (PassThru.IsPresent) {
                WriteObject(document);
            }
        }
    }
}
