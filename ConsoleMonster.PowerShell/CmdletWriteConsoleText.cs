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
    /// <para type="synopsis">Writes coloured text with optional markup, alignment, wrapping and a text log.</para>
    /// <para type="description">Text is literal; Color and BackgroundColor apply per piece. Role, Decoration, Link and Gradient apply to the message. Indent adds spaces and Alignment places text within Width. ShowTimestamp adds a timestamp. NoNewLine continues the current line. LogFile appends plain text with timestamps; new logs use UTF-8 and existing logs retain their encoding. Redirected output is plain text.</para>
    /// </summary>
    /// <example>
    ///   <para>Colour two pieces and indent the message</para>
    ///   <code>Write-ConsoleText -Text 'Checking group: ', 'Operators' -Color Gray, Green -Indent 4</code>
    /// </example>
    /// <example>
    ///   <para>Centre a heading within sixty columns</para>
    ///   <code>Write-ConsoleText 'Maintenance complete' -Role Pass -Alignment Center -Width 60</code>
    /// </example>
    /// <example>
    ///   <para>A multi-line message aligned under the time stamp</para>
    ///   <code>Write-ConsoleText '[info] ', "Path exists`n2. line`n3. line" -Color Yellow, White -ShowTimestamp -SplitLines "`n"</code>
    /// </example>
    [Cmdlet(VerbsCommunications.Write, "ConsoleText")]
    public sealed class CmdletWriteConsoleText : PSCmdlet {
        /// <summary><para type="description">Spaces before the text.</para></summary>
        [Parameter, ValidateRange(0, 1000)] public int Indent { get; set; }
        /// <summary><para type="description">Left, Center or Right within the available width.</para></summary>
        [Parameter] public Layout.HorizontalAlignment Alignment { get; set; } = Layout.HorizontalAlignment.Left;
        /// <summary><para type="description">Text pieces, shown literally (strings, or rich text from New-ConsoleText).</para></summary>
        [Parameter(Position = 0, ValueFromPipeline = true)]

        [AllowEmptyString]
        [AllowNull]
        public object[]? Text { get; set; }

        /// <summary><para type="description">Foreground colour per piece: ConsoleColor names (exactly as Write-Host shows them: DarkRed is ANSI red, Red bright red; grey spells Gray), theme roles, #RRGGBB, rgb(r,g,b), r,g,b, or a markup palette name (cadetblue; palette.darkred for the palette colour where the bare name is a ConsoleColor).</para></summary>
        [Parameter]

        public object[]? Color { get; set; }

        /// <summary><para type="description">Background colour per piece.</para></summary>
        [Parameter]

        public object[]? BackgroundColor { get; set; }

        /// <summary><para type="description">Empty lines before the text.</para></summary>
        [Parameter]
        public int LinesBefore { get; set; }

        /// <summary><para type="description">Empty lines after the text.</para></summary>
        [Parameter]
        public int LinesAfter { get; set; }

        /// <summary><para type="description">A log file the text is appended to (plain text, see -LogFormat).</para></summary>
        [Parameter]

        public string? LogFile { get; set; }

        /// <summary><para type="description">The time format for -ShowTimestamp and the log (default yyyy-MM-dd HH:mm:ss).</para></summary>
        [Parameter]

        public string TimestampFormat { get; set; } = "yyyy-MM-dd HH:mm:ss";

        /// <summary><para type="description">Starts the line with the time in brackets.</para></summary>
        [Parameter]
        public SwitchParameter ShowTimestamp { get; set; }

        /// <summary><para type="description">Leaves the line open, so the next output continues on it.</para></summary>
        [Parameter]
        public SwitchParameter NoNewLine { get; set; }

        /// <summary><para type="description">Markup added after the pieces ([bold lime]Ready[/] in [muted]3s[/]).</para></summary>
        [Parameter]
        public string? Markup { get; set; }

        /// <summary><para type="description">Decorations for the whole text: Bold, Dim, Italic, Underline, Strikethrough, Invert.</para></summary>
        [Parameter]
        public string[]? Decoration { get; set; }

        /// <summary><para type="description">A hyperlink target for the whole text (OSC 8 where supported).</para></summary>
        [Parameter]
        public string? Link { get; set; }

        /// <summary><para type="description">A theme role for pieces without their own colour (Pass, Fail, Warn, Info, Muted, Accent...).</para></summary>
        [Parameter]

        public ThemeToken? Role { get; set; }

        /// <summary><para type="description">Two colours: the text is coloured from the first to the second (per character; steps on 16-colour terminals).</para></summary>
        [Parameter]
        public object[]? Gradient { get; set; }

        /// <summary><para type="description">Wraps long text at the window width, continuing under the text after the time and prefix.</para></summary>
        [Parameter]
        public SwitchParameter Wrap { get; set; }

        /// <summary><para type="description">The width for -Wrap and -Alignment (default the window width).</para></summary>
        [Parameter]
        [ValidateRange(0, 1000)]
        public int Width { get; set; }

        /// <summary><para type="description">Splits the text at this separator; the following lines are aligned under the first, after the time and prefix.</para></summary>
        [Parameter]
        public string? SplitLines { get; set; }

        /// <inheritdoc />
        protected override void ProcessRecord() {
            RichText body = PSRichText.Build(Text, Color, BackgroundColor, Markup, Decoration, Link, Role, Gradient);
            DateTime now = DateTime.Now;
            ConsoleTextLayout layout = new ConsoleTextLayout {
                StartSpaces = Indent,
                LinesBefore = LinesBefore,
                LinesAfter = LinesAfter,
                Time = ShowTimestamp.IsPresent ? now : (DateTime?)null,
                TimestampFormat = TimestampFormat,
                SplitLines = SplitLines,
                Wrap = Wrap.IsPresent,
                HorizontalCenter = Alignment == Layout.HorizontalAlignment.Center
            };
            HostOutput.WriteLines(this, (context, width) => {
                layout.PadLeft = Alignment == Layout.HorizontalAlignment.Right ? width : 0;
                return layout.Lines(body, context, width);
            }, !NoNewLine.IsPresent, Width);
            if (!string.IsNullOrEmpty(LogFile) && (Text != null || Markup != null)) {
                List<string> parts = new List<string>();
                foreach (RichText part in layout.Parts(body)) {
                    parts.Add(part.PlainText);
                }
                ConsoleLogFile log = new ConsoleLogFile(SessionState.Path.GetUnresolvedProviderPathFromPSPath(LogFile)) {
                    TimeFormat = TimestampFormat,
                    Warning = message => WriteWarning("Write-ConsoleText - " + message)
                };
                log.Append(new ConsoleLogRecord(now, string.Join("\n", parts.ToArray())));
            }
        }
    }
}
