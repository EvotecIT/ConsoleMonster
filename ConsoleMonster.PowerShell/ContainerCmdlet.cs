using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Layout;

namespace ConsoleMonster.PowerShell {
    /// <summary>Collects one container's pipeline or script-block content before building it.</summary>
    public abstract class ContainerCmdlet : PSCmdlet {
        private readonly List<object> _content = new List<object>();
        /// <summary>The content supplied as widgets, ordinary objects or a script block.</summary>
        public abstract object? Content { get; set; }

        /// <inheritdoc />
        protected override void ProcessRecord() {
            object? value = PSText.Unwrap(Content);
            if (value is ScriptBlock script) { foreach (PSObject item in script.Invoke()) { if (item != null) { _content.Add(item); } } }
            else if (value != null) { _content.Add(Content!); }
        }

        /// <inheritdoc />
        protected override void EndProcessing() => WriteObject(Build(PSWidgets.CollectAny(_content, this)));

        /// <summary>Builds one container from the collected children; core widgets own layout and rendering.</summary>
        /// <param name="children">Widgets in pipeline/declaration order.</param>
        /// <returns>The configured container.</returns>
        protected abstract IWidget Build(List<IWidget> children);
    }
}
