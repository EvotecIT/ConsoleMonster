using System;
using System.Collections.Generic;
using System.Linq;
using ConsoleMonster.Canvas;
using ConsoleMonster.Help;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    public sealed partial class HelpView {
        private Rows BuildCommandLine(int width, RenderContext context) {
            Rows rows = new Rows();
            if (!string.IsNullOrWhiteSpace(Topic.Synopsis)) {
                rows.Add(Paragraph(Topic.Synopsis!, context, 0));
            }
            ParagraphsInto(rows, Topic.Description, context);
            Section(rows, "Usage", context);
            rows.Add(Paragraph(Topic.CommandLine!.Usage, context, 2));
            if (Topic.CommandLine.Commands.Count > 0) {
                Section(rows, "Commands", context);
                foreach (IGrouping<string, HelpCommand> group in Topic.CommandLine.Commands.GroupBy(c => c.Group ?? string.Empty)) {
                    if (group.Key.Length > 0) {
                        rows.Add(new LineBlock(new[] { StyledLine.From(group.Key, context.Style(ThemeToken.Accent)) }, 2, 2));
                    }
                    rows.Add(new HelpColumns(group.Select(c => (c.Name, c.Description, (string?)null)).ToList()));
                }
            }
            if (ShowParameters && Topic.Parameters.Count > 0) {
                Section(rows, "Options", context);
                List<(string, string, string?)> entries = new List<(string, string, string?)>();
                foreach (HelpParameter parameter in Topic.Parameters) {
                    List<string> names = new List<string>();
                    names.AddRange(parameter.Aliases);
                    names.Add(parameter.Named ? "-" + parameter.Name : "<" + parameter.Name + ">");
                    string syntax = string.Join(", ", names.Distinct().OrderBy(n => n.Length).ThenBy(n => n, StringComparer.Ordinal));
                    if (parameter.Named && !string.IsNullOrEmpty(parameter.Type)) {
                        syntax += " <" + parameter.Type + ">";
                    }
                    entries.Add((syntax, (parameter.Required ? "Required. " : string.Empty) + parameter.Description, parameter.Default));
                }
                rows.Add(new HelpColumns(entries));
            }
            if (ShowExamples && Topic.Examples.Count > 0) {
                Section(rows, "Examples", context);
                foreach (HelpExample example in Topic.Examples) {
                    if (!string.IsNullOrEmpty(example.Title)) rows.Add(Paragraph(example.Title!, context, 2));
                    rows.Add(new LineBlock(new[] { StyledLine.From(example.Code, context.Style(ThemeToken.Title)) }, 2, 4));
                    if (!string.IsNullOrEmpty(example.Remarks)) rows.Add(Paragraph(example.Remarks!, context, 4));
                }
            }
            return rows;
        }

        // Names and descriptions wrap independently. Long syntax moves above its description rather than stealing
        // the description column or being ellipsized. TextWrapper preserves all text even below a token's width.
        private sealed class HelpColumns : IWidget {
            private readonly List<(string Name, string Description, string? Default)> _entries;

            public HelpColumns(List<(string, string, string?)> entries) { _entries = entries; }

            private List<(int X, int Y, StyledLine Line)> Layout(int width, RenderContext context, out int height) {
                List<(int, int, StyledLine)> result = new List<(int, int, StyledLine)>();
                int available = Math.Max(1, width - 2);
                int nameWidth = Math.Min(_entries.Max(e => DisplayWidth.Measure(e.Name)), Math.Max(12, available / 2));
                int descriptionX = Math.Min(width - 1, nameWidth + 4);
                int y = 0;
                foreach (var entry in _entries) {
                    StyledLine name = StyledLine.From(entry.Name, context.Style(ThemeToken.Title));
                    List<TextSpan> spans = new List<TextSpan> { new TextSpan(entry.Description, context.Style(ThemeToken.Text)) };
                    if (!string.IsNullOrEmpty(entry.Default)) spans.Add(new TextSpan("  [default: " + entry.Default + "]", context.Style(ThemeToken.Muted)));
                    StyledLine description = new StyledLine(spans);
                    bool stacked = name.Width > nameWidth;
                    IReadOnlyList<StyledLine> names = TextWrapper.Wrap(name, stacked ? available : nameWidth);
                    foreach (StyledLine line in names) result.Add((2, y++, line));
                    int start = stacked ? y : y - names.Count;
                    int x = stacked ? 4 : descriptionX;
                    IReadOnlyList<StyledLine> descriptions = TextWrapper.Wrap(description, Math.Max(1, width - x));
                    foreach (StyledLine line in descriptions) result.Add((x, start++, line));
                    y = Math.Max(y, start);
                }
                height = y;
                return result;
            }

            public Size Measure(Constraints constraints, RenderContext context) {
                Layout(constraints.MaxWidth, context, out int height);
                return new Size(constraints.MaxWidth, Math.Min(height, constraints.MaxHeight));
            }

            public void Render(Region region, RenderContext context) {
                foreach (var line in Layout(region.Width, context, out _)) {
                    if (line.Y < region.Height) region.Write(line.X, line.Y, line.Line, region.Width - line.X);
                }
            }
        }
    }
}
