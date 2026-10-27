using System;
using System.Collections.Generic;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;
using ConsoleMonster.Interactive;

namespace ConsoleMonster {
    /// <summary>Behavioral definitions and persistent controls. Build materializes immutable identity and kind through the shared factories.</summary>
    public static partial class Ui {
        /// <summary>A retained tab-page definition.</summary>
        public static TabOptions Tab() => new TabOptions();
        /// <summary>A composed modal form definition with named field results.</summary>
        public static FormDialogOptions Dialog() => new FormDialogOptions();
        /// <summary>An embeddable grid definition; configure Key and Grid, then Build.</summary>
        public static DataGridInputOptions DataGrid() => new DataGridInputOptions();
        /// <summary>A budgeted, lazy object-tree input definition.</summary>
        public static TreeSelectInputOptions TreeSelect() => new TreeSelectInputOptions();
        /// <summary>A persistent civil-date or range picker definition.</summary>
        public static DatePickerInputOptions DatePicker() => new DatePickerInputOptions();
        /// <summary>A date or inclusive date-range question.</summary>
        public static DatePrompt DatePrompt() => new DatePrompt(string.Empty);
        /// <summary>Empty sequential form.</summary>
        /// <returns>The empty sequential form.</returns>
        public static Form Form() => new Form(string.Empty);

        /// <summary>Empty navigation menu.</summary>
        /// <returns>The empty navigation menu.</returns>
        public static Menu Menu() => new Menu(string.Empty);

        /// <summary>Empty menu action definition.</summary>
        /// <returns>The empty menu action definition.</returns>
        public static MenuItem MenuItem() => new MenuItem(string.Empty);

        /// <summary>Menu action result.</summary>
        /// <returns>The menu action result.</returns>
        public static MenuRunResult MenuRunResult() => new MenuRunResult(string.Empty);

        /// <summary>Selection control definition; Multiple selects the renderer at Build.</summary>
        /// <returns>The selection control definition; Multiple selects the renderer at Build.</returns>
        public static SelectInputOptions Select() => new SelectInputOptions();

        /// <summary>Text control definition; Multiline selects the renderer at Build.</summary>
        /// <returns>The text control definition; Multiline selects the renderer at Build.</returns>
        public static TextInputOptions TextInput() => new TextInputOptions();

        /// <summary>Button definition; Build delegates to AppInput.Button.</summary>
        /// <returns>The button definition; Build delegates to AppInput.Button.</returns>
        public static ButtonInputOptions Button() => new ButtonInputOptions();

        /// <summary>Boolean control definition; Build delegates to AppInput.Toggle.</summary>
        /// <returns>The boolean control definition; Build delegates to AppInput.Toggle.</returns>
        public static ToggleInputOptions Toggle() => new ToggleInputOptions();

        /// <summary>State-dependent display definition.</summary>
        /// <returns>The state-dependent display definition.</returns>
        public static BindingOptions Binding() => new BindingOptions();

        /// <summary>Asynchronous action definition.</summary>
        /// <returns>The asynchronous action definition.</returns>
        public static CommandOptions Command() => new CommandOptions();

        /// <summary>Interactive record grid; configure Title and rows in the chain.</summary>
        /// <returns>The interactive record grid; configure Title and rows in the chain.</returns>
        public static ConsoleMonster.Interactive.Grid Grid() => new ConsoleMonster.Interactive.Grid(string.Empty);

        /// <summary>Text question; configure Title and validation in the chain.</summary>
        /// <returns>The text question; configure Title and validation in the chain.</returns>
        public static TextPrompt TextPrompt() => new TextPrompt(string.Empty);

        /// <summary>Secret question.</summary>
        /// <returns>The secret question.</returns>
        public static SecretPrompt SecretPrompt() => new SecretPrompt(string.Empty);

        /// <summary>Numeric question.</summary>
        /// <returns>The numeric question.</returns>
        public static NumberPrompt NumberPrompt() => new NumberPrompt(string.Empty);

        /// <summary>Path question.</summary>
        /// <returns>The path question.</returns>
        public static PathPrompt PathPrompt() => new PathPrompt(string.Empty);

        /// <summary>Confirmation question.</summary>
        /// <returns>The confirmation question.</returns>
        public static ConfirmPrompt ConfirmPrompt() => new ConfirmPrompt(string.Empty);

        /// <summary>Typed confirmation question; configure Expected in the chain.</summary>
        /// <returns>The typed confirmation question; configure Expected in the chain.</returns>
        public static TypedConfirmPrompt TypedConfirmPrompt() => new TypedConfirmPrompt(string.Empty, string.Empty);

    }
}
