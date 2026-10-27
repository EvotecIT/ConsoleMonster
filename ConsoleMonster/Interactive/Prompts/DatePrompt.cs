using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using ConsoleMonster.Dates;
using ConsoleMonster.Input;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Interactive {
    /// <summary>Asks for one civil DateTime or, with Range, one inclusive DateRange. Cancellation follows the shared prompt policy.</summary>
    public sealed class DatePrompt : Prompt<object> {
        /// <summary>Creates a date question.</summary>
        public DatePrompt(string title) : base(title) { }
        /// <summary>Returns one DateRange instead of a DateTime.</summary>
        public bool Range { get; set; }
        /// <summary>Date bounds, unavailable dates, culture and marked-day data.</summary>
        public DateSelectionOptions Selection { get; set; } = new DateSelectionOptions();
        /// <inheritdoc />
        public override object Show(InteractiveOptions? options = null, CancellationToken cancellationToken = default) {
            if (HasDefault) { Default = Selection.CreatePicker("Date", Range, Default).Value!; }
            return base.Show(options, cancellationToken);
        }
        /// <inheritdoc />
        public override string Describe(object value) => value is DateRange range ? range.ToString() : ((DateTime)value).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        private bool Parse(string text, out DateTime date) {
            bool valid = DateTime.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date)
                || DateTime.TryParse(text, Selection.Culture ?? CultureInfo.CurrentCulture, DateTimeStyles.None, out date);
            date = CalendarDate.Normalize(date); return valid;
        }
        internal override object AskLines(LinePrompter lines, CancellationToken cancellationToken) {
            DatePicker checker = Selection.CreatePicker("Date", Range, HasDefault ? Default : null);
            DateTime Read(string label, DateTime? initial, DateTime? start = null) {
                DateTime answer = default;
                string? Check(string text) {
                    if (!Parse(text, out answer)) { return "Enter a date as yyyy-MM-dd or in the displayed culture."; }
                    if (start.HasValue && answer < start.Value) { return "The range end precedes its start."; }
                    return checker.Check(start.HasValue ? (object)new DateRange(start.Value, answer) : answer);
                }
                string text = lines.Text(label + " [yyyy-MM-dd]", Description, initial?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), Check, false, cancellationToken);
                Check(text); return answer;
            }
            DateTime? first = Default is DateRange dates ? dates.Start : HasDefault && Default is DateTime date ? date : (DateTime?)null;
            DateTime start = Read(Range ? Title + " — start" : Title, first);
            if (!Range) { return start; }
            DateTime end = Read(Title + " — end", Default is DateRange range ? range.End : first, start);
            return new DateRange(start, end);
        }
        internal override PromptView<object> CreateView(PromptLabels labels) => new DateView(this, labels);
        private sealed class DateView : PromptView<object> {
            private readonly DatePicker _picker;
            internal DateView(DatePrompt prompt, PromptLabels labels) : base(prompt, labels) {
                _picker = prompt.Selection.CreatePicker("Date", prompt.Range, prompt.HasDefault ? prompt.Default : null);
                _picker.Label = prompt.Title; _picker.Focused = true;
            }
            protected override IWidget Build(ViewFrame frame) => new Rows().Add(Header("yyyy-MM-dd"), LayoutSize.Fixed(1)).Add(_picker).Add(Footer(frame), LayoutSize.Fixed(1));
            public override void AddBindings(List<KeyBinding> bindings, ViewFrame frame) {
                bindings.Add(new KeyBinding("select", () => {
                    _picker.HandleInput(InputEvent.Press(ConsoleKey.Enter), frame.Context);
                    if (_picker.ValidationMessage == null && _picker.Value != null) { Accept(_picker.Value); }
                }, new KeyChord(ConsoleKey.Enter)));
                bindings.Add(new KeyBinding("cancel", () => { if (!_picker.HandleInput(InputEvent.Press(ConsoleKey.Escape), frame.Context)) { Decline(); } }, new KeyChord(ConsoleKey.Escape)));
            }
            protected override bool HandleUnbound(InputEvent input) => LastFrame != null && _picker.HandleInput(input, LastFrame.Context);
        }
    }
}
