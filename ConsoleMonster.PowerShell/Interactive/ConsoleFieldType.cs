using System;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Interactive;

namespace ConsoleMonster.PowerShell {
    /// <summary>The kind of field New-ConsoleField creates.</summary>
    public enum ConsoleFieldType {
        /// <summary>Text: one line, or several with -Multiline, or a secret with -AsSecret.</summary>
        Text,

        /// <summary>A number.</summary>
        Number,

        /// <summary>An on/off switch.</summary>
        Toggle,

        /// <summary>One value from a list, or several with -Multiple.</summary>
        Select,

        /// <summary>A file or folder path.</summary>
        Path
    }
}
