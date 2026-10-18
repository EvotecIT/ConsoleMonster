using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ConsoleMonster.Interactive {
    /// <summary>How the equivalent command line is written.</summary>
    public enum CommandStyle {
        /// <summary>PowerShell: <c>Invoke-Thing -Name 'a b' -Server a,b -Force</c>.</summary>
        PowerShell,

        /// <summary>POSIX-style CLIs: <c>thing --name 'a b' --server a,b --force</c>.</summary>
        Posix,

        /// <summary>Windows-style CLIs: <c>thing /name:"a b" /server:a,b /force</c>.</summary>
        Windows
    }
}
