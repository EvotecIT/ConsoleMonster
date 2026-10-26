using System;

namespace ConsoleMonster.Interactive {
    /// <summary>Named grid configuration shared by Ui.DataGrid and New-ConsoleDataGrid.</summary>
    public sealed class DataGridInputOptions : AppInputOptions {
        /// <summary>Rows and columns, including original objects and stable identities.</summary>
        public Grid? Grid { get; set; }
        /// <summary>Natural visible content height.</summary>
        public int Height { get; set; } = 20;
        /// <summary>Whether unbound typing performs prefix search.</summary>
        public bool QuickSearch { get; set; } = true;
        /// <summary>Creates the persistent input after its key and listing are configured.</summary>
        public DataGrid Build() {
            var control = new DataGrid(Key, Grid) { Label = Label ?? Grid?.Title ?? Key, Height = Height, QuickSearch = QuickSearch, Disabled = Disabled, TabIndex = TabIndex, ValidationMessage = ValidationMessage };
            if (OnChange != null) { control.Changed += OnChange; }
            return control;
        }
    }
}
