using System;
using ConsoleMonster.Layout;

namespace ConsoleMonster.Interactive {
    /// <summary>Configuration for a display rebuilt from application state; its identity is fixed by Build.</summary>
    public sealed class BindingOptions {
        /// <summary>The optional live-region identity.</summary>
        public string? WidgetId { get; set; }
        /// <summary>Creates display content from the current state; stable input controls stay in the outer layout.</summary>
        public Func<AppState, IWidget>? Content { get; set; }
    }
}
