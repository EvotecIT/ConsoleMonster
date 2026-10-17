using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;

namespace ConsoleMonster.Interactive {
    /// <summary>
    /// The marks of the interactive layer per glyph tier. Full uses Cascadia-class glyphs, Basic stays inside the WGL4
    /// repertoire of Consolas and Lucida Console, and ASCII uses printable ASCII only (the focused row is marked with
    /// <c>&gt;</c> and brackets there, since colour may be missing too).
    /// </summary>
    internal sealed class InteractiveGlyphs {
        private static readonly InteractiveGlyphs FullSet = new InteractiveGlyphs(
            focusBar: "▌", checkedBox: "[✓]", uncheckedBox: "[ ]", partialBox: "[–]", radioOn: "●", radioOff: "○",
            expanded: "▾", collapsed: "▸", question: "?", scrollTrack: "│", scrollThumb: "┃", up: "↑", down: "↓",
            breadcrumb: "›", dropdown: "▾", pointer: "›", stepDone: "●", stepCurrent: "●", stepTodo: "○", stepLine: "─",
            separator: "·", keyLeftRight: "←→", keyUpDown: "↑↓", enter: "enter", ellipsis: "…");

        private static readonly InteractiveGlyphs BasicSet = new InteractiveGlyphs(
            focusBar: "▌", checkedBox: "[√]", uncheckedBox: "[ ]", partialBox: "[-]", radioOn: "●", radioOff: "○",
            expanded: "▼", collapsed: "►", question: "?", scrollTrack: "│", scrollThumb: "█", up: "↑", down: "↓",
            breadcrumb: "›", dropdown: "▼", pointer: "›", stepDone: "●", stepCurrent: "●", stepTodo: "○", stepLine: "─",
            separator: "·", keyLeftRight: "←→", keyUpDown: "↑↓", enter: "enter", ellipsis: "…");

        private static readonly InteractiveGlyphs AsciiSet = new InteractiveGlyphs(
            focusBar: ">", checkedBox: "[x]", uncheckedBox: "[ ]", partialBox: "[-]", radioOn: "(*)", radioOff: "( )",
            expanded: "v", collapsed: ">", question: "?", scrollTrack: "|", scrollThumb: "#", up: "^", down: "v",
            breadcrumb: ">", dropdown: "v", pointer: ">", stepDone: "x", stepCurrent: "*", stepTodo: "-", stepLine: "-",
            separator: "|", keyLeftRight: "Left/Right", keyUpDown: "Up/Down", enter: "Enter", ellipsis: "...");

        private InteractiveGlyphs(string focusBar, string checkedBox, string uncheckedBox, string partialBox, string radioOn, string radioOff,
            string expanded, string collapsed, string question, string scrollTrack, string scrollThumb, string up, string down,
            string breadcrumb, string dropdown, string pointer, string stepDone, string stepCurrent, string stepTodo, string stepLine,
            string separator, string keyLeftRight, string keyUpDown, string enter, string ellipsis) {
            FocusBar = focusBar;
            Checked = checkedBox;
            Unchecked = uncheckedBox;
            Partial = partialBox;
            RadioOn = radioOn;
            RadioOff = radioOff;
            Expanded = expanded;
            Collapsed = collapsed;
            Question = question;
            ScrollTrack = scrollTrack;
            ScrollThumb = scrollThumb;
            Up = up;
            Down = down;
            Breadcrumb = breadcrumb;
            Dropdown = dropdown;
            Pointer = pointer;
            StepDone = stepDone;
            StepCurrent = stepCurrent;
            StepTodo = stepTodo;
            StepLine = stepLine;
            Separator = separator;
            KeyLeftRight = keyLeftRight;
            KeyUpDown = keyUpDown;
            Enter = enter;
            Ellipsis = ellipsis;
        }

        public string FocusBar { get; }
        public string Checked { get; }
        public string Unchecked { get; }
        public string Partial { get; }
        public string RadioOn { get; }
        public string RadioOff { get; }
        public string Expanded { get; }
        public string Collapsed { get; }
        public string Question { get; }
        public string ScrollTrack { get; }
        public string ScrollThumb { get; }
        public string Up { get; }
        public string Down { get; }
        public string Breadcrumb { get; }
        public string Dropdown { get; }
        public string Pointer { get; }
        public string StepDone { get; }
        public string StepCurrent { get; }
        public string StepTodo { get; }
        public string StepLine { get; }
        public string Separator { get; }
        public string KeyLeftRight { get; }
        public string KeyUpDown { get; }
        public string Enter { get; }
        public string Ellipsis { get; }

        /// <summary>Whether the focused row is marked with brackets (ASCII, where colour cannot be relied on).</summary>
        public bool BracketFocus => ReferenceEquals(this, AsciiSet);

        public static InteractiveGlyphs For(GlyphTier tier) {
            switch (tier) {
                case GlyphTier.Full:
                    return FullSet;
                case GlyphTier.Basic:
                    return BasicSet;
                default:
                    return AsciiSet;
            }
        }

        public static InteractiveGlyphs For(RenderContext context) => For(context.Glyphs.Tier);
    }
}
