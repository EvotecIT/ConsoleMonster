using System;

namespace ConsoleMonster.Widgets {
    /// <summary>Reference and completeness markers for a tree node.</summary>
    [Flags]
    public enum TreeReferenceKind {
        /// <summary>An ordinary node.</summary>
        None = 0,
        /// <summary>A reference to an ancestor; descendants are not followed.</summary>
        Cycle = 1,
        /// <summary>A reference to an identity already shown; descendants are not followed.</summary>
        Repeat = 2,
        /// <summary>A deliberately collapsed subtree; descendants are not followed.</summary>
        Collapsed = 4,
        /// <summary>The source reports that this subtree is incomplete.</summary>
        Incomplete = 8
    }
}
