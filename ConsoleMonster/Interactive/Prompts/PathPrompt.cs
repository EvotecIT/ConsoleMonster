using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using ConsoleMonster.Canvas;
using ConsoleMonster.Input;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Interactive {
    /// <summary>
    /// A file or folder path with completion: matching entries of the typed folder are listed under the box (folders
    /// first), Tab completes the common part or the focused entry, Up/Down move through the suggestions, and Enter checks
    /// that the path exists and is the right kind when required.
    /// </summary>
    public sealed class PathPrompt : Prompt<string> {
        /// <summary>Creates a path prompt.</summary>
        /// <param name="title">The question.</param>
        public PathPrompt(string title) : base(title) {
        }

        /// <summary>What is accepted.</summary>
        public PathKind Kind { get; set; } = PathKind.Any;

        /// <summary>Whether the path must exist.</summary>
        public bool MustExist { get; set; }

        /// <summary>The folder relative paths start from; null uses the current directory.</summary>
        public string? BasePath { get; set; }

        /// <summary>File extensions offered as suggestions (".csv", ".json"); empty offers every file.</summary>
        public IList<string> Extensions { get; } = new List<string>();

        /// <summary>Whether hidden entries are suggested.</summary>
        public bool ShowHidden { get; set; }

        /// <summary>How many suggestions are listed.</summary>
        public int Suggestions { get; set; } = 6;

        /// <inheritdoc />
        public override string Describe(string value) => value;

        internal string Resolve(string text) {
            string expanded = Environment.ExpandEnvironmentVariables(text.Trim().Trim('"'));
            if (expanded.StartsWith("~", StringComparison.Ordinal)) {
                expanded = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + expanded.Substring(1);
            }
            if (expanded.Length == 0) {
                return expanded;
            }
            try {
                return Path.IsPathRooted(expanded) ? expanded : Path.Combine(BasePath ?? Directory.GetCurrentDirectory(), expanded);
            } catch (ArgumentException) {
                return expanded;
            }
        }

        internal string? Validate(string text, PromptLabels labels) {
            if (text.Trim().Length == 0) {
                return labels.Required;
            }
            string full = Resolve(text);
            bool isFile;
            bool isFolder;
            try {
                isFile = File.Exists(full);
                isFolder = Directory.Exists(full);
            } catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException || ex is NotSupportedException) {
                return PromptLabels.Format(labels.PathNotFound, text);
            }
            if (MustExist && !isFile && !isFolder) {
                return PromptLabels.Format(labels.PathNotFound, text);
            }
            if (Kind == PathKind.File && isFolder) {
                return labels.FileExpected;
            }
            if (Kind == PathKind.Folder && isFile) {
                return labels.FolderExpected;
            }
            return null;
        }

        internal List<string> Complete(string text) {
            List<string> result = new List<string>();
            string typed = Environment.ExpandEnvironmentVariables(text.Trim('"'));
            string folderPart;
            string namePart;
            int separator = Math.Max(typed.LastIndexOf('\\'), typed.LastIndexOf('/'));
            if (separator >= 0) {
                folderPart = typed.Substring(0, separator + 1);
                namePart = typed.Substring(separator + 1);
            } else {
                folderPart = string.Empty;
                namePart = typed;
            }
            string folder = folderPart.Length == 0 ? BasePath ?? Directory.GetCurrentDirectory() : Resolve(folderPart);
            try {
                if (!Directory.Exists(folder)) {
                    return result;
                }
                List<string> folders = new List<string>();
                List<string> files = new List<string>();
                foreach (string entry in Directory.EnumerateFileSystemEntries(folder)) {
                    string name = Path.GetFileName(entry);
                    if (!name.StartsWith(namePart, StringComparison.OrdinalIgnoreCase)) {
                        continue;
                    }
                    FileAttributes attributes = File.GetAttributes(entry);
                    if (!ShowHidden && (attributes & FileAttributes.Hidden) != 0) {
                        continue;
                    }
                    if ((attributes & FileAttributes.Directory) != 0) {
                        folders.Add(folderPart + name + Path.DirectorySeparatorChar);
                    } else if (Kind != PathKind.Folder && (Extensions.Count == 0 || HasExtension(name))) {
                        files.Add(folderPart + name);
                    }
                    if (folders.Count + files.Count > 400) {
                        break;
                    }
                }
                folders.Sort((a, b) => StringComparer.CurrentCultureIgnoreCase.Compare(a.TrimEnd(Path.DirectorySeparatorChar), b.TrimEnd(Path.DirectorySeparatorChar)));
                files.Sort(StringComparer.CurrentCultureIgnoreCase);
                result.AddRange(folders);
                result.AddRange(files);
            } catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException || ex is NotSupportedException || ex is System.Security.SecurityException) {
                return result;
            }
            return result;
        }

        private bool HasExtension(string name) {
            foreach (string extension in Extensions) {
                if (name.EndsWith(extension, StringComparison.OrdinalIgnoreCase)) {
                    return true;
                }
            }
            return false;
        }

        internal override PromptView<string> CreateView(PromptLabels labels) => new PathView(this, labels);

        internal override string AskLines(LinePrompter lines, CancellationToken cancellationToken) {
            return lines.Text(Title, Description, HasDefault ? Default : null, text => Validate(text, lines.Labels), false, cancellationToken);
        }

        private sealed class PathView : EntryView<string> {
            private readonly PathPrompt _prompt;
            private List<string> _matches = new List<string>();
            private int _focus = -1;

            public PathView(PathPrompt prompt, PromptLabels labels) : base(prompt, labels, prompt.HasDefault ? prompt.Default : null) {
                _prompt = prompt;
                Refresh();
            }

            protected override string? TryParse(string text, out string value) {
                value = text.Trim().Trim('"');
                return _prompt.Validate(value, Labels);
            }

            protected override void OnEdited() {
                _focus = -1;
                Refresh();
            }

            private void Refresh() {
                _matches = _prompt.Complete(Editor.Text);
            }

            protected override IWidget? Extra(ViewFrame frame) {
                if (_matches.Count == 0) {
                    return null;
                }
                return new Suggestions(this);
            }

            protected override void AddEntryBindings(List<KeyBinding> bindings, ViewFrame frame) {
                if (_matches.Count > 0) {
                    bindings.Add(new KeyBinding(Labels.KeyComplete, CompleteNow, new KeyChord(ConsoleKey.Tab)) { Order = 15 });
                    bindings.Add(new KeyBinding(Labels.KeyMove, () => { }) { KeyText = InteractiveGlyphs.For(frame.Context).KeyUpDown, Order = 20 });
                    bindings.Add(new KeyBinding(string.Empty, () => MoveFocus(-1), new KeyChord(ConsoleKey.UpArrow)) { InFooter = false });
                    bindings.Add(new KeyBinding(string.Empty, () => MoveFocus(1), new KeyChord(ConsoleKey.DownArrow)) { InFooter = false });
                }
            }

            protected override void Submit() {
                if (_focus >= 0 && _focus < _matches.Count) {
                    Apply(_matches[_focus]);
                    if (_prompt.Kind == PathKind.File && _matches.Count > 0 && Editor.Text.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)) {
                        return;
                    }
                }
                base.Submit();
            }

            private void MoveFocus(int delta) {
                if (_matches.Count == 0) {
                    return;
                }
                _focus += delta;
                if (_focus < -1) {
                    _focus = _matches.Count - 1;
                }
                if (_focus >= _matches.Count) {
                    _focus = -1;
                }
            }

            private void CompleteNow() {
                if (_focus >= 0 && _focus < _matches.Count) {
                    Apply(_matches[_focus]);
                    return;
                }
                if (_matches.Count == 1) {
                    Apply(_matches[0]);
                    return;
                }
                string common = _matches[0];
                foreach (string m in _matches) {
                    int n = 0;
                    while (n < common.Length && n < m.Length && char.ToLowerInvariant(common[n]) == char.ToLowerInvariant(m[n])) {
                        n++;
                    }
                    common = common.Substring(0, n);
                }
                if (common.Length > Editor.Text.Length) {
                    Editor.Text = common;
                    Refresh();
                } else {
                    _focus = 0;
                }
            }

            private void Apply(string value) {
                Editor.Text = value;
                _focus = -1;
                Error = null;
                Refresh();
            }

            private sealed class Suggestions : IWidget {
                private readonly PathView _view;

                public Suggestions(PathView view) {
                    _view = view;
                }

                public Size Measure(Constraints constraints, RenderContext context) {
                    int rows = Math.Min(_view._matches.Count, Math.Max(1, _view._prompt.Suggestions)) + (_view._matches.Count > _view._prompt.Suggestions ? 1 : 0);
                    return new Size(constraints.MaxWidth, Math.Min(rows, constraints.MaxHeight));
                }

                public void Render(Region region, RenderContext context) {
                    InteractiveGlyphs g = InteractiveGlyphs.For(context);
                    int limit = Math.Max(1, _view._prompt.Suggestions);
                    int start = _view._focus >= limit ? _view._focus - limit + 1 : 0;
                    int y = 0;
                    for (int i = start; i < _view._matches.Count && y < Math.Min(region.Height, limit); i++, y++) {
                        string entry = _view._matches[i];
                        bool folder = entry.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal);
                        bool focused = i == _view._focus;
                        Style style = context.Style(folder ? ThemeToken.Accent : ThemeToken.Text);
                        if (focused && context.HasColor) {
                            Style focus = context.Style(ThemeToken.Focus);
                            region.Fill(2, y, region.Width - 2, 1, " ", focus);
                            style = focus.With(TextDecoration.Bold);
                            region.Put(2, y, g.FocusBar, context.Style(ThemeToken.Accent).WithBackground(focus.Background));
                        } else if (focused) {
                            region.Put(2, y, g.FocusBar, context.Style(ThemeToken.Accent));
                        }
                        string name = Path.GetFileName(entry.TrimEnd(Path.DirectorySeparatorChar)) + (folder ? Path.DirectorySeparatorChar.ToString() : string.Empty);
                        region.Write(4, y, TextWrapper.Truncate(name, Math.Max(1, region.Width - 5), context.Ellipsis), style);
                    }
                    if (_view._matches.Count > limit && y < region.Height) {
                        region.Write(4, y, PromptLabels.Format(_view.Labels.More, _view._matches.Count - limit), context.Style(ThemeToken.Muted));
                    }
                }
            }
        }
    }
}
