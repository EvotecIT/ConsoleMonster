using System.Management.Automation;

namespace ConsoleMonster.PowerShell {
    /// <summary>Typed hierarchy binding for PowerShell records; traversal and rendering stay in the core hierarchy owner.</summary>
    public sealed class ConsoleTreeOptions {
        /// <summary>The hierarchy root title, including rich text.</summary>
        public object? Title { get; set; }
        /// <summary>The record field containing child records.</summary>
        public string? ChildrenProperty { get; set; }
        /// <summary>An owning-runspace child selector.</summary>
        public ScriptBlock? Children { get; set; }
        /// <summary>The field used as each node label.</summary>
        public string? LabelProperty { get; set; }
        /// <summary>The field used as each node status.</summary>
        public string? StatusProperty { get; set; }
        /// <summary>The field used as each node detail.</summary>
        public string? DetailProperty { get; set; }
        /// <summary>Maximum hierarchy depth.</summary>
        public int Depth { get; set; } = 3;
        /// <summary>Maximum children per collection.</summary>
        public int MaxItems { get; set; } = 100;
        /// <summary>Maximum total hierarchy nodes.</summary>
        public int MaxNodes { get; set; } = 1000;
    }
}
