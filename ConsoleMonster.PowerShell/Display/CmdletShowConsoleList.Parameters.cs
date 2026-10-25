using System;
using System.Globalization;
using System.Management.Automation;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    public sealed partial class CmdletShowConsoleList {
        /// <summary><para type="description">The object, widget or text to display.</para></summary>
        [Parameter(Position = 0, ValueFromPipeline = true, ParameterSetName = "Objects")]
        public override PSObject? InputObject { get; set; }

        /// <summary><para type="description">A widget, text or script block producing display content.</para></summary>
        [Parameter(Mandatory = true, ParameterSetName = "Content")]
        public override object? Content { get; set; }

        /// <summary><para type="description">Property names, patterns or calculated column records.</para></summary>
        [Parameter]
        public override object[]? Property { get; set; }

        /// <summary><para type="description">Groups rows or records by a property or script block.</para></summary>
        [Parameter]
        public override object? GroupBy { get; set; }

        /// <summary><para type="description">The view title, including markup.</para></summary>
        [Parameter]
        public override object? Title { get; set; }

        /// <summary><para type="description">Layout width; zero resolves the terminal with a 100-cell fallback.</para></summary>
        [Parameter, ValidateRange(0, 1000)]
        public override int Width { get; set; }

        /// <summary><para type="description">The culture used for values.</para></summary>
        [Parameter]
        public override CultureInfo? Culture { get; set; }

        /// <summary><para type="description">Returns plain text instead of displaying it.</para></summary>
        [Parameter]
        public override SwitchParameter AsString { get; set; }

        /// <summary><para type="description">Returns the constructed widget.</para></summary>
        [Parameter]
        public override SwitchParameter AsWidget { get; set; }

        /// <summary><para type="description">Returns the original input after display.</para></summary>
        [Parameter]
        public override SwitchParameter PassThru { get; set; }

        /// <summary><para type="description">Keeps empty fields in record views.</para></summary>
        [Parameter]
        public override SwitchParameter ShowEmpty { get; set; }

        /// <summary><para type="description">Maximum nesting depth; default three.</para></summary>
        [Parameter, ValidateRange(0, int.MaxValue)]
        public override int Depth { get; set; } = 3;

    }
}
