namespace ConsoleMonster.Interactive {
    /// <summary>Named scalar/range date input construction shared by Ui.DatePicker and PowerShell.</summary>
    public sealed class DatePickerInputOptions : AppInputOptions {
        /// <summary>Whether Value is an inclusive DateRange.</summary>
        public bool Range { get; set; }
        /// <summary>The initial DateTime or DateRange. Omission leaves selection uncommitted.</summary>
        public object? Value { get; set; }
        /// <summary>Shared date bounds, culture and marks.</summary>
        public DateSelectionOptions Selection { get; set; } = new DateSelectionOptions();
        /// <summary>Constructs the persistent picker.</summary>
        public DatePicker Build() {
            DatePicker control = Selection.CreatePicker(Key, Range, Value);
            control.Label = Label ?? Key; control.Disabled = Disabled; control.TabIndex = TabIndex; control.ValidationMessage = ValidationMessage;
            if (OnChange != null) { control.Changed += OnChange; }
            return control;
        }
    }
}
