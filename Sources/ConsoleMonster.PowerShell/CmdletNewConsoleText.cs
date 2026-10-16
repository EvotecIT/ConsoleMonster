using System.Management.Automation;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Creates a text block from Spectre-compatible markup.</para>
    /// <para type="description">Markup such as [bold], [red], [#ff8800 on navy] and theme tokens such as [pass], [warn] or [muted] is supported. Use -Plain to show brackets literally. Text wraps to the available width.</para>
    /// <example>
    ///   <para>Styled text</para>
    ///   <code>New-ConsoleText '[pass]Ready[/] in [muted]3.2s[/]'</code>
    /// </example>
    /// </summary>
    [Cmdlet(VerbsCommon.New, "ConsoleText")]
    [OutputType(typeof(TextBlock))]
    public sealed class CmdletNewConsoleText : PSCmdlet {
        /// <summary><para type="description">The text (markup unless -Plain).</para></summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true)]
        [AllowEmptyString]
        public string Text { get; set; } = string.Empty;

        /// <summary><para type="description">Show the text literally, without markup.</para></summary>
        [Parameter]
        public SwitchParameter Plain { get; set; }

        /// <summary><para type="description">Alignment: Left, Center or Right.</para></summary>
        [Parameter]
        public HorizontalAlignment Alignment { get; set; } = HorizontalAlignment.Left;

        /// <summary><para type="description">A theme token applied to the whole text (Muted, Accent, Pass, Fail, Warn...).</para></summary>
        [Parameter]
        public ThemeToken? Token { get; set; }

        /// <inheritdoc />
        protected override void ProcessRecord() {
            TextBlock block = Plain.IsPresent ? TextBlock.Plain(Text, Token) : new TextBlock(Text) { Token = Token };
            block.Alignment = Alignment;
            WriteObject(block);
        }
    }
}
