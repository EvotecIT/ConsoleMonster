using System;
using System.Collections.Generic;
using System.Globalization;
using System.Management.Automation;
using ConsoleMonster.Canvas;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

using ConsoleMonster.Output;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Writes multicolour text to the console and, optionally, to a log file: Write-Color compatible, with markup, styles, gradients, padding, wrapping and structured logs.</para>
    /// <para type="description">The parameters and aliases of Write-Color (PSWriteColor) work the same way: -Text pieces get -Color and -BackGroundColor per piece (pieces beyond the colours use the first colour), -StartTab, -StartSpaces, -LinesBefore, -LinesAfter, -ShowTime with -DateTimeFormat, -NoNewLine, -HorizontalCenter, -LogFile with -LogTime, -LogRetry and -Encoding, and -NoConsoleOutput. ConsoleColor names render exactly as Write-Host shows them. Beyond Write-Color: -Markup, -Style, -Link, -Role, hex and RGB colours, -Gradient, -PadLeft/-PadRight/-PadCenter with -PadCharacter (measured in terminal cells, so emoji and CJK pad correctly), -Wrap with a hanging indent under the time and prefix, -SplitLines with the following lines aligned under the first, and -LogFormat Text, Csv or JsonLines with -LogSeparator, -Level and -Category. Differences: the default colour is the terminal's own (Write-Color uses White), and a new log file is UTF-8 without a byte order mark by default (Write-Color writes UTF-16); appending to an existing log keeps its encoding, so Write-Color's UTF-16 logs stay UTF-16. In transcripts each call is one line; in jobs and remoting the colours travel with the output; redirected output is plain text. There is deliberately no Write-Color or wc alias.</para>
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

        /// <summary><para type="description">Foreground colour per piece: ConsoleColor names (exactly as Write-Host shows them: DarkRed is ANSI red, Red bright red; grey spells Gray), theme roles, #RRGGBB, rgb(r,g,b), r,g,b, or a markup palette name (cadetblue; palette.darkred for the palette colour where the bare name is a ConsoleColor).</para></summary>
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

        /// <summary><para type="description">The encoding of a new log file: utf8 without a byte order mark by default (Write-Color defaults to unicode, which is UTF-16 LE); Write-Color's names mean the same here (unicode, string and unknown are UTF-16 LE, bigendianunicode UTF-16 BE, utf32 UTF-32 LE), plus utf8BOM and utf8NoBOM. Appending to an existing file always keeps that file's encoding (detected from its byte order mark, or UTF-16 text without one), so a UTF-16 log from Write-Color stays UTF-16; when -Encoding names a different one, a warning says so.</para></summary>
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
            ConsoleTextLayout layout = new ConsoleTextLayout {
                StartTab = StartTab,
                StartSpaces = StartSpaces,
                LinesBefore = LinesBefore,
                LinesAfter = LinesAfter,
                Time = ShowTime.IsPresent ? now : (DateTime?)null,
                TimeFormat = DateTimeFormat,
                SplitLines = SplitLines,
                Wrap = Wrap.IsPresent,
                HorizontalCenter = HorizontalCenter.IsPresent,
                PadLeft = PadLeft,
                PadRight = PadRight,
                PadCenter = PadCenter,
                PadCharacter = PadCharacter
            };
            if (!NoConsoleOutput.IsPresent) {
                HostOutput.WriteLines(this, (context, width) => layout.Lines(body, context, width), !NoNewLine.IsPresent, Width);
            }
            if (!string.IsNullOrEmpty(LogFile) && (Text != null || Markup != null)) {
                List<string> parts = new List<string>();
                foreach (RichText part in layout.Parts(body)) {
                    parts.Add(part.PlainText);
                }
                ConsoleLogFile log = new ConsoleLogFile(SessionState.Path.GetUnresolvedProviderPathFromPSPath(LogFile)) {
                    Format = (ConsoleLogFormat)Enum.Parse(typeof(ConsoleLogFormat), LogFormat, true),
                    Separator = LogSeparator,
                    Encoding = ConsoleLogFile.ResolveEncoding(Encoding),
                    EncodingSpecified = MyInvocation.BoundParameters.ContainsKey(nameof(Encoding)),
                    Retries = LogRetry,
                    IncludeTime = LogTime,
                    TimeFormat = DateTimeFormat,
                    Warning = message => WriteWarning("Write-ConsoleText - " + message)
                };
                log.Append(new ConsoleLogRecord(now, string.Join("\n", parts.ToArray()), Level, Category));
            }
        }
    }
}