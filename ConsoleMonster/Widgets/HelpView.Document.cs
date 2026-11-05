using System;
using System.Collections.Generic;
using System.Globalization;
using ConsoleMonster.Canvas;
using ConsoleMonster.Help;
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    public sealed partial class HelpView {
        /// <summary>
        /// The help as a navigable document: a header card with the synopsis and badges, then Overview (syntax,
        /// description, inputs, outputs, notes), Parameters (one folded row per parameter), Examples (each folded under its
        /// title) and Related. Compact command-line help has no such structure and returns null.
        /// </summary>
        /// <param name="context">The render context the styles come from.</param>
        /// <param name="openCommands">Whether related command names become links the viewer can open.</param>
        internal DocumentModel? Document(RenderContext context, bool openCommands) {
            if (Topic.CommandLine != null) { return null; }
            DocumentModel model = new DocumentModel(Topic.Name) { Header = HeaderCard(context) };
            DocumentPart overview = new DocumentPart("Overview");
            if (Topic.Syntax.Count > 0) {
                Rows syntax = new Rows();
                for (int i = 0; i < Topic.Syntax.Count; i++) {
                    if (i > 0) { syntax.Add(Blank()); }
                    syntax.Add(SyntaxBlock(Topic.Syntax[i], context));
                }
                overview.Sections.Add(new DocumentSection("Syntax", 1, "syntax", syntax));
            }
            if (Topic.Description.Count > 0) { overview.Sections.Add(new DocumentSection("Description", 1, "description", Paragraphs(Topic.Description, context))); }
            AddTypes(overview, "Inputs", Topic.Inputs, context);
            AddTypes(overview, "Outputs", Topic.Outputs, context);
            if (Topic.Notes.Count > 0) { overview.Sections.Add(new DocumentSection("Notes", 1, "notes", Paragraphs(Topic.Notes, context))); }
            if (overview.Sections.Count > 0) { model.Parts.Add(overview); }

            if (ShowParameters && (Topic.Parameters.Count > 0 || Topic.CommonParameters)) {
                DocumentPart parameters = new DocumentPart("Parameters");
                foreach (HelpParameter parameter in Topic.Parameters) {
                    string name = (parameter.Named ? "-" : string.Empty) + parameter.Name;
                    parameters.Sections.Add(new DocumentSection(name, 1, "parameter-" + DocumentModel.Slugs.Slug(parameter.Name), ParameterDetails(parameter, context)) {
                        StartFolded = true, Summary = ParameterSummary(parameter), Category = "Parameter"
                    });
                }
                if (Topic.CommonParameters) {
                    parameters.Sections.Add(new DocumentSection(string.Empty, 0, string.Empty, new LineBlock(new[] { StyledLine.From("Common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -OutVariable, -Verbose, -WarningAction and the others (about_CommonParameters).", context.Style(ThemeToken.Muted)) }, 0, 0)));
                }
                model.Parts.Add(parameters);
            }

            if (ShowExamples && Topic.Examples.Count > 0) {
                DocumentPart examples = new DocumentPart("Examples");
                for (int i = 0; i < Topic.Examples.Count; i++) {
                    HelpExample example = Topic.Examples[i];
                    string number = (i + 1).ToString(CultureInfo.InvariantCulture);
                    string title = string.IsNullOrEmpty(example.Title) ? "Example " + number : number + ". " + example.Title;
                    Rows body = new Rows();
                    if (example.Code.Length > 0) { body.Add(CodeView.For(example.Code, "powershell")); }
                    if (!string.IsNullOrEmpty(example.Remarks)) {
                        if (example.Code.Length > 0) { body.Add(Blank()); }
                        body.Add(Paragraph(example.Remarks!, context, 0));
                    }
                    examples.Sections.Add(new DocumentSection(title, 1, "example-" + number, body) { StartFolded = true, Summary = FirstLine(example.Code), Category = "Example" });
                }
                model.Parts.Add(examples);
            }

            if (Topic.Links.Count > 0) {
                DocumentPart related = new DocumentPart("Related");
                List<StyledLine> links = new List<StyledLine>();
                foreach (HelpLink link in Topic.Links) {
                    List<TextSpan> spans = new List<TextSpan>();
                    string text = (link.Text ?? string.Empty).Trim();
                    if (text.Length > 0) {
                        // A command or about topic name opens that help in the viewer; anything else stays text.
                        bool command = openCommands && text.IndexOf(' ') < 0;
                        spans.Add(new TextSpan(text, command ? context.Style(ThemeToken.Accent2).With(TextDecoration.Underline).WithLink(DocumentModel.HelpScheme + text) : context.Style(ThemeToken.Text)));
                    }
                    if (!string.IsNullOrEmpty(link.Uri)) {
                        spans.Add(new TextSpan((spans.Count > 0 ? "  " : string.Empty) + link.Uri, context.Style(ThemeToken.Accent).WithLink(link.Uri)));
                    }
                    if (spans.Count > 0) { links.Add(new StyledLine(spans)); }
                }
                related.Sections.Add(new DocumentSection(string.Empty, 0, string.Empty, new LineBlock(links, 0, 2)));
                model.Parts.Add(related);
            }
            return model.Parts.Count == 0 ? null : model;
        }

        // The name with the module, version and -WhatIf badges, the synopsis and any notice.
        private IWidget HeaderCard(RenderContext context) {
            List<TextSpan> head = new List<TextSpan> { new TextSpan(Topic.Name, context.Style(ThemeToken.Title).With(TextDecoration.Bold)) };
            Badge(head, Topic.Module, context);
            Badge(head, Topic.Version == null ? null : "v" + Topic.Version, context);
            Badge(head, Topic.SupportsWhatIf ? "-WhatIf" : null, context);
            Rows card = new Rows();
            card.Add(new LineBlock(new[] { new StyledLine(head) }, 0, 2));
            if (HasSynopsis()) { card.Add(Paragraph(Topic.Synopsis!, context, 0)); }
            if (!string.IsNullOrEmpty(Topic.Notice)) { card.Add(new LineBlock(new[] { StyledLine.From(Topic.Notice!, context.Style(ThemeToken.Muted)) }, 0, 0)); }
            return new Panel(card) { BorderRole = ThemeToken.Border };
        }

        // A badge reads as a filled label in colour and as [label] where colour is off.
        private static void Badge(List<TextSpan> spans, string? text, RenderContext context) {
            if (string.IsNullOrEmpty(text)) { return; }
            spans.Add(new TextSpan("  "));
            spans.Add(context.HasColor
                ? new TextSpan(" " + text + " ", context.Style(ThemeToken.Accent).With(TextDecoration.Reverse))
                : new TextSpan("[" + text + "]", context.Style(ThemeToken.Accent)));
        }

        private static IWidget Paragraphs(IList<string> paragraphs, RenderContext context) {
            Rows rows = new Rows();
            for (int i = 0; i < paragraphs.Count; i++) {
                if (i > 0) { rows.Add(Blank()); }
                rows.Add(Paragraph(paragraphs[i], context, 0));
            }
            return rows;
        }

        private static void AddTypes(DocumentPart part, string name, IList<string> types, RenderContext context) {
            if (types.Count == 0) { return; }
            List<StyledLine> lines = new List<StyledLine>();
            foreach (string entry in types) {
                int tab = entry.IndexOf('\t');
                string type = tab >= 0 ? entry.Substring(0, tab) : entry;
                string note = tab >= 0 ? entry.Substring(tab + 1).Trim() : string.Empty;
                List<TextSpan> spans = new List<TextSpan> { new TextSpan(type.Trim(), context.Style(ThemeToken.Accent2)) };
                if (note.Length > 0) { spans.Add(new TextSpan("  " + note, context.Style(ThemeToken.Muted))); }
                lines.Add(new StyledLine(spans));
            }
            part.Sections.Add(new DocumentSection(name, 1, name.ToLowerInvariant(), new LineBlock(lines, 0, 2)));
        }

        // "<string[]>  default .  Specifies a path": what a folded parameter row shows.
        private static string ParameterSummary(HelpParameter parameter) {
            List<string> parts = new List<string>();
            if (!string.IsNullOrEmpty(parameter.Type)) { parts.Add("<" + parameter.Type + ">"); }
            if (parameter.Required) { parts.Add("required"); }
            if (!string.IsNullOrEmpty(parameter.Default)) { parts.Add("default " + parameter.Default); }
            string description = FirstLine(parameter.Description);
            if (description.Length > 0) { parts.Add(description); }
            return string.Join("  ", parts);
        }

        private static IWidget ParameterDetails(HelpParameter parameter, RenderContext context) {
            Style label = context.Style(ThemeToken.Muted);
            Style value = context.Style(ThemeToken.Text);
            List<StyledLine> facts = new List<StyledLine>();
            void Fact(string name, string? text) {
                if (string.IsNullOrEmpty(text)) { return; }
                facts.Add(new StyledLine(new[] { new TextSpan((name + ":").PadRight(10), label), new TextSpan(text!, value) }));
            }
            Fact("Type", parameter.Type);
            Fact("Required", parameter.Required ? "yes" : "no");
            Fact("Position", parameter.Position ?? "named");
            Fact("Pipeline", parameter.Pipeline ?? "no");
            Fact("Default", parameter.Default);
            Fact("Aliases", parameter.Aliases.Count == 0 ? null : string.Join(", ", parameter.Aliases));
            Rows rows = new Rows();
            rows.Add(new LineBlock(facts, 0, 10));
            if (!string.IsNullOrEmpty(parameter.Description)) {
                rows.Add(Blank());
                rows.Add(Paragraph(parameter.Description!, context, 0));
            }
            return rows;
        }

        private static string FirstLine(string? text) {
            if (string.IsNullOrEmpty(text)) { return string.Empty; }
            foreach (string line in text!.Replace("\r\n", "\n").Split('\n')) {
                string trimmed = line.Trim();
                if (trimmed.Length > 0) { return trimmed; }
            }
            return string.Empty;
        }
    }
}
