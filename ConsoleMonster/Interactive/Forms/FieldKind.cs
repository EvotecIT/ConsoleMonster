using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

namespace ConsoleMonster.Interactive {
    /// <summary>The kind of a form field.</summary>
    public enum FieldKind {
        /// <summary>A line of text.</summary>
        Text,

        /// <summary>A secret.</summary>
        Secret,

        /// <summary>A number.</summary>
        Number,

        /// <summary>An on/off switch.</summary>
        Toggle,

        /// <summary>One value from a list.</summary>
        Choice,

        /// <summary>Several values from a list.</summary>
        MultiChoice,

        /// <summary>A file or folder path.</summary>
        Path,

        /// <summary>Several lines of text.</summary>
        TextArea
    }
}
