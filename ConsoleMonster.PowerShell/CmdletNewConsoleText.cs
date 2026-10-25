using System.Management.Automation;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Creates rich text: multicolour words for table cells, calculated properties, titles, labels, log lines or output on its own.</para>
    /// <para type="description">-Text takes pieces that are shown literally (data is never interpreted); -Color and -BackgroundColor take one colour per piece, and pieces beyond the colours use the first colour, as Write-Color does. Colours are ConsoleColor names exactly as Write-Host shows them, theme roles (Pass, Fail, Warn, Info, Muted, Accent) that follow the light or dark theme, #RRGGBB, rgb(r,g,b) or r,g,b. -Markup adds Spectre-compatible markup ([bold lime]Ready[/]); -Decoration adds Bold, Dim, Italic, Underline, Strikethrough or Invert; -Link makes the text a hyperlink where the terminal supports it; -Gradient colours the text from one colour to another. The result works as a cell value (also from a calculated property), as the title or label of any widget, with Write-ConsoleLogEntry and Update-Console, and with Show-Console. Without colour (NO_COLOR, redirected output) the text stays plain.</para>
    /// </summary>
    /// <example>
    ///   <para>A status cell from a calculated property</para>
    ///   <code>Get-Service | Show-ConsoleTable -Property Name, @{ Name = 'Status'; Expression = { if ($_.Status -eq 'Running') { New-ConsoleText 'Running' -Color Green } else { New-ConsoleText $_.Status -Role Fail -Decoration Bold } } }</code>
    /// </example>
    /// <example>
    ///   <para>Multicolour words, Write-Color style</para>
    ///   <code>New-ConsoleText 'SRV', '01', ' online' -Color Cyan, Yellow, Green | Show-Console</code>
    /// </example>
    /// <example>
    ///   <para>Markup and a gradient</para>
    ///   <code>New-ConsoleText -Markup '[bold]ConsoleMonster[/] [muted]2.0[/]'; New-ConsoleText 'ConsoleMonster' -Gradient '#00c6ff', '#ff00aa'</code>
    /// </example>
    [Cmdlet(VerbsCommon.New, "ConsoleText")]
    [OutputType(typeof(RichText))]
    public sealed class CmdletNewConsoleText : PSCmdlet {
        /// <summary><para type="description">The text pieces, shown literally (strings, or rich text from New-ConsoleText).</para></summary>
        [Parameter(Position = 0, ValueFromPipeline = true)]

        [AllowEmptyString]
        [AllowNull]
        public object[]? Text { get; set; }

        /// <summary><para type="description">Foreground colour per piece: ConsoleColor names (PowerShell mapping), theme roles, #RRGGBB, rgb(r,g,b) or r,g,b.</para></summary>
        [Parameter]

        public object[]? Color { get; set; }

        /// <summary><para type="description">Background colour per piece.</para></summary>
        [Parameter]

        public object[]? BackgroundColor { get; set; }

        /// <summary><para type="description">Markup added after the pieces ([bold lime]Ready[/] in [muted]3s[/]).</para></summary>
        [Parameter]
        public string? Markup { get; set; }

        /// <summary><para type="description">Decorations for the whole text: Bold, Dim, Italic, Underline, Strikethrough, Invert.</para></summary>
        [Parameter]
        public string[]? Decoration { get; set; }

        /// <summary><para type="description">A hyperlink target for the whole text (OSC 8 in Windows Terminal and VS Code, plain text elsewhere).</para></summary>
        [Parameter]
        public string? Link { get; set; }

        /// <summary><para type="description">A theme role for pieces without their own colour (Pass, Fail, Warn, Info, Muted, Accent...).</para></summary>
        [Parameter]

        public ThemeToken? Role { get; set; }

        /// <summary><para type="description">Two colours: the text is coloured from the first to the second, per character.</para></summary>
        [Parameter]
        public object[]? Gradient { get; set; }

        /// <summary><para type="description">Alignment when the text is written as a widget: Left, Center or Right.</para></summary>
        [Parameter]
        public HorizontalAlignment Alignment { get; set; } = HorizontalAlignment.Left;

        /// <summary><para type="description">Overflow policy when rendered in constrained cells.</para></summary>
        [Parameter] public Widgets.TextOverflow Overflow { get; set; } = Widgets.TextOverflow.Wrap;

        /// <inheritdoc />
        protected override void ProcessRecord() {
            RichText text = PSRichText.Build(Text, Color, BackgroundColor, Markup, Decoration, Link, Role, Gradient);
            text.Alignment = Alignment;
            text.Overflow = Overflow;
            WriteObject(text);
        }
    }
}
