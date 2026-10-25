using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Interactive;

namespace ConsoleMonster.PowerShell {
    /// <summary>Choice inputs reuse the prompt choice converter, preserving existing Choice objects and PowerShell values.</summary>
    public abstract class AppChoiceCmdlet : AppInputCmdlet {
        /// <summary><para type="description">Choices as objects or existing Choice values. Objects themselves become the selected values.</para></summary>
        [Parameter(Mandatory = true)]
        [AllowEmptyCollection]
        public object[] Choices { get; set; } = new object[0];

        /// <summary><para type="description">Display property name or scriptblock for object choices, as in Read-ConsoleSelect.</para></summary>
        [Parameter]
        public object? Display { get; set; }

        /// <summary>Builds choices with the canonical prompt converter; does not clone existing choices.</summary>
        /// <returns>Choices retaining their original values.</returns>
        protected List<Choice> BuildChoices() => PSInteractive.Choices(Choices, Display, null, null, null, null, null, null);
    }
}
