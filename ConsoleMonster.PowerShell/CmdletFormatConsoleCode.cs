using System;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation;
using System.Text;
using ConsoleMonster.Highlighting;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Shows source text with syntax highlighting, line numbers and highlighted lines.</para>
    /// <para type="description">Highlights JSON, XML/HTML, YAML, PowerShell, C#, SQL, INI/TOML, CSV/TSV, diffs, log lines and Markdown, or plain text with -Language Smart (numbers, booleans, GUIDs, IP addresses, URLs, paths, dates, DNs, quoted strings). -Language Auto (the default) picks the language from the file extension, then from the content, and falls back to plain text. Text comes from -Path or from pipeline strings (joined as lines). The text is always literal: markup is not read and control characters are dropped. -LineNumbers adds a gutter, -Highlight marks lines (10, '10-14'), long lines are truncated with an ellipsis or wrapped with -Wrap, and output stops after -MaxLines lines with "+n more lines". A file is shown on a panel titled with its name (-Title changes it, -NoTitle leaves the panel out). Redirected output is plain text.</para>
    /// </summary>
    /// <example>
    ///   <para>A script with line numbers and two lines marked</para>
    ///   <code>Format-ConsoleCode -Path .\deploy.ps1 -LineNumbers -Highlight '12-13'</code>
    /// </example>
    /// <example>
    ///   <para>A log file's last lines</para>
    ///   <code>Get-Content .\agent.log -Tail 20 | Format-ConsoleCode -Language Log</code>
    /// </example>
    [Cmdlet(VerbsCommon.Format, "ConsoleCode", DefaultParameterSetName = "Text")]
    [OutputType(typeof(IWidget))]
    public sealed class CmdletFormatConsoleCode : PSCmdlet {
        private readonly StringBuilder _text = new StringBuilder();

        /// <summary><para type="description">Text to show (pipeline strings are joined as lines).</para></summary>
        [Parameter(Position = 0, ValueFromPipeline = true, ParameterSetName = "Text")]
        [AllowEmptyString]
        public object? InputObject { get; set; }

        /// <summary><para type="description">A file to show.</para></summary>
        [Parameter(Mandatory = true, ParameterSetName = "Path")]
        [Alias("FullName")]
        public string Path { get; set; } = string.Empty;

        /// <summary><para type="description">The language: Auto (default), Json, Xml, Yaml, PowerShell, CSharp, Sql, Ini, Csv, Diff, Log, Markdown, Smart, Text, or a language registered from C#.</para></summary>
        [Parameter]
        public string Language { get; set; } = "Auto";

        /// <summary><para type="description">Shows line numbers.</para></summary>
        [Parameter]
        public SwitchParameter LineNumbers { get; set; }

        /// <summary><para type="description">Lines to mark: numbers or ranges such as 10, '10-14'.</para></summary>
        [Parameter]
        public string[]? Highlight { get; set; }

        /// <summary><para type="description">The number of the first line (default 1), for excerpts.</para></summary>
        [Parameter]
        public int FirstLine { get; set; } = 1;

        /// <summary><para type="description">Wraps long lines instead of truncating them.</para></summary>
        [Parameter]
        public SwitchParameter Wrap { get; set; }

        /// <summary><para type="description">The most lines shown (default 2000).</para></summary>
        [Parameter]
        public int MaxLines { get; set; } = 2000;

        /// <summary><para type="description">The panel title (default the file name with -Path; none for pipeline text).</para></summary>
        [Parameter]
        public string? Title { get; set; }

        /// <summary><para type="description">Leaves the panel out.</para></summary>
        [Parameter]
        public SwitchParameter NoTitle { get; set; }

        /// <summary><para type="description">Writes the view as one string of plain text instead of drawing it, to keep it in a variable or a file from any script, function or PowerShell version. Without it, PowerShell 7.2+ captures a view typed at the prompt ($x = ..., &gt; file, Out-File); inside scripts and functions, and on Windows PowerShell 5.1, the view is drawn on the screen. Piped into another command (Out-Host, Out-String, Select-String) the view goes on as plain lines, without colour. Cannot be combined with -PassThru.</para></summary>
        [Parameter]
        public SwitchParameter AsString { get; set; }
        /// <summary><para type="description">Lays the output out for this width instead of the terminal's.</para></summary>
        [Parameter]
        public int Width { get; set; }

        /// <summary><para type="description">Returns the view instead of writing it.</para></summary>
        [Parameter]
        public SwitchParameter AsWidget { get; set; }

        /// <summary><para type="description">Returns semantic Markdown, fenced Text with the exact console layout, or a self-contained Html page. Cannot be combined with -AsString or -AsWidget.</para></summary>
        [Parameter]
        [ValidateSet("Text", "Markdown", "Html")]
        public string? As { get; set; }

        /// <inheritdoc />
        protected override void BeginProcessing() => PSOutputExport.Validate(this);

        /// <inheritdoc />
        protected override void ProcessRecord() {
            if (ParameterSetName != "Text" || InputObject == null) {
                return;
            }
            object value = InputObject is PSObject ps ? ps.BaseObject : InputObject;
            if (_text.Length > 0) {
                _text.Append('\n');
            }
            _text.Append(value as string ?? LanguagePrimitives.ConvertTo<string>(value));
        }

        /// <inheritdoc />
        protected override void EndProcessing() {
            string? path = null;
            string text;
            if (ParameterSetName == "Path") {
                path = SessionState.Path.GetUnresolvedProviderPathFromPSPath(Path);
                text = File.ReadAllText(path);
            } else {
                text = _text.ToString();
            }
            WriteView(this, text, Language, path, view => {
                view.LineNumbers = LineNumbers.IsPresent;
                view.FirstLine = FirstLine;
                view.Wrap = Wrap.IsPresent;
                view.MaxLines = Math.Max(1, MaxLines);
                view.Title = NoTitle.IsPresent ? null : Title ?? (path == null ? null : System.IO.Path.GetFileName(path));
                foreach (string range in Highlight ?? new string[0]) {
                    view.HighlightLines(range);
                }
            }, Width, AsWidget.IsPresent, AsString.IsPresent);
        }

        internal static void WriteView(PSCmdlet cmdlet, string text, string language, string? path, Action<CodeView> configure, int width, bool asWidget, bool asString = false) {
            CodeView view;
            try {
                view = CodeView.For(text, language, path);
                configure(view);
            } catch (ArgumentException ex) {
                throw new PSArgumentException(ex.Message, ex);
            }
            if (asWidget) {
                cmdlet.WriteObject(view);
            } else {
                PSFormatting.Emit(cmdlet, view, width, null, asString);
            }
        }    }

    /// <summary>
    /// <para type="synopsis">Shows JSON with syntax highlighting: JSON text re-indented, or objects written as JSON.</para>
    /// <para type="description">A thin convenience over Format-ConsoleCode -Language Json. Pipeline strings are joined (so Get-Content works); when they are JSON they are re-indented, otherwise they are shown as a JSON string. Other objects are collected and written with ConsoleMonster's shared value rules to -Depth levels (default 4): dictionaries with any key type, cycles as "(cycle)", values past the depth as "…", secure strings masked and byte arrays as their size.</para>
    /// </summary>
    /// <example>
    ///   <para>An object as highlighted JSON</para>
    ///   <code>Get-Process -Id $PID | Select-Object Name, Id, StartTime | Format-ConsoleJson</code>
    /// </example>
    /// <example>
    ///   <para>A JSON file with line numbers</para>
    ///   <code>Get-Content .\settings.json | Format-ConsoleJson -LineNumbers</code>
    /// </example>
    [Cmdlet(VerbsCommon.Format, "ConsoleJson")]
    [OutputType(typeof(IWidget))]
    public sealed class CmdletFormatConsoleJson : PSCmdlet {
        private readonly List<object> _objects = new List<object>();
        private readonly StringBuilder _text = new StringBuilder();

        /// <summary><para type="description">JSON text or objects (pipeline input).</para></summary>
        [Parameter(Position = 0, ValueFromPipeline = true)]
        [AllowEmptyString]
        public object? InputObject { get; set; }

        /// <summary><para type="description">How many levels of objects and lists are written (default 4); deeper values are shown as one line of text.</para></summary>
        [Parameter]
        public int Depth { get; set; } = 4;

        /// <summary><para type="description">Shows line numbers.</para></summary>
        [Parameter]
        public SwitchParameter LineNumbers { get; set; }

        /// <summary><para type="description">The most lines shown (default 2000).</para></summary>
        [Parameter]
        public int MaxLines { get; set; } = 2000;

        /// <summary><para type="description">A panel title, or none.</para></summary>
        [Parameter]
        public string? Title { get; set; }

        /// <summary><para type="description">Writes the view as one string of plain text instead of drawing it, to keep it in a variable or a file from any script, function or PowerShell version. Without it, PowerShell 7.2+ captures a view typed at the prompt ($x = ..., &gt; file, Out-File); inside scripts and functions, and on Windows PowerShell 5.1, the view is drawn on the screen. Piped into another command (Out-Host, Out-String, Select-String) the view goes on as plain lines, without colour. Cannot be combined with -PassThru.</para></summary>
        [Parameter]
        public SwitchParameter AsString { get; set; }
        /// <summary><para type="description">Lays the output out for this width instead of the terminal's.</para></summary>
        [Parameter]
        public int Width { get; set; }

        /// <summary><para type="description">Returns the view instead of writing it.</para></summary>
        [Parameter]
        public SwitchParameter AsWidget { get; set; }

        /// <summary><para type="description">Returns semantic Markdown, fenced Text with the exact console layout, or a self-contained Html page. Cannot be combined with -AsString or -AsWidget.</para></summary>
        [Parameter]
        [ValidateSet("Text", "Markdown", "Html")]
        public string? As { get; set; }

        /// <inheritdoc />
        protected override void BeginProcessing() => PSOutputExport.Validate(this);

        /// <inheritdoc />
        protected override void ProcessRecord() {
            if (InputObject == null) {
                return;
            }
            object value = InputObject is PSObject ps && !(ps.BaseObject is PSCustomObject) ? ps.BaseObject : InputObject;
            _objects.Add(InputObject);
            if (value is string text) {
                if (_text.Length > 0) {
                    _text.Append('\n');
                }
                _text.Append(text);
            } else {
                _allText = false;
            }
        }

        private bool _allText = true;

        /// <inheritdoc />
        protected override void EndProcessing() {
            if (_objects.Count == 0) {
                return;
            }
            string json;
            string text = _text.ToString();
            if (_allText && Values.ValueJson.TryReindent(text, out string indented)) {
                // JSON text (Get-Content settings.json, a REST response): re-indented and highlighted.
                json = indented;
            } else {
                // Objects, or text that is not JSON (a JSON string value then), written with the shared value rules:
                // dictionaries with any keys, cycles marked, secrets masked, the pipeline order kept.
                object input = _objects.Count == 1 ? _objects[0] : _objects;
                json = Values.ValueJson.Serialize(input, Math.Max(0, Depth), Binding.PSShaping.Options());
            }            CmdletFormatConsoleCode.WriteView(this, json, "json", null, view => {
                view.LineNumbers = LineNumbers.IsPresent;
                view.MaxLines = Math.Max(1, MaxLines);
                view.Title = Title;
            }, Width, AsWidget.IsPresent, AsString.IsPresent);
        }
    }
}
