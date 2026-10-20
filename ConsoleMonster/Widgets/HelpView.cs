using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Help;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    /// <summary>
    /// A command's help (<see cref="HelpTopic"/>): the name and synopsis, the syntax of each parameter set with the
    /// parameters, types and optional parts told apart, the description, a parameter table (type, required, position,
    /// pipeline input, default, description), the examples with highlighted code, inputs, outputs, notes and related links.
    /// Everything is printed as it grows, so it reads well through a pager. Show-ConsoleHelp is the PowerShell face.
    /// </summary>
    public sealed class HelpView : IWidget {
        private Rows? _built;
        private int _builtWidth = -1;

        /// <summary>Creates the view.</summary>
        /// <param name="topic">The help.</param>
        public HelpView(HelpTopic topic) {
            Topic = topic ?? throw new ArgumentNullException(nameof(topic));
        }

        /// <summary>The help shown.</summary>
        public HelpTopic Topic { get; }

        /// <summary>Whether the examples are shown (default true).</summary>
        public bool ShowExamples { get; set; } = true;

        /// <summary>Whether the parameter table is shown (default true).</summary>
        public bool ShowParameters { get; set; } = true;

        // One column is kept free: a line as wide as the console makes some hosts (Windows PowerShell's console host)
        // wrap and add an empty line after it.
        private static int Usable(int width) => width > 20 ? width - 1 : width;

        /// <inheritdoc />
        public Size Measure(Constraints constraints, RenderContext context) {
            int width = Usable(constraints.MaxWidth);
            Size size = Build(width, context).Measure(new Constraints(width, constraints.MaxHeight), context);
            return new Size(constraints.MaxWidth, size.Height);
        }

        /// <inheritdoc />
        public void Render(Region region, RenderContext context) {
            if (region.Width > 0 && region.Height > 0) {
                int width = Usable(region.Width);
                Build(width, context).Render(region.Sub(0, 0, width, region.Height), context);
            }
        }

        private Rows Build(int width, RenderContext context) {
            if (_built != null && _builtWidth == width) {
                return _built;
            }
            Rows rows = new Rows();
            Style title = context.Style(ThemeToken.Title).With(TextDecoration.Bold);
            List<TextSpan> head = new List<TextSpan> { new TextSpan(Topic.Name, title) };
            rows.Add(new LineBlock(new[] { new StyledLine(head) }, 0, 0));
            if (HasSynopsis()) {
                rows.Add(Paragraph(Topic.Synopsis!, context, 0));
            }
            if (!string.IsNullOrEmpty(Topic.Notice)) {
                rows.Add(new LineBlock(new[] { StyledLine.From(Topic.Notice!, context.Style(ThemeToken.Muted)) }, 0, 0));
            }
            if (Topic.Syntax.Count > 0) {
                Section(rows, "Syntax", context);
                for (int i = 0; i < Topic.Syntax.Count; i++) {
                    if (i > 0) {
                        rows.Add(Blank());
                    }
                    rows.Add(SyntaxBlock(Topic.Syntax[i], context));
                }
            }
            if (Topic.Description.Count > 0) {
                Section(rows, "Description", context);
                ParagraphsInto(rows, Topic.Description, context);
            }
            if (ShowParameters && (Topic.Parameters.Count > 0 || Topic.CommonParameters)) {
                Section(rows, "Parameters", context);
                if (Topic.Parameters.Count > 0) {
                    rows.Add(new Padding(ParameterTable(), 2, 0, 0, 0));
                }
                if (Topic.CommonParameters) {
                    rows.Add(new LineBlock(new[] { StyledLine.From("Common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -OutVariable, -Verbose, -WarningAction and the others (about_CommonParameters).", context.Style(ThemeToken.Muted)) }, 2, 2));
                }
            }
            if (ShowExamples && Topic.Examples.Count > 0) {
                Section(rows, "Examples", context);
                for (int i = 0; i < Topic.Examples.Count; i++) {
                    HelpExample example = Topic.Examples[i];
                    if (i > 0) {
                        rows.Add(Blank());
                    }
                    string number = (i + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
                    string caption = string.IsNullOrEmpty(example.Title) ? "Example " + number : number + ". " + example.Title;
                    rows.Add(new LineBlock(new[] { StyledLine.From(caption, context.Style(ThemeToken.Text).With(TextDecoration.Bold)) }, 2, 5));
                    if (example.Code.Length > 0) {
                        rows.Add(new Padding(CodeView.For(example.Code, "powershell"), 5, 0, 0, 0));
                    }
                    if (!string.IsNullOrEmpty(example.Remarks)) {
                        rows.Add(Paragraph(example.Remarks!, context, 5));
                    }
                }
            }
            TypesInto(rows, "Inputs", Topic.Inputs, context);
            TypesInto(rows, "Outputs", Topic.Outputs, context);
            if (Topic.Notes.Count > 0) {
                Section(rows, "Notes", context);
                ParagraphsInto(rows, Topic.Notes, context);
            }
            if (Topic.Links.Count > 0) {
                Section(rows, "Related links", context);
                List<StyledLine> links = new List<StyledLine>();
                foreach (HelpLink link in Topic.Links) {
                    List<TextSpan> spans = new List<TextSpan>();
                    if (!string.IsNullOrEmpty(link.Text)) {
                        spans.Add(new TextSpan(link.Text!, context.Style(ThemeToken.Text)));
                    }
                    if (!string.IsNullOrEmpty(link.Uri)) {
                        spans.Add(new TextSpan((spans.Count > 0 ? "  " : string.Empty) + link.Uri, context.Style(ThemeToken.Accent).WithLink(link.Uri)));
                    }
                    if (spans.Count > 0) {
                        links.Add(new StyledLine(spans));
                    }
                }
                rows.Add(new LineBlock(links, 2, 4));
            }
            _built = rows;
            _builtWidth = width;
            return rows;
        }

        // Without a help file PowerShell puts the syntax in the synopsis; the syntax section shows it already.
        private bool HasSynopsis() {
            string synopsis = (Topic.Synopsis ?? string.Empty).Trim();
            if (synopsis.Length == 0 || string.Equals(synopsis, Topic.Name, StringComparison.OrdinalIgnoreCase)) {
                return false;
            }
            return !(Topic.Syntax.Count > 0 && synopsis.StartsWith(Topic.Name + " ", StringComparison.OrdinalIgnoreCase)
                && (synopsis.IndexOf("[-", StringComparison.Ordinal) >= 0 || synopsis.IndexOf(" -", StringComparison.Ordinal) >= 0 || synopsis.IndexOf("<CommonParameters>", StringComparison.Ordinal) >= 0));
        }

        private static IWidget Blank() => new LineBlock(new[] { StyledLine.Empty }, 0, 0);

        private static void Section(Rows rows, string name, RenderContext context) {
            rows.Add(Blank());
            rows.Add(new LineBlock(new[] { StyledLine.From(name, context.Style(ThemeToken.Accent).With(TextDecoration.Bold)) }, 0, 0));
        }

        private static IWidget Paragraph(string text, RenderContext context, int indent) {
            List<StyledLine> lines = new List<StyledLine>();
            foreach (string line in text.Replace("\r\n", "\n").Split('\n')) {
                lines.Add(StyledLine.From(line.TrimEnd(), context.Style(ThemeToken.Text)));
            }
            return new LineBlock(lines, indent, indent);
        }

        private static void ParagraphsInto(Rows rows, IList<string> paragraphs, RenderContext context) {
            for (int i = 0; i < paragraphs.Count; i++) {
                if (i > 0) {
                    rows.Add(Blank());
                }
                rows.Add(Paragraph(paragraphs[i], context, 2));
            }
        }

        private static void TypesInto(Rows rows, string name, IList<string> types, RenderContext context) {
            if (types.Count == 0) {
                return;
            }
            Section(rows, name, context);
            List<StyledLine> lines = new List<StyledLine>();
            foreach (string entry in types) {
                int tab = entry.IndexOf('\t');
                string type = tab >= 0 ? entry.Substring(0, tab) : entry;
                string note = tab >= 0 ? entry.Substring(tab + 1).Trim() : string.Empty;
                List<TextSpan> spans = new List<TextSpan> { new TextSpan(type.Trim(), context.Style(ThemeToken.Accent2)) };
                if (note.Length > 0) {
                    spans.Add(new TextSpan("  " + note, context.Style(ThemeToken.Muted)));
                }
                lines.Add(new StyledLine(spans));
            }
            rows.Add(new LineBlock(lines, 2, 4));
        }

        // Command [[-Path] <string[]>] [-Filter <string>] -Name <string> [-Force] [<CommonParameters>]: the command, then
        // each parameter as a unit that wraps with a hanging indent under the first parameter.
        private static IWidget SyntaxBlock(HelpSyntax syntax, RenderContext context) {
            Style command = context.Style(ThemeToken.Title).With(TextDecoration.Bold);
            Style name = context.Style(ThemeToken.Accent2);
            Style type = context.Style(ThemeToken.Muted);
            Style bracket = context.Style(ThemeToken.Subtle);
            List<List<TextSpan>> units = new List<List<TextSpan>>();
            foreach (HelpSyntaxParameter parameter in syntax.Parameters) {
                List<TextSpan> unit = new List<TextSpan>();
                bool optional = !parameter.Required;
                bool hasValue = !string.IsNullOrEmpty(parameter.Type);
                if (optional) {
                    unit.Add(new TextSpan("[", bracket));
                }
                if (parameter.Positional && hasValue) {
                    unit.Add(new TextSpan("[", bracket));
                    unit.Add(new TextSpan("-" + parameter.Name, name));
                    unit.Add(new TextSpan("]", bracket));
                } else {
                    unit.Add(new TextSpan("-" + parameter.Name, name));
                }
                if (hasValue) {
                    unit.Add(new TextSpan(" <" + parameter.Type + ">", type));
                }
                if (optional) {
                    unit.Add(new TextSpan("]", bracket));
                }
                units.Add(unit);
            }
            if (syntax.CommonParameters) {
                units.Add(new List<TextSpan> { new TextSpan("[<CommonParameters>]", bracket) });
            }
            return new SyntaxLines(new TextSpan(syntax.Command, command), units);
        }

        private Table ParameterTable() {
            // Always a grid, also for a single parameter.
            Table table = new Table { RecordViewRows = 0 };
            table.AddColumn(new TableColumn("Parameter") { IsKey = true });
            table.AddColumn(new TableColumn("Type"));
            table.AddColumn(new TableColumn("Required"));
            table.AddColumn(new TableColumn("Position"));
            table.AddColumn(new TableColumn("Pipeline"));
            // Columns nobody fills (help generated without a help file has no defaults or descriptions) are left out.
            bool defaults = false;
            bool descriptions = false;
            foreach (HelpParameter parameter in Topic.Parameters) {
                defaults |= !string.IsNullOrEmpty(parameter.Default);
                descriptions |= !string.IsNullOrEmpty(parameter.Description);
            }
            if (defaults) {
                table.AddColumn(new TableColumn("Default"));
            }
            if (descriptions) {
                table.AddColumn(new TableColumn("Description") { MaxLines = TableColumn.Unlimited });
            }
            foreach (HelpParameter parameter in Topic.Parameters) {
                string label = "[accent2]-" + Text.Markup.Escape(parameter.Name) + "[/]";
                if (parameter.Aliases.Count > 0) {
                    label += "\n[muted]" + (parameter.Aliases.Count == 1 ? "alias " : "aliases ") + Text.Markup.Escape(string.Join(", ", parameter.Aliases)) + "[/]";
                }
                List<TableCell> cells = new List<TableCell> {
                    label,
                    "[muted]" + Text.Markup.Escape(parameter.Type ?? string.Empty) + "[/]",
                    parameter.Required ? "yes" : "[muted]no[/]",
                    parameter.Position == null ? "[muted]named[/]" : Text.Markup.Escape(parameter.Position),
                    parameter.Pipeline == null ? "[muted]no[/]" : Text.Markup.Escape(parameter.Pipeline)
                };
                if (defaults) {
                    cells.Add(Text.Markup.Escape(parameter.Default ?? string.Empty));
                }
                if (descriptions) {
                    cells.Add(Text.Markup.Escape(parameter.Description ?? string.Empty));
                }
                table.AddRow(cells.ToArray());
            }
            return table;
        }

        // Styled lines wrapped at the width with an indent for the first line and another for the rest.
        private sealed class LineBlock : IWidget {
            private readonly IReadOnlyList<StyledLine> _lines;
            private readonly int _first;
            private readonly int _rest;

            public LineBlock(IReadOnlyList<StyledLine> lines, int first, int rest) {
                _lines = lines;
                _first = first;
                _rest = rest;
            }

            private List<(int Indent, StyledLine Line)> Layout(int width) {
                List<(int, StyledLine)> result = new List<(int, StyledLine)>();
                foreach (StyledLine line in _lines) {
                    if (line.Width == 0) {
                        result.Add((0, StyledLine.Empty));
                        continue;
                    }
                    IReadOnlyList<StyledLine> wrapped = TextWrapper.Wrap(line, Math.Max(1, width - Math.Max(_first, _rest)));
                    for (int i = 0; i < wrapped.Count; i++) {
                        result.Add((i == 0 ? _first : _rest, wrapped[i]));
                    }
                }
                return result;
            }

            public Size Measure(Constraints constraints, RenderContext context) => new Size(constraints.MaxWidth, Math.Min(constraints.MaxHeight, Layout(constraints.MaxWidth).Count));

            public void Render(Region region, RenderContext context) {
                List<(int Indent, StyledLine Line)> lines = Layout(region.Width);
                for (int y = 0; y < lines.Count && y < region.Height; y++) {
                    region.Write(lines[y].Indent, y, lines[y].Line, region.Width - lines[y].Indent);
                }
            }
        }

        // A syntax line: the command, then the parameter units, wrapped between units under the first parameter.
        private sealed class SyntaxLines : IWidget {
            private const int Indent = 2;
            private readonly TextSpan _command;
            private readonly List<List<TextSpan>> _units;

            public SyntaxLines(TextSpan command, List<List<TextSpan>> units) {
                _command = command;
                _units = units;
            }

            private List<StyledLine> Layout(int width) {
                int hanging = Indent + _command.Width + 1;
                if (hanging > width / 2) {
                    hanging = Indent + 4;
                }
                List<StyledLine> lines = new List<StyledLine>();
                List<TextSpan> current = new List<TextSpan> { new TextSpan(new string(' ', Indent)), _command };
                int used = Indent + _command.Width;
                foreach (List<TextSpan> unit in _units) {
                    int unitWidth = 0;
                    foreach (TextSpan span in unit) {
                        unitWidth += span.Width;
                    }
                    if (used + 1 + unitWidth > width && used > hanging) {
                        lines.Add(new StyledLine(current));
                        current = new List<TextSpan> { new TextSpan(new string(' ', hanging)) };
                        used = hanging;
                    } else {
                        current.Add(new TextSpan(" "));
                        used++;
                    }
                    current.AddRange(unit);
                    used += unitWidth;
                }
                lines.Add(new StyledLine(current));
                return lines;
            }

            public Size Measure(Constraints constraints, RenderContext context) => new Size(constraints.MaxWidth, Math.Min(constraints.MaxHeight, Layout(constraints.MaxWidth).Count));

            public void Render(Region region, RenderContext context) {
                List<StyledLine> lines = Layout(region.Width);
                for (int y = 0; y < lines.Count && y < region.Height; y++) {
                    region.Write(0, y, TextWrapper.Truncate(lines[y], region.Width, context.Ellipsis));
                }
            }
        }
    }
}
