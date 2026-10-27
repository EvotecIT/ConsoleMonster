using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using ConsoleMonster.Layout;
using ConsoleMonster.Text;
using ConsoleMonster.Values;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Views {
    /// <summary>An explicitly supplied renderer for a view owned by an add-on.</summary>
    public interface IConsoleViewProvider {
        /// <summary>Builds an add-on view from already collected input.</summary>
        /// <param name="items">Input records or text.</param>
        /// <param name="options">View configuration.</param>
        /// <returns>The constructed widget.</returns>
        IWidget Create(IReadOnlyList<object?> items, ConsoleViewOptions options);
    }

    public static partial class ConsoleView {
        /// <summary>Builds highlighted code or JSON through the same representation factory as Show-Console.</summary>
        /// <param name="items">Text or structured JSON input.</param>
        /// <param name="options">Display configuration.</param>
        /// <returns>The configured code view.</returns>
        public static CodeView BuildCode(IReadOnlyList<object?> items, ConsoleViewOptions options) {
            if (items == null) { throw new ArgumentNullException(nameof(items)); }
            if (options == null) { throw new ArgumentNullException(nameof(options)); }
            string text = string.Join("\n", items.Select(item => Convert.ToString(item, CultureInfo.CurrentCulture) ?? string.Empty));
            bool json = options.View == ConsoleViewKind.Json;
            if (json) {
                if (!items.All(item => item is string) || !ValueJson.TryReindent(text, out string indented)) {
                    text = ValueJson.Serialize(items.Count == 1 ? items[0] : items, options.Depth, options.Shape);
                } else { text = indented; }
            }
            CodeView code = CodeView.For(text, json ? "Json" : options.Language, options.Path);
            code.LineNumbers = options.LineNumbers;
            code.FirstLine = options.FirstLine;
            code.Wrap = options.Wrap;
            code.MaxLines = Math.Max(1, options.MaxLines);
            code.Title = options.NoTitle ? null : options.Title ?? (options.Path == null ? null : Path.GetFileName(options.Path));
            foreach (string range in options.Highlight ?? Array.Empty<string>()) { code.HighlightLines(range); }
            return code;
        }

        /// <summary>Builds one error view or a vertical sequence of error views.</summary>
        /// <param name="items">Exceptions, ExceptionInfo descriptors or error messages.</param>
        /// <param name="options">Error presentation configuration.</param>
        /// <returns>The error widget.</returns>
        public static IWidget BuildErrors(IReadOnlyList<object?> items, ConsoleViewOptions options) {
            if (items == null) { throw new ArgumentNullException(nameof(items)); }
            if (options == null) { throw new ArgumentNullException(nameof(options)); }
            var errors = new List<IWidget>();
            foreach (object? item in items) {
                ExceptionInfo info = item is ExceptionInfo descriptor ? descriptor : item is Exception error
                    ? ExceptionInfo.FromException(error) : new ExceptionInfo("Error", Convert.ToString(item, CultureInfo.CurrentCulture) ?? string.Empty);
                errors.Add(new ExceptionView(info) {
                    ShowFrameworkFrames = options.ShowFrameworkFrames, MaxFrames = Math.Max(1, options.MaxFrames),
                    ShowStack = options.ShowStack && (!options.Compact || options.ShowFrameworkFrames),
                    MaxInner = options.MaxInner, ShowSource = options.ShowSource, Compact = options.Compact,
                    DetailsHint = options.DetailsHint, BasePath = options.BasePath
                });
            }
            return errors.Count == 1 ? errors[0] : new Rows(errors.ToArray()) { Gap = 1 };
        }

        /// <summary>Builds a record sheet from ordinary C# objects or dictionaries.</summary>
        /// <param name="items">Records.</param>
        /// <param name="options">Title, property selection and empty-field policy.</param>
        /// <returns>The record sheet.</returns>
        public static RecordView BuildList(IReadOnlyList<object?> items, ConsoleViewOptions options) {
            if (items == null) { throw new ArgumentNullException(nameof(items)); }
            if (options == null) { throw new ArgumentNullException(nameof(options)); }
            var view = new RecordView(options.Title) { HideEmpty = !options.ShowEmpty };
            foreach (object? item in items) {
                if (item == null) { continue; }
                if (items.Count > 1) { view.AddSection(Markup.Escape(ValueShaper.Title(item, options.Shape))); }
                foreach (Data.InspectMember member in Members(item, options)) {
                    if (options.Property != null && !options.Property.Contains(member.Name, StringComparer.OrdinalIgnoreCase)) { continue; }
                    view.AddField(member.Name, SafeRead(member));
                }
            }
            return view;
        }
    }
}
