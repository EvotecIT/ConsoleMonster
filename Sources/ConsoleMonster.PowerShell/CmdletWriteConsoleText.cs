using System;
using System.Collections.Generic;
using System.Globalization;
using System.Management.Automation;
using ConsoleMonster.Canvas;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Writes multicolour text to the console and, optionally, to a log file: Write-Color compatible, with markup, styles, gradients, padding, wrapping and structured logs.</para>
    /// <para type="description">The parameters and aliases of Write-Color (PSWriteColor) work the same way, so most scripts migrate with a rename: -Text pieces get -Color and -BackGroundColor per piece (pieces beyond the colours use the first colour), -StartTab, -StartSpaces, -LinesBefore, -LinesAfter, -ShowTime with -DateTimeFormat, -NoNewLine, -HorizontalCenter, -LogFile with -LogTime, -LogRetry and -Encoding, and -NoConsoleOutput. ConsoleColor names render exactly as Write-Host shows them. Beyond Write-Color: -Markup, -Style, -Link, -Role, hex and RGB colours, -Gradient, -PadLeft/-PadRight/-PadCenter with -PadCharacter (measured in terminal cells, so emoji and CJK pad correctly), -Wrap with a hanging indent under the time and prefix, -SplitLines with the following lines aligned under the first, and -LogFormat Text, Csv or JsonLines with -LogSeparator, -Level and -Category. Differences: the default colour is the terminal's own (Write-Color uses White), and the log file is UTF-8 without a byte order mark by default (Write-Color writes UTF-16). In transcripts each call is one line; in jobs and remoting the colours travel with the output; redirected output is plain text. There is deliberately no Write-Color or wc alias.</para>
    /// <example>
    ///   <para>Write-Color style</para>
    ///   <code>Write-ConsoleText -Text 'Updating group: ', $Group -Color Gray, Green -StartTab 1</code>
    /// </example>
    /// <example>
    ///   <para>A menu with padding and a log file in CSV</para>
    ///   <code>Write-ConsoleText '1.', ' Option one' -Color Yellow, Green -PadRight 30 -LogFile .\menu.csv -LogFormat Csv -Level Info</code>
    /// </example>
    /// <example>
    ///   <para>A multi-line message aligned under the time stamp</para>
    ///   <code>Write-ConsoleText '[info] ', "Path exists`n2. line`n3. line" -Color Yellow, White -ShowTime -SplitLines "`n"</code>
    /// </example>
    /// </summary>
    [Cmdlet(VerbsCommunications.Write, "ConsoleText")]
    public sealed class CmdletWriteConsoleText : PSCmdlet {
        /// <summary><para type="description">Text pieces, shown literally (strings, or rich text from New-ConsoleText).</para></summary>
        [Parameter(Position = 0, ValueFromPipeline = true)]
        [Alias("T")]
        [AllowEmptyString]
        [AllowNull]
        public object[]? Text { get; set; }

        /// <summary><para type="description">Foreground colour per piece: ConsoleColor names (exactly as Write-Host shows them), theme roles, #RRGGBB, rgb(r,g,b) or r,g,b.</para></summary>
        [Parameter]
        [Alias("C", "ForegroundColor", "FGC")]
        public object[]? Color { get; set; }

        /// <summary><para type="description">Background colour per piece.</para></summary>
        [Parameter]
        [Alias("B", "BGC")]
        public object[]? BackGroundColor { get; set; }

        /// <summary><para type="description">Tabs before the text (eight cells each).</para></summary>
        [Parameter]
        [Alias("Indent")]
        public int StartTab { get; set; }

        /// <summary><para type="description">Empty lines before the text.</para></summary>
        [Parameter]
        public int LinesBefore { get; set; }

        /// <summary><para type="description">Empty lines after the text.</para></summary>
        [Parameter]
        public int LinesAfter { get; set; }

        /// <summary><para type="description">Spaces before the text.</para></summary>
        [Parameter]
        public int StartSpaces { get; set; }

        /// <summary><para type="description">A log file the text is appended to (plain text, see -LogFormat).</para></summary>
        [Parameter]
        [Alias("L")]
        public string? LogFile { get; set; }

        /// <summary><para type="description">The time format for -ShowTime and the log (default yyyy-MM-dd HH:mm:ss).</para></summary>
        [Parameter]
        [Alias("DateFormat", "TimeFormat")]
        public string DateTimeFormat { get; set; } = "yyyy-MM-dd HH:mm:ss";

        /// <summary><para type="description">Whether log lines start with the time (default $true).</para></summary>
        [Parameter]
        [Alias("LogTimeStamp")]
        public bool LogTime { get; set; } = true;

        /// <summary><para type="description">Attempts to write the log file before giving up with a warning (default 2).</para></summary>
        [Parameter]
        public int LogRetry { get; set; } = 2;

        /// <summary><para type="description">The log file encoding (default utf8 without a byte order mark; Write-Color's names are accepted, plus utf8BOM and utf8NoBOM).</para></summary>
        [Parameter]
        [ValidateSet("unknown", "string", "unicode", "bigendianunicode", "utf8", "utf8BOM", "utf8NoBOM", "utf7", "utf32", "ascii", "default", "oem", IgnoreCase = true)]
        public string Encoding { get; set; } = "utf8";

        /// <summary><para type="description">Starts the line with the time in brackets.</para></summary>
        [Parameter]
        public SwitchParameter ShowTime { get; set; }

        /// <summary><para type="description">Leaves the line open, so the next output continues on it.</para></summary>
        [Parameter]
        public SwitchParameter NoNewLine { get; set; }

        /// <summary><para type="description">Centres each line in the window (measured in terminal cells, so emoji and CJK centre correctly).</para></summary>
        [Parameter]
        public SwitchParameter HorizontalCenter { get; set; }

        /// <summary><para type="description">Writes only to the log file.</para></summary>
        [Parameter]
        [Alias("HideConsole")]
        public SwitchParameter NoConsoleOutput { get; set; }

        /// <summary><para type="description">Markup added after the pieces ([bold lime]Ready[/] in [muted]3s[/]).</para></summary>
        [Parameter]
        public string? Markup { get; set; }

        /// <summary><para type="description">Decorations for the whole text: Bold, Dim, Italic, Underline, Strikethrough, Invert.</para></summary>
        [Parameter]
        public string[]? Style { get; set; }

        /// <summary><para type="description">A hyperlink target for the whole text (OSC 8 where supported).</para></summary>
        [Parameter]
        public string? Link { get; set; }

        /// <summary><para type="description">A theme role for pieces without their own colour (Pass, Fail, Warn, Info, Muted, Accent...).</para></summary>
        [Parameter]
        public ThemeToken? Role { get; set; }

        /// <summary><para type="description">Two colours: the text is coloured from the first to the second (per character; steps on 16-colour terminals).</para></summary>
        [Parameter]
        public object[]? Gradient { get; set; }

        /// <summary><para type="description">Pads the text on the left to this many cells (right-aligned).</para></summary>
        [Parameter]
        public int PadLeft { get; set; }

        /// <summary><para type="description">Pads the text on the right to this many cells.</para></summary>
        [Parameter]
        public int PadRight { get; set; }

        /// <summary><para type="description">Centres the text within this many cells.</para></summary>
        [Parameter]
        public int PadCenter { get; set; }

        /// <summary><para type="description">The padding character (default a space).</para></summary>
        [Parameter]
        public char PadCharacter { get; set; } = ' ';

        /// <summary><para type="description">Wraps long text at the window width, continuing under the text after the time and prefix.</para></summary>
        [Parameter]
        public SwitchParameter Wrap { get; set; }

        /// <summary><para type="description">The width for -Wrap and -HorizontalCenter (default the window width).</para></summary>
        [Parameter]
        public int Width { get; set; }

        /// <summary><para type="description">Splits the text at this separator; the following lines are aligned under the first, after the time and prefix.</para></summary>
        [Parameter]
        public string? SplitLines { get; set; }

        /// <summary><para type="description">The log format: Text (the Write-Color layout), Csv or JsonLines.</para></summary>
        [Parameter]
        [ValidateSet("Text", "Csv", "JsonLines", IgnoreCase = true)]
        public string LogFormat { get; set; } = "Text";

        /// <summary><para type="description">The CSV separator (default a comma).</para></summary>
        [Parameter]
        public string LogSeparator { get; set; } = ",";

        /// <summary><para type="description">A level for the log record (Info, Warning, Error...).</para></summary>
        [Parameter]
        public string? Level { get; set; }

        /// <summary><para type="description">A category for the log record.</para></summary>
        [Parameter]
        public string? Category { get; set; }

        /// <inheritdoc />
        protected override void ProcessRecord() {
            RichText body = PSRichText.Build(Text, Color, BackGroundColor, Markup, Style, Link, Role, Gradient);
            DateTime now = DateTime.Now;
            if (!NoConsoleOutput.IsPresent) {
                HostOutput.WriteLines(this, (context, width) => BuildLines(context, width, body, now), !NoNewLine.IsPresent, Width);
            }
            if (!string.IsNullOrEmpty(LogFile) && (Text != null || Markup != null)) {
                string path = SessionState.Path.GetUnresolvedProviderPathFromPSPath(LogFile);
                List<string> parts = new List<string>();
                foreach (RichText part in Parts(body)) {
                    parts.Add(part.PlainText);
                }
                ConsoleTextLog.Write(this, path, new ConsoleTextLog.Record(now, string.Join("\n", parts.ToArray()), Level, Category), LogFormat, LogSeparator, Encoding, LogRetry, LogTime, DateTimeFormat);
            }
        }

        private List<RichText> Parts(RichText body) {
            return string.IsNullOrEmpty(SplitLines) ? new List<RichText> { body } : PSRichText.Split(body, SplitLines!);
        }

        // The console lines: empty lines before, the prefix (tabs, spaces, time) on the first line, split and wrapped lines
        // aligned under the text, padding, centring, empty lines after.
        private IReadOnlyList<StyledLine> BuildLines(RenderContext context, int width, RichText body, DateTime now) {
            List<StyledLine> result = new List<StyledLine>();
            for (int i = 0; i < LinesBefore; i++) {
                result.Add(StyledLine.Empty);
            }
            string prefix = new string(' ', (Math.Max(0, StartTab) * 8) + Math.Max(0, StartSpaces));
            if (ShowTime.IsPresent) {
                prefix += "[" + now.ToString(DateTimeFormat, CultureInfo.CurrentCulture) + "] ";
            }
            int prefixWidth = DisplayWidth.Measure(prefix);
            List<KeyValuePair<StyledLine, bool>> lines = new List<KeyValuePair<StyledLine, bool>>();
            List<RichText> parts = Parts(body);
            for (int p = 0; p < parts.Count; p++) {
                IReadOnlyList<StyledLine> styled = parts[p].ToStyledText(context).Lines;
                for (int l = 0; l < styled.Count; l++) {
                    // Split parts continue under the text (hanging); line breaks inside a part start at the left, as Write-Host does.
                    lines.Add(new KeyValuePair<StyledLine, bool>(Pad(styled[l]), p > 0 && l == 0));
                }
            }
            bool first = true;
            foreach (KeyValuePair<StyledLine, bool> entry in lines) {
                IReadOnlyList<StyledLine> pieces = Wrap.IsPresent ? TextWrapper.Wrap(entry.Key, Math.Max(1, width - prefixWidth)) : new[] { entry.Key };
                for (int w = 0; w < pieces.Count; w++) {
                    List<TextSpan> spans = new List<TextSpan>();
                    if (first) {
                        spans.Add(new TextSpan(prefix, Styling.Style.Plain));
                    } else if (entry.Value || w > 0) {
                        spans.Add(new TextSpan(new string(' ', prefixWidth), Styling.Style.Plain));
                    }
                    spans.AddRange(pieces[w]);
                    StyledLine line = new StyledLine(spans);
                    if (HorizontalCenter.IsPresent && line.Width < width) {
                        List<TextSpan> centred = new List<TextSpan> { new TextSpan(new string(' ', (width - line.Width) / 2), Styling.Style.Plain) };
                        centred.AddRange(line);
                        line = new StyledLine(centred);
                    }
                    result.Add(line);
                    first = false;
                }
            }
            if (lines.Count == 0) {
                result.Add(StyledLine.From(prefix));
            }
            for (int i = 0; i < LinesAfter; i++) {
                result.Add(StyledLine.Empty);
            }
            return result;
        }

        // -PadCenter, then -PadLeft, then -PadRight: the first one given applies; widths are terminal cells.
        private StyledLine Pad(StyledLine line) {
            int target = PadCenter > 0 ? PadCenter : PadLeft > 0 ? PadLeft : PadRight;
            if (target <= line.Width) {
                return line;
            }
            int missing = target - line.Width;
            int before = PadCenter > 0 ? missing / 2 : PadLeft > 0 ? missing : 0;
            int after = missing - before;
            int fill = Math.Max(1, DisplayWidth.Measure(PadCharacter.ToString()));
            List<TextSpan> spans = new List<TextSpan>();
            if (before > 0) {
                spans.Add(new TextSpan(new string(PadCharacter, before / fill) + new string(' ', before % fill), Styling.Style.Plain));
            }
            spans.AddRange(line);
            if (after > 0) {
                spans.Add(new TextSpan(new string(PadCharacter, after / fill) + new string(' ', after % fill), Styling.Style.Plain));
            }
            return new StyledLine(spans);
        }
    }
}
