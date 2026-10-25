using System;
using System.Collections;
using System.Management.Automation;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Creates a reusable footer with key hints, messages and linked segments.</para>
    /// <para type="description">Hint maps key names to their labels. Segment accepts FooterSegment values or hashtables with Text, Link and Role. The footer is a widget and does not read input or start a session.</para>
    /// </summary>
    /// <example><para>Show navigation hints.</para><code>New-ConsoleFooterBar -Hint ([ordered] @{ Enter = 'Accept'; Esc = 'Cancel' }) | Show-Console</code></example>
    [Cmdlet(VerbsCommon.New, "ConsoleFooterBar")]
    [OutputType(typeof(FooterBar))]
    public sealed class CmdletNewConsoleFooterBar : PSCmdlet {
        /// <summary><para type="description">An ordered dictionary of key names and action labels.</para></summary>
        [Parameter(Position = 0)] public IDictionary? Hint { get; set; }
        /// <summary><para type="description">A message shown instead of key hints.</para></summary>
        [Parameter] public object? Message { get; set; }
        /// <summary><para type="description">Text at the right of the footer.</para></summary>
        [Parameter] public object? Right { get; set; }
        /// <summary><para type="description">FooterSegment objects or Text/Link/Role hashtables.</para></summary>
        [Parameter] public object[] Segment { get; set; } = Array.Empty<object>();
        /// <summary><para type="description">Keeps the last hint when space runs out.</para></summary>
        [Parameter] public SwitchParameter KeepLast { get; set; }
        /// <summary><para type="description">Initial indentation in terminal cells.</para></summary>
        [Parameter] [ValidateRange(0, 1000)] public int Indent { get; set; } = 1;
        /// <inheritdoc />
        protected override void EndProcessing() {
            FooterBar footer = new FooterBar { KeepLast = KeepLast.IsPresent, Indent = Indent, Message = Line(Message), Right = Line(Right) };
            if (Hint != null) {
                foreach (KeyHint hint in PSFooterHints.Read(Hint)) { footer.Hints.Add(hint); }
            }
            foreach (object item in Segment) {
                object? raw = PSText.Unwrap(item);
                if (raw is FooterSegment segment) { footer.Segments.Add(segment); }
                else if (raw is IDictionary data) {
                    footer.Segments.Add(new FooterSegment(PSWidgets.ToText(data["Text"]), data["Link"] as string,
                        data["Role"] == null ? null : LanguagePrimitives.ConvertTo<ThemeToken>(data["Role"])));
                } else { throw new PSArgumentException("Segment must be a FooterSegment or a Text/Link/Role hashtable."); }
            }
            WriteObject(footer);
        }
        private static StyledLine? Line(object? value) => value == null ? null : Terminal.CreateContext().Markup(PSText.Markup(value) ?? string.Empty).Lines[0];
    }
}
