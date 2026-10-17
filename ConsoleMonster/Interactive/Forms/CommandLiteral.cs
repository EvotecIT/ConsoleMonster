using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using ConsoleMonster.Layout;

namespace ConsoleMonster.Interactive {
    /// <summary>Text written into a command line as is (no quoting), such as a secret placeholder.</summary>
    internal sealed class CommandLiteral {
        public CommandLiteral(string text) {
            Text = text;
        }

        public string Text { get; }

        public override string ToString() => Text;
    }
}
