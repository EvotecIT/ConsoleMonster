using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using ConsoleMonster.Canvas;
using ConsoleMonster.Input;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Interactive {
    /// <summary>What a path prompt accepts.</summary>
    public enum PathKind {
        /// <summary>A file or a folder.</summary>
        Any,

        /// <summary>A file.</summary>
        File,

        /// <summary>A folder.</summary>
        Folder
    }
}
