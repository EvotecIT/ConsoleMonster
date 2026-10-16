using System;
using System.Text;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Presenters {
    /// <summary>
    /// A hyperlink: clickable (OSC 8) in Windows Terminal and VS Code, plain text elsewhere. When the shown text differs from
    /// the address and the terminal cannot link, the address follows in brackets so nothing is lost in logs.
    /// </summary>
    public sealed class LinkPresenter : ICellPresenter {
        /// <summary>Creates a link presenter.</summary>
        /// <param name="text">Picks the shown text from the value, or null to show the address.</param>
        /// <param name="address">Picks the address from the value, or null to use the value itself.</param>
        public LinkPresenter(Func<object?, string>? text = null, Func<object?, string>? address = null) {
            TextSelector = text;
            AddressSelector = address;
        }

        /// <summary>Picks the shown text, or null.</summary>
        public Func<object?, string>? TextSelector { get; }

        /// <summary>Picks the address, or null.</summary>
        public Func<object?, string>? AddressSelector { get; }

        /// <inheritdoc />
        public PresentedValue Present(object? value, PresenterContext context) {
            if (PresenterValues.IsEmpty(value)) {
                return new PresentedValue(StyledLine.Empty) { IsEmpty = true };
            }
            string address = Clean(AddressSelector?.Invoke(value) ?? PresenterValues.ToText(value, context.Culture));
            string text = TextSelector?.Invoke(value) ?? address;
            Style style = context.Render.Style(ThemeToken.Accent).With(TextDecoration.Underline);
            if (context.Hyperlinks) {
                return new PresentedValue(StyledLine.From(text, style.WithLink(address)));
            }
            if (string.Equals(text, address, StringComparison.Ordinal)) {
                return new PresentedValue(StyledLine.From(text, context.Render.Style(ThemeToken.Accent)));
            }
            return new PresentedValue(new StyledLine(new[] {
                new TextSpan(text, context.Render.Style(ThemeToken.Accent)),
                new TextSpan(" (" + address + ")", context.Render.Style(ThemeToken.Muted))
            }));
        }

        // Control characters would terminate or corrupt the OSC 8 sequence.
        private static string Clean(string address) {
            StringBuilder sb = new StringBuilder(address.Length);
            foreach (char c in address) {
                if (c >= 0x20 && c != 0x7F) {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }
    }
}
