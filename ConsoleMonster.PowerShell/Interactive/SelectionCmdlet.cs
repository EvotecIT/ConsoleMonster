using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Interactive;

namespace ConsoleMonster.PowerShell {
    /// <summary>The parameters of the selection prompts: the choices (from the pipeline or -Choices) and how to show them.</summary>
    public abstract class SelectionCmdlet : PromptCmdlet {
        private readonly List<object?> _items = new List<object?>();

        /// <summary><para type="description">The choices: any objects (strings, services, records). The chosen objects themselves are returned.</para></summary>
        [Parameter(Position = 1, ValueFromPipeline = true)]

        [AllowNull]
        [AllowEmptyCollection]
        public object?[]? Choices { get; set; }

        /// <summary><para type="description">What to show for a choice: a property name or a scriptblock ($_ is the choice). Default: the choice as text.</para></summary>
        [Parameter]

        public object? Display { get; set; }

        /// <summary><para type="description">The group heading of a choice: a property name or a scriptblock.</para></summary>
        [Parameter]
        public object? GroupBy { get; set; }

        /// <summary><para type="description">A sentence for the focused choice: a property name or a scriptblock.</para></summary>
        [Parameter]

        public object? Describe { get; set; }

        /// <summary><para type="description">Short right-aligned text per choice: a property name or a scriptblock.</para></summary>
        [Parameter]
        public object? Badge { get; set; }

        /// <summary><para type="description">A key that picks a choice directly: a property name or a scriptblock returning a letter.</para></summary>
        [Parameter]
        public object? Hotkey { get; set; }

        /// <summary><para type="description">Returns why a choice is unavailable ($_ is the choice), or nothing when it is available.</para></summary>
        [Parameter]
        public ScriptBlock? DisabledReason { get; set; }

        /// <summary><para type="description">Content for the preview pane ($_ is the focused choice): text, rich text, a widget, or an object shown as a list.</para></summary>
        [Parameter]
        public ScriptBlock? Preview { get; set; }

        /// <summary><para type="description">The preferred number of visible rows.</para></summary>
        [Parameter]
        public int PageSize { get; set; }

        /// <summary>The collected choices.</summary>
        protected List<object?> Items => _items;

        /// <inheritdoc />
        protected override void ProcessRecord() {
            if (Choices == null) {
                return;
            }
            foreach (object? item in Choices) {
                _items.Add(item);
            }
        }

        /// <summary>Builds the choices.</summary>
        protected List<Choice> BuildChoices(object? selected) {
            return PSInteractive.Choices(_items, Display, GroupBy, Describe, Badge, Hotkey, DisabledReason, selected);
        }
    }
}
