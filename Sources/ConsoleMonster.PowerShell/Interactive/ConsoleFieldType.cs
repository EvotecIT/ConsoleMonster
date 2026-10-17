using System;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Interactive;

namespace ConsoleMonster.PowerShell {
    /// <summary>The kind of field New-ConsoleField creates.</summary>
    public enum ConsoleFieldType {
        /// <summary>A line of text.</summary>
        Text,

        /// <summary>A secret.</summary>
        Secret,

        /// <summary>A number.</summary>
        Number,

        /// <summary>An on/off switch.</summary>
        Toggle,

        /// <summary>One value from a list.</summary>
        Select,

        /// <summary>Several values from a list.</summary>
        MultiSelect,

        /// <summary>A file or folder path.</summary>
        Path
    }
}
