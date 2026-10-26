using System.Collections.Generic;
using ConsoleMonster.Data;
using ConsoleMonster.Layout;
using ConsoleMonster.Values;

namespace ConsoleMonster.Views {
    /// <summary>Options for <see cref="ConsoleView"/>.</summary>
    public sealed class ConsoleViewOptions {
        /// <summary>A view to use instead of the automatic choice.</summary>
        public ConsoleViewKind? View { get; set; }

        /// <summary>The member that holds each object's children, for a tree (found automatically when null).</summary>
        public string? ChildrenProperty { get; set; }

        /// <summary>Member sources tried before reflection (for example PowerShell objects).</summary>
        public IList<IMemberSource> Sources { get; } = new List<IMemberSource>();

        /// <summary>The members to show as columns, or null for all public members.</summary>
        public IList<string>? Property { get; set; }

        /// <summary>A title for the view (markup), or null.</summary>
        public string? Title { get; set; }

        /// <summary>The deepest level a tree or inspector shows (default 3).</summary>
        public int Depth { get; set; } = 3;
        /// <summary>The code language, or Auto.</summary>
        public string Language { get; set; } = "Auto";

        /// <summary>The source path, used for language detection and the default code title.</summary>
        public string? Path { get; set; }

        /// <summary>Shows code line numbers.</summary>
        public bool LineNumbers { get; set; }

        /// <summary>The first displayed code line number.</summary>
        public int FirstLine { get; set; } = 1;

        /// <summary>The maximum displayed code lines.</summary>
        public int MaxLines { get; set; } = 2000;

        /// <summary>Code line numbers or ranges to highlight.</summary>
        public string[]? Highlight { get; set; }

        /// <summary>Wraps code lines.</summary>
        public bool Wrap { get; set; }

        /// <summary>Suppresses the source file title.</summary>
        public bool NoTitle { get; set; }

        /// <summary>Includes empty record fields.</summary>
        public bool ShowEmpty { get; set; }

        /// <summary>The rule title alignment.</summary>
        public HorizontalAlignment Alignment { get; set; }

        /// <summary>Includes framework stack frames.</summary>
        public bool ShowFrameworkFrames { get; set; }

        /// <summary>Maximum application stack frames.</summary>
        public int MaxFrames { get; set; } = 8;

        /// <summary>Includes stack frames.</summary>
        public bool ShowStack { get; set; } = true;

        /// <summary>Maximum inner errors.</summary>
        public int MaxInner { get; set; } = 4;

        /// <summary>Includes source excerpts.</summary>
        public bool ShowSource { get; set; } = true;

        /// <summary>Uses the compact error layout.</summary>
        public bool Compact { get; set; }

        /// <summary>The folder used to shorten source paths.</summary>
        public string? BasePath { get; set; }

        /// <summary>Host-specific advice for requesting full error details.</summary>
        public string? DetailsHint { get; set; }

        /// <summary>An explicitly supplied add-on view provider.</summary>
        public IConsoleViewProvider? Provider { get; set; }

        /// <summary>Member adapters and host shaping policies for structured values.</summary>
        public ShapeOptions? Shape { get; set; }

    }
}
