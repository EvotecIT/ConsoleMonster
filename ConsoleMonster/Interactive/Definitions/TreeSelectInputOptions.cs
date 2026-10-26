using System;

namespace ConsoleMonster.Interactive {
    /// <summary>Named, lazy object-tree construction shared by Ui.TreeSelect and PowerShell.</summary>
    public sealed class TreeSelectInputOptions : AppInputOptions {
        /// <summary>The explorer model with member adapters and expansion limits.</summary>
        public ObjectExplorer? Explorer { get; set; }
        /// <summary>Natural visible content rows.</summary>
        public int Height { get; set; } = 20;
        /// <summary>Constructs a persistent tree input.</summary>
        public TreeSelect Build() {
            var control = new TreeSelect(Key, Explorer ?? throw new ArgumentException("A tree needs Explorer.")) { Height = Height, Label = Label ?? Explorer.Title, Disabled = Disabled, TabIndex = TabIndex, ValidationMessage = ValidationMessage };
            if (OnChange != null) { control.Changed += OnChange; }
            return control;
        }
    }
}
